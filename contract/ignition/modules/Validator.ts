// This setup uses Hardhat Ignition to manage smart contract deployments.
// Learn more about it at https://hardhat.org/ignition

import { buildModule } from "@nomicfoundation/hardhat-ignition/modules";

const ValidatorModule = buildModule("ValidatorModule", (m) => {
  const validator = m.contract("Validator", [
    ["0x17c5185167401ed00cf5f5b2fc97d9bbfdb7d025"]
]);

  return { validator };
});

export default ValidatorModule;


