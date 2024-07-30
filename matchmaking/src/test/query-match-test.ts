import { logger } from '../logger.js';
import { MatchDetailsRequest } from '../services/requests.js';

queryTestMatch();

async function queryTestMatch() {
    let payload: MatchDetailsRequest = {
        address: "02f0fde2f86f3bec8a032072c543281c71596dfbe0564002286ef4e00d153652c1",
        matchId: process.argv[2]!
    }

    let response = await fetch("http://localhost:8090/match-details", {
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
