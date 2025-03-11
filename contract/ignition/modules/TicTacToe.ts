// This setup uses Hardhat Ignition to manage smart contract deployments.
// Learn more about it at https://hardhat.org/ignition

import { buildModule } from "@nomicfoundation/hardhat-ignition/modules";

const TicTacToeModule = buildModule("TicTacToeModule", (m) => {
  const ticTacToe = m.contract("TicTacToe");

  m.call(ticTacToe, "initialize", [
    "0x5FbDB2315678afecb367f032d93F642f64180aa3", // validator
    "0xF5EF9E34BBBD4AB847A7740589A5525739CC88A1F1E888585E96BE04833BAE10", // rid
    "0xc328ff8De8A468D090A48dE40ae8e623B0725Ed8" // default admin
  ]);

  return { ticTacToe };
});

export default TicTacToeModule;


