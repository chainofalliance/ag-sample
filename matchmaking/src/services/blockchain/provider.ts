import { SignatureProvider, newSignatureProvider } from "postchain-client";
import { DAPP_PRIVATE_KEY } from "../../env";

let signatureProvider: SignatureProvider | null = null

export function getProvider() {
    if (!signatureProvider) {
        const privKey = DAPP_PRIVATE_KEY;
        signatureProvider = newSignatureProvider({ privKey: privKey });
    }

    return signatureProvider;
}
