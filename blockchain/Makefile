ROOTDIR=./rell
CONFDIR=${ROOTDIR}/config
PROJECT=ttt_chromia
ENV?=local
export CHR_CLI_VERSION := 0.20.1

include .env
export

.PHONY: start
start: setenv
	docker compose -p ${PROJECT} up blockchain -d

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
