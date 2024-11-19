import { IClient, createClient } from "postchain-client";
import config from 'config';

let client: IClient | null = null

export async function getClient() {
    if (!client) {
        const nodeUrl: string = config.get(`network.postchain.url`);

        if (config.has(`network.postchain.chainId`)) {
            const chainId = await config.get(`network.postchain.chainId`);

            client = await createClient({
                nodeUrlPool: nodeUrl,
                blockchainIid: chainId as number
            })

        } else if (config.has(`network.postchain.brid`)) {
            const blockchainRid = config.get(`network.postchain.brid`);

            client = await createClient({
                nodeUrlPool: nodeUrl,
                blockchainRid: blockchainRid as string
            })

        } else {
            throw new Error("brid and chainId not set for env");
        }
    }

    return client
}