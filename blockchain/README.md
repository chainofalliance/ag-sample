# Alliance Games on Chromia blockchain

## Install dependencies
- `make install` runs the install script to install all libs defined in modules.json

## Run

- `make test` runs all Rell unit tests
- `make MODULES=module1_tests,module2_tests test` runs the Rell unit tests in the specified modules
- `make ENV=local start` starts the project locally in a docker container and exposes the blockchain on port 7740. Env can be [local, dev, staging, prod], default is local.
- `make ENV=local update` deploys the new config after 5 blocks
- `make ENV=local restart` stops clears and deletes the container, and starts it in provided env
- `make stop` stops the container and keeps the blockchain data
- `make clear` deletes the blockchain data

If you omit to set ENV, the default, i.e. local, is set.
