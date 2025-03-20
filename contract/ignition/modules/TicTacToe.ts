// This setup uses Hardhat Ignition to manage smart contract deployments.
// Learn more about it at https://hardhat.org/ignition

import { buildModule } from "@nomicfoundation/hardhat-ignition/modules";

const TicTacToeModule = buildModule("TicTacToeModule", (m) => {
  const ticTacToe = m.contract("TicTacToe");

  m.call(ticTacToe, "initialize", [
    "0x8c10d38b33969fa5B901978ff862d1331254405B", // alliancesGamesProof
    "0xc328ff8De8A468D090A48dE40ae8e623B0725Ed8" // default admin
  ]);

  return { ticTacToe };
});

export default TicTacToeModule;


