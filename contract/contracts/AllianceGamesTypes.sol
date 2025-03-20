// SPDX-License-Identifier: GPL-3.0-only
pragma solidity ^0.8.28;

import "./Data.sol";

library AllianceGamesTypes {
    struct ProofData {
        bytes eventData;
        Data.Proof eventProof;
        bytes blockHeader;
        bytes[] signatures;
        address[] signers;
        Data.ExtraProofData extraProof;
        bytes encodedData;
    }
}