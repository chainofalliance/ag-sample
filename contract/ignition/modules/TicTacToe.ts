// This setup uses Hardhat Ignition to manage smart contract deployments.
// Learn more about it at https://hardhat.org/ignition

import { buildModule } from "@nomicfoundation/hardhat-ignition/modules";

const TicTacToeModule = buildModule("TicTacToeModule", (m) => {
  const ticTacToe = m.contract("TicTacToe", [
    "0x0d5Af8A0F885F6E9e573cCa9b67fbE69a98B06eE", // validator
    "9E32CFBBA7C62BAA311D110C3212749136EF4BFC0595078738FEE2BF357692BD", // rid
    "0xc328ff8De8A468D090A48dE40ae8e623B0725Ed8" // default admin
  ]
);
  return { ticTacToe };
});

export default TicTacToeModule;
