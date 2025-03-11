ROOTDIR=./rell
CONFDIR=${ROOTDIR}/config
PROJECT=ttt_chromia
TEST_PROJECT=${PROJECT}_test
DEVNET_PROJECT=${PROJECT}_devnet
ENV?=local
export CHR_CLI_VERSION := 0.23.0
export PMC_VERSION := 3.30.0
VERSION_BUMP?=

devnet_brid := $(shell curl -s 'https://node0.devnet1.chromia.dev:7740/brid/iid_0')


.PHONY: start
start: setenv
	docker compose -p ${PROJECT} up blockchain -d

.PHONY: devnet
devnet: setenv install
	docker compose -p ${DEVNET_PROJECT} -f docker-compose-devnet.yml up blockchain -d

.PHONY: devnet-restart
devnet-restart: devnet-stop devnet

.PHONY: devnet-stop
devnet-stop: setenv
	docker compose -p ${DEVNET_PROJECT} down

.PHONY: devnet-update
devnet-update: setenv
	docker compose -p ${DEVNET_PROJECT} -f docker-compose-devnet.yml run --rm blockchain_update

.PHONY: test
test: setenv
	docker compose -p ${TEST_PROJECT} -f docker-compose-test.yml up \
		--abort-on-container-exit

.PHONY: update
update: setenv
	docker compose -p ${PROJECT} run --rm blockchain_update

.PHONY: restart
restart: stop clear start

.PHONY: clear
clear:
	docker compose -p ${PROJECT} down -v

.PHONY: stop
stop:
	docker compose -p ${PROJECT} down

.PHONY: setenv
setenv:
	${CONFDIR}/generate_config.sh ${ROOTDIR} ${CONFDIR} ${ENV} ${MODULES}

.PHONY: install
install: setenv
	docker run --rm -v ${PWD}/rell:/usr/app \
	registry.gitlab.com/chromaway/core-tools/chromia-cli/chr:${CHR_CLI_VERSION} chr install

.PHONY: create-container
create-container:
	docker run --rm -v ${PWD}/rell/config:/usr/app \
	-w /usr/app --entrypoint sh -e DEVNET_BRID=${devnet_brid} -e CREATE_PROVIDER=${CREATE_PROVIDER} \
	registry.gitlab.com/chromaway/core-tools/management-console/pmc:${PMC_VERSION} register-container.sh

PHONY: deploy-testnet
deploy-testnet: setenv
	docker run -it --rm -v ${PWD}/rell:/usr/app \
	registry.gitlab.com/chromaway/core-tools/chromia-cli/chr:${CHR_CLI_VERSION} chr deployment create \
	--network testnet \
	--blockchain TicTacToe \
	--secret /usr/app/config/.secret_${ENV}

PHONY: update-testnet
update-testnet: setenv
	docker run -it --rm -v ${PWD}/rell:/usr/app \
	registry.gitlab.com/chromaway/core-tools/chromia-cli/chr:${CHR_CLI_VERSION} chr deployment update \
	--network testnet \
	--blockchain TicTacToe \
	--secret /usr/app/config/.secret_${ENV}

PHONY: info-testnet
info-testnet: setenv
	docker run -it --rm -v ${PWD}/rell:/usr/app \
	registry.gitlab.com/chromaway/core-tools/chromia-cli/chr:${CHR_CLI_VERSION} chr deployment info \
	--network testnet \
	--blockchain TicTacToe

PHONY: remove-testnet
remove-testnet: setenv
	docker run -it --rm -v ${PWD}/rell:/usr/app \
	registry.gitlab.com/chromaway/core-tools/chromia-cli/chr:${CHR_CLI_VERSION} chr deployment remove \
	--network testnet \
	--blockchain TicTacToe \
	--secret /usr/app/config/.secret_${ENV}
