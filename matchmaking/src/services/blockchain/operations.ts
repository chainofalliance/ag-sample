import { guid } from "../../javascript-helper";
import { logger } from "../../logger";
import { getClient } from "./postchain";
import { getProvider } from "./provider";
import { MatchData, Participant } from "./types";
import { DAPP_NAME } from "../../env";

export async function addSession(sessionId: string, participants: Participant[], matchData: MatchData[]) {
    try {
        const client = await getClient();
        client.signAndSendUniqueTransaction(
            {
                name: "ag.ISession.add",
                args: [
                    DAPP_NAME(),
                    sessionId,
                    participants.map(elem => [elem.address, elem.pubkey, elem.role as number]),
                    JSON.stringify(matchData)
                ]
            },
            getProvider()
        );
    } catch (error) {
        log("error", `API error: ${(error as Error).message}`);
    }
}

function log(level: any, message: string): string {
    var id = guid();
    logger.log(level, `Postchain ${id} > ${message}`);
    return id;
}
