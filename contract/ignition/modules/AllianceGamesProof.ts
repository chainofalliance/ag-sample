// This setup uses Hardhat Ignition to manage smart contract deployments.
// Learn more about it at https://hardhat.org/ignition

import { buildModule } from "@nomicfoundation/hardhat-ignition/modules";

const AllianceGamesProofModule = buildModule("AllianceGamesProofModule", (m) => {
  const allianceGamesProof = m.contract("AllianceGamesProof");

  m.call(allianceGamesProof, "initialize", [
    "0x91c08e907b0aA3AbcB1280112A3a5C49D11B2006", // validator
    "0x63F766110ED31818038A323D849ECBA64E85ABA5104E6D7F24014CEF2F0756A5", // rid
    "0xc328ff8De8A468D090A48dE40ae8e623B0725Ed8" // default admin
  ]);

  return { allianceGamesProof };
});

export default AllianceGamesProofModule;


