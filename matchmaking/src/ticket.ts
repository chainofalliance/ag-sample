import config from 'config';
import { guid, now } from './javascript-helper';
import { TicketStatusResponse } from './services/requests';
import { PostgresService } from './postgres';

const FALLBACK_TIMEOUT: number = config.get(`common.matchmaking.fallback_timeout`);

export class Ticket {
    private _state: TicketState;
    private _matchId: string | null = null;
    private _dirty: boolean = false;

    public get state(): TicketState { return this._state; }
    public get matchId(): string | null { return this._matchId; }
    public get dirty(): boolean { return this._dirty; }

    public constructor(
        public readonly address: string,
        public readonly id: string = guid(),
        public readonly createdAt: number = now(),
        matchId: string | null = null
    ) {
        this._state = TicketState.Open;
        this._matchId = matchId;
    }

    public update() {
        if (this.state != TicketState.Open)
            return;

        this._dirty = true;
    }

    public status(): TicketStatusResponse {
        return {
            ticketId: this.id,
            createdAt: this.createdAt,
            status: TicketState[this.state],
            matchId: this.matchId
        };
    }

    public canMatch(ticket: Ticket): boolean {
        return this.address != ticket.address
            && this.state == TicketState.Open
            && ticket.state == TicketState.Open
    }

    public checkFallback(): boolean {
        if (this._state != TicketState.Open)
            return false;

        const diffTime = now() - this.createdAt;
        return diffTime > FALLBACK_TIMEOUT;
    }

    public waitForServer() {
        if (this._state == TicketState.Open) {
            this._state = TicketState.WaitingForServer;
            this._dirty = true;
        }
    }

    public match(matchId: string) {
        if (this._state != TicketState.WaitingForServer)
            return;

        this._state = TicketState.Matched;
        this._matchId = matchId;
        this._dirty = true;
    }

    public cancel() {
        if (this._state == TicketState.Open) {
            this._state = TicketState.Cancelled;
            this._dirty = true;
        }
    }

    public close() {
        if (this._state == TicketState.Open) {
            this._state = TicketState.Closed;
            this._dirty = true;
        }
    }

    public setState(ticketState: TicketState) {
        this._state = ticketState;
    }

    public merge(incoming: Ticket) {
        this._state = incoming.state;
        this._matchId = incoming.matchId;
    }

    public async flush(postgres: PostgresService) {
        if (!this._dirty)
            return;
        this._dirty = false;
        await postgres.updateTicket(this);
    }

    public toString(): string {
        return JSON.stringify(this);
    }
}

export enum TicketState {
    Open,
    WaitingForServer,
    Matched,
    Closed,
    Cancelled
}
