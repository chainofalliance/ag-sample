import { guid } from "../../javascript-helper";
import { logger } from "../../logger";
import { getClient } from "./postchain";

export async function queryConnectionDetails(sessionId: string): Promise<string | undefined> {
    try {
        const client = await getClient();
        return await client.query<string>('ag.ISession.get_connection_details', { session_id: sessionId });

    } catch (e: unknown) {
        log("error", `API error: ${(e as Error).message}`);
        return undefined;
    }
}

function log(level: any, message: string): string {
    var id = guid();
    logger.log(level, `Postchain ${id} > ${message}`);
    return id;
}
