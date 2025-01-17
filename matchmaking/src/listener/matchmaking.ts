import config from 'config';
import { Ticket } from '../ticket.js';
import { guid, now } from '../javascript-helper.js';
import { Match } from '../match.js';
import { PostgresService } from '../postgres.js';
import { addSession } from '../services/blockchain/operations.js';
import { MatchData } from '../services/blockchain/types.js';
import { logger } from '../logger.js';
import { formatter } from 'postchain-client';
import { queryConnectionDetails } from '../services/blockchain/queries.js';

let postgres: PostgresService;

export function init(
    postgresService: PostgresService
) {
    postgres = postgresService;
}

export async function run() {
    const MATCH_FREQUENCY: number = config.get(`application.matchmaking.match_frequency`) as number * 1000;

    try {
        postgres.ticketCache.forEach(t => t.update());
        match();
        for (let ticket of postgres.ticketCache) {
            await ticket.flush(postgres);
        }
    } catch (error) {
        log('error', `Error: ${error}`);
    }

    setTimeout(async () => await run(), MATCH_FREQUENCY);
}

function match() {
    for (let ticket of postgres.ticketCache) {
        let validTickets = postgres.ticketCache.filter(t => ticket.canMatch(t));
        if (validTickets.length > 0) {
            let matchedTicket = validTickets[Math.floor(Math.random() * validTickets.length)]!;
            log('info', `Matched ticket ${ticket.id} with ${matchedTicket.id}`);
            resolve(ticket, matchedTicket);
        }

        if (ticket.checkFallback()) {
            log('info', `Start AI match for ${ticket.id}`);
            resolve(ticket, null);
        }
    }
}

async function resolve(ticket1: Ticket, ticket2: Ticket | null) {
    try {
        ticket1.waitForServer();
        ticket2?.waitForServer();
        const sessionId = guid();

        const participants: Buffer[] = [];
        const matchData: MatchData[] = [];

        participants.push(formatter.toBuffer(ticket1.address));

        matchData.push({
            Id: ticket1.address
        });

        if (ticket2) {
            participants.push(formatter.toBuffer(ticket2.address));
            matchData.push({Id: ticket2.address});
        }

        await addSession(sessionId, participants, matchData);

        ticket1.match(sessionId);
        ticket2?.match(sessionId);

        const url = await queryConnectionDetails(sessionId);

        if(url == undefined) {
            throw new Error("Cannot query connection details");
        }

        let match: Match = {
            id: sessionId,
            isAI: ticket2 == null,
            serverDetails: url!,
            createdAt: now()
        };

        await postgres.addMatch(match, ticket1, ticket2);

    } catch (error) {
        log('error', `Failed to create match ${error}`);
        ticket1.close();
        ticket2?.close();
    }

    await postgres.updateTicket(ticket1);
    if (ticket2)
        await postgres.updateTicket(ticket2);
}

export function log(level: any, message: string): string {
    const id = guid();
    logger.log(level, `MatchmakingListener ${id} > ${message}`);
    return id;
}
