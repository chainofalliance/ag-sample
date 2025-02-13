// This setup uses Hardhat Ignition to manage smart contract deployments.
// Learn more about it at https://hardhat.org/ignition

import { buildModule } from "@nomicfoundation/hardhat-ignition/modules";

const ValidatorModule = buildModule("ValidatorModule", (m) => {
  const validator = m.contract("Validator", [
    ["0x02294995722c05902622ef5b118600ecd69bc807", "0x153277f9fc39138d1464a6e6b5c56e8435729afa", "0xb40516c825a6e2844ec78c7de360eef9817c659b", "0xf2e3579f99be9de376e8f9f4371a29b6e0ff148b"]
]);

  return { validator };
});

export default ValidatorModule;


