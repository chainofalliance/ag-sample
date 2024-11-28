import { DAPP_NAME } from "../../env";
import { guid } from "../../javascript-helper";
import { logger } from "../../logger";
import { getClient } from "./postchain";
import { ActiveNode, DappInfo, Version } from "./types";

export async function getActiveNodes(): Promise<ActiveNode[]> {
    try {
        const client = await getClient();
        return await client.query<ActiveNode[]>('ag.INodeProvider.get_active_nodes')
    } catch (e: unknown) {
        log("error", `API error: ${(e as Error).message}`);
    }

    return [];
}

export async function queryDappInfo(): Promise<DappInfo | undefined> {
    try {
        const client = await getClient();
        const uid = await client.query<string>('ag.IDappProvider.get_uid', { display_name: DAPP_NAME() });
        const version = await client.query<Version>('ag.IDappProvider.get_active_version', { uid: uid });

        return {
            uid: uid,
            version: version.version
        }
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
