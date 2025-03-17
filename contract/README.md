# Sample Hardhat Project

This project demonstrates a basic Hardhat use case. It comes with a sample contract, a test for that contract, and a Hardhat Ignition module that deploys that contract.

Try running some of the following tasks:

```shell
npx hardhat help
npx hardhat test
REPORT_GAS=true npx hardhat test
npx hardhat node
npx hardhat ignition deploy ./ignition/modules/Lock.ts
```

```shell
npx hardhat clean
npx hardhat compile
npx hardhat ignition deploy ignition/modules/Validator.ts --network testnet --verify
npx hardhat ignition deploy ignition/modules/TicTacToe.ts --network testnet --verify
npx hardhat vars set BNB_PRIVATE_KEY
npx hardhat vars set BSCSCAN_API_KEY
npx hardhat run --network testnet ./scripts/update-validators.ts
```

```shell
#Local deployment
npx hardhat node
npx hardhat clean
npx hardhat compile
npx hardhat ignition deploy ./ignition/modules/Validator.ts --network localhost
npx hardhat ignition deploy ./ignition/modules/TicTacToe.ts --network localhost
```