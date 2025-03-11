// This setup uses Hardhat Ignition to manage smart contract deployments.
// Learn more about it at https://hardhat.org/ignition

import { buildModule } from "@nomicfoundation/hardhat-ignition/modules";

const ValidatorModule = buildModule("ValidatorModule", (m) => {
  const validator = m.contract("Validator", [
    ["0x19d2f0a38fc8019f807bbf6fa05f1e6e3655a5e8", "0xae98c8eb8de7f5445d2b4d75fed81df56aa6efb4", "0xc3c425f3b64e28f8c777460d149b6c78f2d43b1b"]
]);

  return { validator };
});

export default ValidatorModule;


