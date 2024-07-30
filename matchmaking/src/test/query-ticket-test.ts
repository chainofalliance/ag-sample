import { logger } from '../logger.js';
import { TicketStatusRequest } from '../services/requests.js';

queryTestTicket();

async function queryTestTicket() {
    let payload: TicketStatusRequest = {
        address: "123",
        ticketId: process.argv[2]!
    }
    let response = await fetch("http://localhost:8090/ticket-status", {
        method: 'post',
        body: JSON.stringify(payload),
        headers: {
            'Content-Type': 'application/json',
        },
    })
    console.log(await response.json());
}

export function log(level: any, message: string) {
    logger.log(level, `CreateTicketTest > ${message}`);
}
