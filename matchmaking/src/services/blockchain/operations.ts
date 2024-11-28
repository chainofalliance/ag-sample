import { guid } from "../../javascript-helper";
import { logger } from "../../logger";
import { getSession } from "./postchain";
import { MatchData, Participant } from "./types";
import { DAPP_NAME } from "../../env";

export async function addSession(sessionId: string, participants: Participant[], matchData: MatchData[]) {
    try {
        const session = await getSession();
        var uid = await session.query("ag.IDappProvider.get_uid", { display_name: DAPP_NAME() });
        const parti = participants.map(elem => [elem.address, elem.pubkey, elem.role as number]);
        console.log("addSession with: " + parti);
        session.call(
            {
                name: "ag.ISession.add",
                args: [
                    String(uid),
                    sessionId,
                    parti,
                    JSON.stringify(matchData)
                ]
            }
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
