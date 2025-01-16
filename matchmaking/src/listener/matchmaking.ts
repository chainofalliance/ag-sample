import config from 'config';
import { Ticket } from '../ticket.js';
import { guid, now } from '../javascript-helper.js';
import { Match } from '../match.js';
import { PostgresService } from '../postgres.js';
import { getActiveNodes, queryDappInfo } from '../services/blockchain/queries.js';
import { addSession } from '../services/blockchain/operations.js';
import { ActiveNode, DappInfo, MatchData, Participant, ParticipantRole } from '../services/blockchain/types.js';
import { NODES_NEEDED } from '../env.js';
import { logger } from '../logger.js';
import { formatter } from 'postchain-client';

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
        const nodes = await getActiveNodes();
        log('info', `Checking ${nodes.length} nodes`);

        const dappInfo = await queryDappInfo();
        if (dappInfo == undefined)
            throw new Error("Cannot query dapp info");

        let chosenNodes: ActiveNode[] = [];
        let chosenCount: number = 0;
        const nodesNeeded = NODES_NEEDED();

        nodes.sort(() => Math.random() - 0.5);
        for (let node of nodes) {
            if (await isNodeHealthy(node, dappInfo)) {
                chosenNodes.push(node);
                chosenCount++;
                log('info', `Node ${node.address.toString('hex')} is healthy`);

                if (chosenCount == nodesNeeded)
                    break;
            } else {
                log('info', `Node ${node.address.toString('hex')} is not healthy`);
            }
        }

        if (chosenNodes.length < nodesNeeded) {
            log('error', `Not enough nodes available, needed ${nodesNeeded}..`);
            ticket1.close();
            ticket2?.close();
            return;
        }

        const participants: Participant[] = [];
        const matchData: MatchData[] = [];
        const mainNode: ActiveNode = chosenNodes.pop()!;

        participants.push(
            {
                address: "",
                pubkey: formatter.toBuffer(ticket1.address),
                role: ParticipantRole.PLAYER
            },
            {
                address: mainNode.url,
                pubkey: mainNode.address,
                role: ParticipantRole.MAIN
            }
        );

        chosenNodes.forEach(e => participants.push(
            {
                address: e.url,
                pubkey: e.address,
                role: ParticipantRole.OBSERVER
            }
        ));

        matchData.push({
            Id: ticket1.address
        });

        if (ticket2) {
            participants.push(
                {
                    address: "",
                    pubkey: formatter.toBuffer(ticket2.address),
                    role: ParticipantRole.PLAYER
                }
            );
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

async function isNodeHealthy(node: ActiveNode, info: DappInfo) {
    try {
        const headers: Headers = new Headers();
        headers.set('Content-Type', 'application/json');
        headers.set('Accept', 'application/json');

        const request: RequestInfo = new Request(`${node.url}/status`, {
            method: 'POST',
            headers: headers,
            body: JSON.stringify({
                uid: info.uid,
                version: info.version
            })
        })

        const response = await fetch(request);

        if (response.status == 200) {
            const json = await response.json();
            return !json.error;
        }

        return false;
    } catch (error) {
        log('debug', `Failed to fetch ${node.url} ${error}`);
        return false;
    }
}
