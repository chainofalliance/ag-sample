import { guid } from "../../javascript-helper";
import { logger } from "../../logger";
import { getSession } from "./postchain";
import { MatchData } from "./types";
import { DAPP_NAME } from "../../env";
import { formatter } from "postchain-client";

export async function addSession(sessionId: string, participants: Buffer[], matchData: MatchData[]) {
    try {
        const session = await getSession();
        var uid = await session.query("ag.IDappProvider.get_uid", { display_name: DAPP_NAME() });

        participants.forEach(p => {
            console.log(`PubKey: ${formatter.toString(p)}`)
        });

        await session.call(
            {
                name: "ag.IMatchmaking.add_session",
                args: [
                    String(uid),
                    sessionId,
                    participants,
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
