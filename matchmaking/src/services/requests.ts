import config from 'config';
import { now } from '../javascript-helper';
import { MatchmakingError } from './matchmaking';

type RateLimitKey = [string, Request];
type RateLimitValue = [number, number];
const RATE_LIMIT_MAP: Map<RateLimitKey, RateLimitValue> = new Map();

interface Rule {
    type: Request;
    requests: number;
    in_time: number;
};
const RULES: Rule[] = config.get(`common.rate_limit`);

export enum Request {
    CreateTicket = "create-ticket",
    TicketStatus = "ticket-status",
    MatchDetails = "match-details",
    CancelTicket = "cancel-ticket",
    CancelAllTickets = "cancel-all-ticket",
}

export interface BaseTicketRequest {
    address: string;
}

export interface CreateTicketRequest extends BaseTicketRequest {
}

export interface CreateTicketResponse {
    ticketId: string;
}

export interface TicketStatusRequest extends BaseTicketRequest {
    ticketId: string;
}

export interface TicketStatusResponse {
    ticketId: string;
    matchId: string | null;
    createdAt: number;
    status: string;
}

export interface MatchDetailsRequest extends BaseTicketRequest {
    matchId: string;
}

export interface MatchDetailsResponse {
    matchId: string;
    matchedAt: number;
    serverDetails: string;
}

export interface CancelTicketRequest extends BaseTicketRequest {
    ticketId: string;
}

export function rateLimit(request: Request, ctx: any) {
    let id = ctx.request.body.playfabId;

    const key: [string, Request] = [id, request];
    const rule = RULES.find(e => e.type == request)!;
    const currentTimestamp = now();
    if (RATE_LIMIT_MAP.has(key)) {
        let [timestamp, triesSince] = RATE_LIMIT_MAP.get(key)!;
        let timeDiff = currentTimestamp - timestamp;
        if (timeDiff < rule.in_time) {
            let newTries = triesSince + 1;
            if (newTries >= rule.requests) {
                throw new MatchmakingError("Rate limit exceeded");
            } else {
                RATE_LIMIT_MAP.set(key, [timestamp, newTries]);
                return;
            }
        }
    } else {
        RATE_LIMIT_MAP.set(key, [currentTimestamp, 1]);
    }
}

