// SPDX-License-Identifier: MIT
pragma solidity ^0.8.28;

import "@openzeppelin/contracts-upgradeable/proxy/utils/Initializable.sol";
import "@openzeppelin/contracts-upgradeable/access/AccessControlUpgradeable.sol";
import "@openzeppelin/contracts-upgradeable/utils/PausableUpgradeable.sol";
import "@openzeppelin/contracts-upgradeable/utils/ReentrancyGuardUpgradeable.sol";

import "./Postchain.sol";
import "./IValidator.sol";

contract TicTacToe is Initializable, PausableUpgradeable, AccessControlUpgradeable, ReentrancyGuardUpgradeable {
    using Postchain for bytes32;
    using MerkleProof for bytes32[];

    bytes32 public constant GAME_CONTROL = keccak256("GAME_CONTROL");
    bytes32 public constant PAUSER_ROLE = keccak256("PAUSER_ROLE");

    struct RewardEvent {
        uint256 serialNumber;
        bytes32 rewardHash;
    }

    struct GameResult {
        address pubkey;
        string session_id;
        string opponent;
        uint256 points;
        uint8 outcome;
    }

    struct ClaimData {
        bytes eventData;
        Data.Proof eventProof;
        bytes blockHeader;
        bytes[] signatures;
        address[] signers;
        Data.ExtraProofData extraProof;
        bytes encodedData;
    }

    IValidator public validator;
    bytes32 internal blockchainRid;
    mapping(bytes32 => address[]) internal _eventClaimers;
    mapping(address => uint256) internal _points;

    event Initialize(IValidator indexed _validator);
    event SetBlockchainRid(bytes32 rid);
    event RewardClaimed(bytes32 indexed hash, bytes32 eventHash, address indexed claimer);

    function initialize(IValidator _validator, bytes32 rid, address _defaultAdmin) public initializer {
        require(address(_validator) != address(0), "TicTacToe: validator address is invalid");
        __AccessControl_init();
        __Pausable_init();
        __ReentrancyGuard_init();
    
        _grantRole(DEFAULT_ADMIN_ROLE, _defaultAdmin);
        _grantRole(GAME_CONTROL, _defaultAdmin);
        _grantRole(PAUSER_ROLE, _defaultAdmin);

        validator = _validator;
        blockchainRid = rid;
        emit Initialize(_validator);
    }

    function setBlockchainRid(bytes32 rid) public onlyRole(GAME_CONTROL) {
        require(rid != bytes32(0), "TicTacToe: blockchain rid is invalid");
        blockchainRid = rid;
        emit SetBlockchainRid(rid);
    }

    function pause() external onlyRole(PAUSER_ROLE) {
        _pause();
    }

    function unpause() external onlyRole(PAUSER_ROLE) {
        _unpause();
    }

    function batchClaim(ClaimData[] memory claims) external {
        require(blockchainRid != bytes32(0), "TicTacToe: blockchain rid is not set");

        for (uint i = 0; i < claims.length; i++) {
            ClaimData memory claim = claims[i];

            addClaimer(claim.eventProof.leaf, msg.sender);

            require(Hash.hashGtvBytes64Leaf(claim.extraProof.leaf) == claim.extraProof.hashedLeaf, "Postchain: invalid EIF extra data");
            (, bytes32 blockRid) = Postchain.verifyBlockHeader(blockchainRid, claim.blockHeader, claim.extraProof);
            bytes32 eventRoot = _bytesToBytes32(claim.extraProof.leaf, 0);
            
            if (!validator.isValidSignatures(blockRid, claim.signatures, claim.signers)) revert("TicTacToe: block signature is invalid");
            if (!MerkleProof.verify(claim.eventProof.merkleProofs, claim.eventProof.leaf, claim.eventProof.position, eventRoot)) revert("TicTacToe: invalid merkle proof");
            
            RewardEvent memory evt = abi.decode(claim.eventData, (RewardEvent));
            require(keccak256(claim.eventData) == claim.eventProof.leaf, "Postchain: invalid event");
            require(keccak256(claim.encodedData) == evt.rewardHash, "TicTacToe: invalid reward hash");

            GameResult[] memory grs = abi.decode(claim.encodedData, (GameResult[]));
            bool found = false;
            for (uint j = 0; j < grs.length; j++) {
                if(grs[j].pubkey == msg.sender) {
                    _points[grs[j].pubkey] += grs[j].points;
                    found = true;
                }
            }
            require(found, "TicTacToe: invalid claimer");
            
            emit RewardClaimed(evt.rewardHash, claim.eventProof.leaf, msg.sender);
        }
    }

    function addClaimer(bytes32 eventHash, address claimer) internal {
        for (uint i = 0; i < _eventClaimers[eventHash].length; i++) {
            require(_eventClaimers[eventHash][i] != claimer, "Address already added");
        }
        _eventClaimers[eventHash].push(claimer);
    }

    function getPoints(address pubkey) external view returns (uint256) {
        return _points[pubkey];
    }

    function _bytesToBytes32(bytes memory b, uint offset) internal pure returns (bytes32) {
        bytes32 out;

        for (uint i = 0; i < 32; i++) {
            out |= bytes32(b[offset + i] & 0xFF) >> (i * 8);
        }
        return out;
    }
}