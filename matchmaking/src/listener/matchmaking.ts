import config from 'config';
import { Ticket } from '../ticket.js';
import { guid, now } from '../javascript-helper.js';
import { Match } from '../match.js';
import { PostgresService } from '../postgres.js';
import { getActiveNodes } from '../services/blockchain/queries.js';
import { addSession } from '../services/blockchain/operations.js';
import { ActiveNode, MatchData, Participant, ParticipantRole } from '../services/blockchain/types.js';
import { NODES_NEEDED } from '../env.js';
import { logger } from '../logger.js';

let postgres: PostgresService;

export function init(
    postgresService: PostgresService
) {
    postgres = postgresService;
}

export async function run() {
    const MATCH_FREQUENCY: number = config.get(`common.matchmaking.match_frequency`) as number * 1000;

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

        // if (ticket.checkFallback()) {
        //     log('info', `Start AI match for ${ticket.id}`);
        //     resolve(ticket);
        // }
    }
}

async function resolve(ticket1: Ticket, ticket2: Ticket | null = null) {
    try {
        ticket1.waitForServer();
        ticket2?.waitForServer();
        const sessionId = guid();
        const nodes = await getActiveNodes();
        log('info', `Checking ${nodes.length} nodes`);

        let chosenNodes: ActiveNode[] = [];
        let chosenCount: number = 0;

        nodes.sort(() => Math.random() - 0.5);
        for (let node of nodes) {
            if (await isNodeHealthy(node)) {
                chosenNodes.push(node);
                chosenCount++;
                log('info', `Node ${node.address.toString('hex')} is healthy`);

                if (chosenCount == NODES_NEEDED)
                    break;
            } else {
                log('info', `Node ${node.address.toString('hex')} is not healthy`);
            }
        }

        if (chosenNodes.length < NODES_NEEDED) {
            log('error', `Not enough nodes available, needed ${NODES_NEEDED}..`);
            ticket1.close();
            ticket2?.close();
            return;
        }

        const participants: Participant[] = [];
        const matchData: MatchData[] = [];
        const mainNode: ActiveNode = chosenNodes.pop()!;

        participants.push(
            [ticket1.address, ParticipantRole.PLAYER],
            [mainNode.address.toString('hex'), ParticipantRole.MAIN]
        );

        chosenNodes.forEach(e => participants.push([e.address.toString('hex'), ParticipantRole.OBSERVER]))

        matchData.push({
            Id: ticket1.address
        });

        if (ticket2) {
            participants.push([ticket2.address, ParticipantRole.PLAYER]);
            matchData.push({
                Id: ticket2.address
            });
        }

        await addSession(sessionId, participants, matchData);

        ticket1.match(sessionId);
        ticket2?.match(sessionId);

        let match: Match = {
            id: sessionId,
            isAI: ticket2 == null,
            serverDetails: mainNode.url,
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

async function isNodeHealthy(node: ActiveNode) {
    const url = `${node.url}/connect`;
    const nodeAddress = node.address.toString('hex');
    try {
        const response = await fetch(url);

        if (response.status == 200) {
            const json = await response.json();
            return json.address == nodeAddress;
        }

        return false;
    } catch (error) {
        log('debug', `Failed to fetch ${url} ${error}`);
        return false;
    }
}
