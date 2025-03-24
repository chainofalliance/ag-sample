import { ethers } from "hardhat";

async function main () {
    const address = '0x91c08e907b0aA3AbcB1280112A3a5C49D11B2006';
    const validator = await ethers.getContractAt('Validator', address);
    const tx = await validator.updateValidators([
        '0x19d2f0a38fc8019f807bbf6fa05f1e6e3655a5e8',
        '0x2b6fa98b7e2a9afea0506cf48736a570ce7303cc',
        '0xae98c8eb8de7f5445d2b4d75fed81df56aa6efb4',
        "0xc3c425f3b64e28f8c777460d149b6c78f2d43b1b"
    ]);
    await tx.wait();
}
  
  main()
    .then(() => process.exit(0))
    .catch(error => {
      console.error(error); 
      process.exit(1);
    });