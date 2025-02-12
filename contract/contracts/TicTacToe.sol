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

    IValidator public validator;
    bytes32 internal blockchainRid;
    mapping(bytes32 => bool) internal _events;
    mapping(address => uint256) internal _points;

    event Initialize(IValidator indexed _validator);
    event SetBlockchainRid(bytes32 rid);

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

    function Claim(
        bytes memory _event,
        Data.Proof memory eventProof,
        bytes memory blockHeader,
        bytes[] memory sigs,
        address[] memory signers,
        Data.ExtraProofData memory extraProof,
        bytes memory encodedData
    ) external whenNotPaused {
        require(blockchainRid != bytes32(0), "TicTacToe: blockchain rid is not set");
        require(_events[eventProof.leaf] == false, "TicTacToe: event hash was already used");

        require(Hash.hashGtvBytes64Leaf(extraProof.leaf) == extraProof.hashedLeaf, "Postchain: invalid EIF extra data");
        (, bytes32 blockRid) = Postchain.verifyBlockHeader(blockchainRid, blockHeader, extraProof);
        bytes32 eventRoot = _bytesToBytes32(extraProof.leaf, 0);
        
        if (!validator.isValidSignatures(blockRid, sigs, signers)) revert("TicTacToe: block signature is invalid");
        if (!MerkleProof.verify(eventProof.merkleProofs, eventProof.leaf, eventProof.position, eventRoot)) revert("TicTacToe: invalid merkle proof");
        
        RewardEvent memory evt = abi.decode(_event, (RewardEvent));
        require(keccak256(_event) == eventProof.leaf, "Postchain: invalid event");
        _events[eventProof.leaf] = true;

        require(keccak256(encodedData) == evt.rewardHash, "TicTacToe: invalid reward hash");

        (address pubkey, uint256 points) = abi.decode(encodedData, (address, uint256));
        require(pubkey != msg.sender, "TicTacToe: pubkey is invalid");
        _points[pubkey] += points;
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