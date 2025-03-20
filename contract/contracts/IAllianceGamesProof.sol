// SPDX-License-Identifier: GPL-3.0-only
pragma solidity ^0.8.28;

import "./AllianceGamesTypes.sol";

interface IAllianceGamesProof {
    function isProofValid(AllianceGamesTypes.ProofData calldata proof) external view returns (bool);
}
