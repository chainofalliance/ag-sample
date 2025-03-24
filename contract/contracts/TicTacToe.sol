// SPDX-License-Identifier: MIT
pragma solidity ^0.8.28;

import "@openzeppelin/contracts-upgradeable/proxy/utils/Initializable.sol";
import "@openzeppelin/contracts-upgradeable/access/AccessControlUpgradeable.sol";
import "@openzeppelin/contracts-upgradeable/utils/PausableUpgradeable.sol";
import "@openzeppelin/contracts-upgradeable/utils/ReentrancyGuardUpgradeable.sol";

import "./IAllianceGamesProof.sol";

contract TicTacToe is Initializable, PausableUpgradeable, AccessControlUpgradeable, ReentrancyGuardUpgradeable {
    bytes32 public constant GAME_CONTROL = keccak256("GAME_CONTROL");
    bytes32 public constant PAUSER_ROLE = keccak256("PAUSER_ROLE");

    IAllianceGamesProof public allianceGamesProof;

    struct GameResult {
        address pubkey;
        string session_id;
        string opponent;
        uint256 points;
        uint8 outcome;
    }

    mapping(bytes32 => address[]) internal _eventClaimers;
    mapping(address => uint256) internal _points;

    event RewardClaimed(bytes32 eventHash, address indexed claimer);

    function initialize(IAllianceGamesProof _allianceGamesProof, address _defaultAdmin) public initializer {
        require(address(_allianceGamesProof) != address(0), "TicTacToe: allianceGamesProof address is invalid");
        
        __AccessControl_init();
        __Pausable_init();
        __ReentrancyGuard_init();
    
        _grantRole(DEFAULT_ADMIN_ROLE, _defaultAdmin);
        _grantRole(GAME_CONTROL, _defaultAdmin);
        _grantRole(PAUSER_ROLE, _defaultAdmin);

        allianceGamesProof = _allianceGamesProof;
    }

    function pause() external onlyRole(PAUSER_ROLE) {
        _pause();
    }

    function unpause() external onlyRole(PAUSER_ROLE) {
        _unpause();
    }

    function batchClaim(AllianceGamesTypes.ProofData[] memory claims) external {
        for (uint i = 0; i < claims.length; i++) {
            AllianceGamesTypes.ProofData memory claim = claims[i];

            addClaimer(claim.eventProof.leaf, msg.sender);

            if (!allianceGamesProof.isProofValid(claim)) {
                revert("TicTacToe: invalid proof");
            }

            GameResult[] memory grs = abi.decode(claim.encodedData, (GameResult[]));
            bool found = false;
            for (uint j = 0; j < grs.length; j++) {
                if(grs[j].pubkey == msg.sender) {
                    _points[grs[j].pubkey] += grs[j].points;
                    found = true;
                }
            }
            require(found, "TicTacToe: invalid claimer");
            
            emit RewardClaimed(claim.eventProof.leaf, msg.sender);
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

    function getClaimStatus(bytes32[] memory eventHashes, address claimer) external view returns (bool[] memory) {
        bool[] memory claimsStatus = new bool[](eventHashes.length);
        
        for (uint i = 0; i < eventHashes.length; i++) {
            claimsStatus[i] = false; // Default to not claimed
            for (uint j = 0; j < _eventClaimers[eventHashes[i]].length; j++) {
                if (_eventClaimers[eventHashes[i]][j] == claimer) {
                    claimsStatus[i] = true; // Claimed
                }
            }
        }
        
        return claimsStatus;
    }
}