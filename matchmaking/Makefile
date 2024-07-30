PROJECT=coa_matchmaking
DEVNET_PROJECT=${PROJECT}_devnet
export NETWORK?=local

GIT_SHA=$(shell git rev-parse --short HEAD)

include .env
export

.PHONY: start
start:
	docker compose -p ${PROJECT} up -d --build --force-recreate ${SERVICE}

.PHONY: stop
stop:
	docker compose -p ${PROJECT} down

.PHONY: restart
restart: stop start

.PHONY: devnet
devnet:
	docker compose -p ${DEVNET_PROJECT} -f docker-compose-devnet.yml up --build -d

.PHONY: devnet-stop
devnet-stop:
	docker compose -p ${DEVNET_PROJECT} down

.PHONY: devnet-restart
devnet-restart: devnet-stop devnet
