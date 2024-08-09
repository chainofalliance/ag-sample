import { Network } from "./env";
import { guid } from "./javascript-helper";
import { Match, MatchDetails } from "./match";
import { Ticket, TicketState } from "./ticket";
import { Client, Value } from 'ts-postgres';
import { logger } from './logger.js';
import crypto from 'crypto';

const sha256 = (x: string) => crypto.createHash('sha256').update(x, 'utf8').digest('hex').substring(0, 8);

export class PostgresService {
    private static client: Client;
    public ticketCache: Ticket[] = [];

    private readonly ticketTableSql = `
        id CHAR(36) PRIMARY KEY,
        created_at BIGINT NOT NULL,
        address CHAR(66) NOT NULL,
        state SMALLINT NOT NULL,
        match_id CHAR(36)
    `;
    private readonly matchTableSql = `
        id CHAR(36) PRIMARY KEY,
        created_at BIGINT NOT NULL,
        server_details TEXT NOT NULL,
        home_player CHAR(66) NOT NULL,
        away_player CHAR(66)
    `;
    private readonly envName = this.env.replace('-', '');
    private readonly ticketTable = `ticket_${this.envName}_${sha256(this.ticketTableSql)}`;
    private readonly matchTable = `match_${this.envName}_${sha256(this.matchTableSql)}`;
    private readonly notifyName = `notify_${this.envName}_${sha256(this.ticketTableSql)}`;

    private constructor(private env: Network) { }

    public static async connectMatchmaking(env: Network, skipTableCheck: boolean = false): Promise<PostgresService> {
        var postgres = await this.connect(env);
        if (!skipTableCheck) {
            await postgres.initMatchmaking();
        }
        return postgres;
    }

    private static async connect(env: Network): Promise<PostgresService> {
        if (this.client == undefined) {
            this.client = new Client({
                host: "matchmaking-db",
                port: 5432,
                database: "matchmaking",
                user: "postgres",
                password: "postgres"
            });
            await this.client.connect();
        }

        return new PostgresService(env);
    }

    public static async cleanup(): Promise<void> {
        await this.client.end();
    }

    private async initMatchmaking(): Promise<void> {
        PostgresService.client.query(`begin`);
        await this.initTable(this.ticketTable, this.ticketTableSql);
        await this.initTable(this.matchTable, this.matchTableSql);
        await PostgresService.client.query(`commit`);
    }

    private async initTable(name: string, sql: string): Promise<void> {
        if (await this.doesTableExist(name)) {
            log('debug', `Skip 'initTable' for '${name}'`);
            return;
        }

        log('debug', `Creating table '${name}'`);

        PostgresService.client.query(`CREATE TABLE IF NOT EXISTS ${name} (${sql});`);
        if (name == this.ticketTable) {
            PostgresService.client.query(`CREATE OR REPLACE FUNCTION public.${this.notifyName}()
                    RETURNS trigger
                    LANGUAGE plpgsql
                AS $${this.notifyName}$
                BEGIN
                    IF (TG_OP = 'UPDATE') THEN
                        PERFORM pg_notify('update_ticket_${this.envName}', row_to_json(NEW)::text);
                    END IF;
                    RETURN NULL;
                END;
                $${this.notifyName}$;`);
            PostgresService.client.query(`CREATE OR REPLACE TRIGGER ${this.ticketTable} AFTER UPDATE ON ${this.ticketTable}
                FOR EACH ROW EXECUTE PROCEDURE ${this.notifyName}();`);
        }
    }
    public addListener(): void {
        PostgresService.client.query(`LISTEN update_ticket_${this.envName}`);
        PostgresService.client.on("notification", data => {
            const incomingTicket = this.jsonToTicket(data.payload!);
            const ticket = this.ticketCache.find(t => t.id == incomingTicket.id);
            if (!ticket) {
                return;
            }

            ticket.merge(incomingTicket);
            this.updateTicketCache(ticket);
        });
    }

    public async addTicket(ticket: Ticket): Promise<boolean> {
        var result = await PostgresService.client.query(`
            INSERT INTO ${this.ticketTable}
            (id, created_at, address, state)
            VALUES
            ($1, $2, $3, $4)`
            , [
                ticket.id,
                ticket.createdAt,
                ticket.address,
                ticket.state as number,
            ]);
        var success = result.status != 'INSERT 0';
        if (success) {
            this.ticketCache.push(ticket);
        }
        return success;
    }

    public async getTicket(id: string): Promise<Ticket | null> {
        var cachedTicket = this.ticketCache.find(t => t.id == id);
        if (cachedTicket != undefined) {
            return cachedTicket;
        }

        var result = await PostgresService.client.query(`SELECT * FROM ${this.ticketTable} WHERE id = $1`, [id]);
        if (result.rows.length == 0) {
            return null;
        }
        return this.queryToTicket(result.rows[0]!);
    }

    public async updateTicket(ticket: Ticket): Promise<boolean> {
        let result = await PostgresService.client.query(`UPDATE ${this.ticketTable} SET state = $1, match_id = $3 WHERE id = $4`, [
            ticket.state as number,
            ticket.matchId,
            ticket.id
        ]);
        var success = result.status != 'UPDATE 0';
        if (success) {
            this.updateTicketCache(ticket);
        }
        return success;
    }

    public async cancelTicket(ticket: Ticket): Promise<boolean> {
        let result = await PostgresService.client.query(`UPDATE public.${this.ticketTable} SET state = $1 WHERE id = $2 AND state = $3`, [
            TicketState.Cancelled as number,
            ticket.id,
            TicketState.Open as number
        ]);
        var success = result.status != 'UPDATE 0';
        if (success) {
            this.updateTicketCache(ticket);
        }
        return success;
    }

    public async cancelAllTickets(address: string): Promise<void> {
        await PostgresService.client.query(`UPDATE ${this.ticketTable} SET state = $1 WHERE address = $2 AND state = $3`, [
            TicketState.Cancelled as number,
            address,
            TicketState.Open as number
        ]);
        this.ticketCache.filter(t => t.address == address).forEach(t => t.cancel());
    }

    public async closePendingTickets(): Promise<void> {
        await PostgresService.client.query(`UPDATE ${this.ticketTable} SET state = $1 WHERE state = $2`, [
            TicketState.Closed as number,
            TicketState.Open as number
        ]);
        this.ticketCache.length = 0;
    }

    private updateTicketCache(ticket: Ticket) {
        const index = this.ticketCache.indexOf(ticket);
        if (ticket.state != TicketState.Open
            && ticket.state != TicketState.WaitingForServer
            && index > -1) {
            this.ticketCache.splice(index, 1);
        }
    }

    public async addMatch(match: Match, home: Ticket, away: Ticket | null): Promise<boolean> {
        PostgresService.client.query(`begin`);
        PostgresService.client.query(`
            INSERT INTO ${this.matchTable}
            (id, created_at, server_details, home_player, away_player)
            VALUES
            ($1, $2, $3, $4, $5)`
            , [
                match.id,
                match.createdAt,
                match.serverDetails,
                home.address,
                away?.address ?? null
            ]);
        PostgresService.client.query(`UPDATE ${this.ticketTable} SET state = $1 WHERE id = $2 OR id = $3`, [
            TicketState.Matched as number,
            home.id,
            away?.id ?? null
        ]);
        var result = await PostgresService.client.query(`commit`);
        return result.status != 'INSERT 0';
    }

    public async getMatch(id: string, address: string): Promise<MatchDetails | null> {
        var result = await PostgresService.client.query(`SELECT id, created_at, server_details FROM ${this.matchTable} WHERE id = $1 AND (home_player = $2 OR away_player = $2)`, [
            id,
            address
        ]);
        if (result.rows.length == 0)
            return null;
        return this.queryToMatchDetails(result.rows[0]!);
    }

    private async doesTableExist(name: string) {
        var result = await PostgresService.client.query(`SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = $1);`, [
            name
        ]);
        return result.rows[0]![0]!;
    }

    private queryToTicket(values: Value[]): Ticket {
        let ticket = new Ticket(
            values[0] as string,
            values[2] as string,
            Number(values[1]),
            values[4] as string
        );
        ticket.setState(<TicketState>Number(values[6]));
        return ticket;
    }

    private queryToMatchDetails(values: Value[]): MatchDetails {
        return {
            id: values[0] as string,
            createdAt: Number(values[1]),
            serverDetails: values[2] as string
        };
    }

    private jsonToTicket(json: string): Ticket {
        let obj = JSON.parse(json);
        let ticket = new Ticket(
            obj['address'] as string,
            obj['id'] as string,
            Number(obj['created_at']),
            obj['match_id'] as string
        );
        ticket.setState(obj['state']);
        return ticket;
    }
}

function log(level: any, message: string): string {
    var id = guid();
    logger.log(level, `Postgres ${id} > ${message}`);
    return id;
}
