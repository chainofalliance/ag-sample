import { logger } from '../logger.js';
import { CreateTicketRequest } from '../services/requests.js';

createTestTicket();

async function createTestTicket() {
    let payload: CreateTicketRequest = {
        address: "02f0fde2f86f3bec8a032072c543281c71596dfbe0564002286ef4e00d153652c1",
    }
    let response = await fetch("http://localhost:9085/create-ticket", {
        method: 'post',
        body: JSON.stringify(payload),
        headers: {
            'Content-Type': 'application/json',
        },
    })
    console.log(await response.json());

    // var s = await Postgres.connect("prod");
    // // for (let i = 0; i < 10; i++)
    //     await s.addTicket(new Ticket(
    //         "18A72DD00D08B0589200F53B34FE0544DE8C93148BD50207A560F147CF2311D9",
    //         "f96c1194-0f19-4abd-badd-2dbc10200dc1",
    //         3.3,
    //         []
    //     ));
    // var t = await s.deleteTicket("4bebe139-a516-4815-bde3-c45cee06de11");
    // console.log(t);
    // console.log(t?.accountId);
    // console.log(t?.canMatch(t));
    // console.log(t?.checkFallback());
    // Postgres.cleanup();
}

export function log(level: any, message: string) {
    logger.log(level, `CreateTicketTest > ${message}`);
}
