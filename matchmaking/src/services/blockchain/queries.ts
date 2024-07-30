import { DAPP_NAME, DAPP_VERSION } from "../../env";
import { guid } from "../../javascript-helper";
import { logger } from "../../logger";
import { getClient } from "./postchain";
import { ActiveNode } from "./types";

export async function getActiveNodes(): Promise<ActiveNode[]> {
    try {
        const client = await getClient();
        return await client.query<ActiveNode[]>('ag.INodeProvider.get_active_nodes_by_dapp', { 'name': DAPP_NAME, 'version': DAPP_VERSION })
    } catch (e: unknown) {
        log("error", `API error: ${(e as Error).message}`);
    }

    return [];
}

function log(level: any, message: string): string {
    var id = guid();
    logger.log(level, `Postchain ${id} > ${message}`);
    return id;
}
