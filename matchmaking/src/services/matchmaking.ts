import Router from '@koa/router';
import { logger } from '../logger.js';
import { Ticket } from '../ticket.js';
import {
    Request,
    CreateTicketRequest,
    rateLimit,
    CancelTicketRequest,
    TicketStatusRequest,
    BaseTicketRequest,
    CreateTicketResponse,
    MatchDetailsRequest
} from '../services/requests.js';
import { guid } from '../javascript-helper.js';
import { PostgresService } from '../postgres.js';

let postgres: PostgresService;

export class MatchmakingError extends Error { }

export function init(
    postgresService: PostgresService
) {
    postgres = postgresService;
}

export const router = new Router();

/*
INFORMATION:
 The nodejs event loop specifies that timers are handled before inbound requests.
 https://nodejs.org/en/docs/guides/event-loop-timers-and-nexttick/#event-loop-explained
 Therefore we do not have to think about thread safety or anything, but need to keep in mind that
 incoming requests for cancellation can already be matched. The matchmaking listener does not
 need to check for that.
*/
router.post('create-ticket', async (ctx, _next) => {
    await requestDispatcher(Request.CreateTicket, ctx);
});

router.post('ticket-status', async (ctx, _next) => {
    await requestDispatcher(Request.TicketStatus, ctx);
});

router.post('match-details', async (ctx, _next) => {
    await requestDispatcher(Request.MatchDetails, ctx);
});

router.post('cancel-ticket', async (ctx, _next) => {
    await requestDispatcher(Request.CancelTicket, ctx);
});

router.post('cancel-all-tickets', async (ctx, _next) => {
    await requestDispatcher(Request.CancelAllTickets, ctx);
});

async function requestDispatcher(request: Request, ctx: any) {
    try {
        rateLimit(request, ctx);

        // const accountInfo = await authenticate(ctx.request.body as BaseTicketRequest);
        switch (request) {
            case Request.CreateTicket: await createTicket(ctx.request.body as CreateTicketRequest, ctx); break;
            case Request.TicketStatus: await getTicketStatus(ctx.request.body as TicketStatusRequest, ctx); break;
            case Request.MatchDetails: await getMatchDetails(ctx.request.body as MatchDetailsRequest, ctx); break;
            case Request.CancelTicket: await cancelTicket(ctx.request.body as CancelTicketRequest); break;
            case Request.CancelAllTickets: await cancelAllTickets(ctx.request.body as BaseTicketRequest); break;
        }

        if (!ctx.response.body)
            ctx.response.body = {};
        ctx.response.body = {
            ...ctx.response.body, ...{
                error: false,
                errorMessage: null,
            }
        };
        ctx.response.status = 200;
    } catch (error: unknown) {
        const id = log('error', `${error}`);
        let message = "Unknown error: " + id;
        if (error instanceof MatchmakingError)
            message = error.message;

        ctx.response.body = {
            error: true,
            errorMessage: message,
        };
        ctx.response.status = 400;
    }
}

async function createTicket(
    request: CreateTicketRequest,
    ctx: any
): Promise<void> {

    let ticket = new Ticket(
        request.address
    );

    if (!await postgres.addTicket(ticket))
        throw new MatchmakingError("creating ticket failed");

    log('info', `Add ticket ${ticket.id} for ${request.address}`);
    const response: CreateTicketResponse = {
        ticketId: ticket.id
    };
    ctx.response.body = response;
}

async function getTicketStatus(
    request: TicketStatusRequest,
    ctx: any
): Promise<void> {
    const ticket = await postgres.getTicket(request.ticketId);
    if (ticket)
        ctx.response.body = ticket.status();
    else
        throw new MatchmakingError("ticket not found");
}

async function getMatchDetails(
    request: MatchDetailsRequest,
    ctx: any
): Promise<void> {
    const match = await postgres.getMatch(request.matchId, request.address);
    if (match)
        ctx.response.body = match;
    else
        throw new MatchmakingError("match not found");
}

async function cancelTicket(
    request: CancelTicketRequest
): Promise<void> {
    const ticket = await postgres.getTicket(request.ticketId);
    if (!ticket)
        throw new MatchmakingError("ticket not found");
    else if (ticket.address != request.address)
        throw new MatchmakingError("ticket does not belong to the user");

    if (!await postgres.cancelTicket(ticket))
        throw new MatchmakingError("could not cancel ticket");
}

async function cancelAllTickets(
    request: BaseTicketRequest,
): Promise<void> {
    await postgres.cancelAllTickets(request.address);
}

export function log(level: any, message: string): string {
    const id = guid();
    logger.log(level, `MatchmakingService ${id} > ${message}`);
    return id;
}
