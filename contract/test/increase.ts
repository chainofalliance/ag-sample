import { ethers } from "hardhat";

async function main () {
    const address = '0x79154DF67f15A0865a65B0675F146c37B3e74230';
    const counter = await ethers.getContractAt('Counter', address);
    const tx = await counter.inc();
    await tx.wait();
    const result = await counter.get();
    console.log(result);
}
  
  main()
    .then(() => process.exit(0))
    .catch(error => {
      console.error(error); 
      process.exit(1);
    });