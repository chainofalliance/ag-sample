import { createInMemoryFtKeyStore, createKeyStoreInteractor, Session } from "@chromia/ft4";
import { IClient, createClient } from "postchain-client";
import config from 'config';
import { getProvider } from "./provider";
import { DappInfo } from "./types";
import { queryDappInfo } from "./queries";

let client: IClient | null = null
let session: Session | null = null;
let dappInfo: DappInfo | null = null;

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

export async function getSession() {
    if (!session) {

        const client = await getClient();

        const keyStore = createInMemoryFtKeyStore(getProvider());
        const { getAccounts, getSession } = createKeyStoreInteractor(client, keyStore);
        const accountsData = await getAccounts();
        const account = accountsData[0];

        if (account == undefined || account == null)
            throw new Error("Account not registered for KeyPair");

        session = await getSession(account.id);
    }

    return session;
}

export async function getDappInfo() {
    if (!dappInfo) {
        const version = await queryDappInfo();

        if (version == undefined || version == null)
            throw new Error("Cannot query active version");

        dappInfo = version;
    }

    return dappInfo;
}