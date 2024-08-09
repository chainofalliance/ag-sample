PROJECT=ttt_matchmaking
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
