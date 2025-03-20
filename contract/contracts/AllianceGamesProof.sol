// SPDX-License-Identifier: MIT
pragma solidity ^0.8.28;

import "@openzeppelin/contracts-upgradeable/proxy/utils/Initializable.sol";
import "@openzeppelin/contracts-upgradeable/access/AccessControlUpgradeable.sol";
import "@openzeppelin/contracts-upgradeable/utils/PausableUpgradeable.sol";
import "@openzeppelin/contracts-upgradeable/utils/ReentrancyGuardUpgradeable.sol";

import "./Postchain.sol";
import "./IValidator.sol";
import "./AllianceGamesTypes.sol";

contract AllianceGamesProof is Initializable, PausableUpgradeable, AccessControlUpgradeable, ReentrancyGuardUpgradeable {
    using Postchain for bytes32;
    using MerkleProof for bytes32[];

    bytes32 public constant PAUSER_ROLE = keccak256("PAUSER_ROLE");

    struct RewardEvent {
        uint256 serialNumber;
        bytes32 rewardHash;
    }

    IValidator public validator;
    bytes32 internal blockchainRid;

    event Initialize(IValidator indexed _validator);
    event SetBlockchainRid(bytes32 rid);

    function initialize(IValidator _validator, bytes32 rid, address _defaultAdmin) public initializer {
        require(address(_validator) != address(0), "AllianceGamesProof: validator address is invalid");
        __AccessControl_init();
        __Pausable_init();
        __ReentrancyGuard_init();
    
        _grantRole(DEFAULT_ADMIN_ROLE, _defaultAdmin);
        _grantRole(PAUSER_ROLE, _defaultAdmin);

        validator = _validator;
        blockchainRid = rid;
        emit Initialize(_validator);
    }

    function setBlockchainRid(bytes32 rid) public onlyRole(DEFAULT_ADMIN_ROLE) {
        require(rid != bytes32(0), "AllianceGamesProof: blockchain rid is invalid");
        blockchainRid = rid;
        emit SetBlockchainRid(rid);
    }

    function pause() external onlyRole(PAUSER_ROLE) {
        _pause();
    }

    function unpause() external onlyRole(PAUSER_ROLE) {
        _unpause();
    }

    function isProofValid(AllianceGamesTypes.ProofData calldata proof) external view returns (bool) {
        if (blockchainRid == bytes32(0)) return false;
        if (Hash.hashGtvBytes64Leaf(proof.extraProof.leaf) != proof.extraProof.hashedLeaf) return false;
        
        (, bytes32 blockRid) = Postchain.verifyBlockHeader(blockchainRid, proof.blockHeader, proof.extraProof);
        bytes32 eventRoot = _bytesToBytes32(proof.extraProof.leaf, 0);

        if (!validator.isValidSignatures(blockRid, proof.signatures, proof.signers)) return false;
        if (!MerkleProof.verify(proof.eventProof.merkleProofs, proof.eventProof.leaf, proof.eventProof.position, eventRoot)) return false;

        RewardEvent memory evt = abi.decode(proof.eventData, (RewardEvent));
        if (keccak256(proof.eventData) != proof.eventProof.leaf) return false;
        if (keccak256(proof.encodedData) != evt.rewardHash) return false;
        
        return true;
    }

    function _bytesToBytes32(bytes memory b, uint offset) internal pure returns (bytes32) {
        bytes32 out;

        for (uint i = 0; i < 32; i++) {
            out |= bytes32(b[offset + i] & 0xFF) >> (i * 8);
        }
        return out;
    }
}