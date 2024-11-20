FROM docker.io/library/node:20.16.0-alpine AS builder
WORKDIR /build

COPY package.json package-lock.json tsconfig.json ./
RUN npm ci

COPY src src
RUN node_modules/.bin/tsc


FROM docker.io/library/node:20.16.0-alpine AS packages
WORKDIR /build
COPY package.json package-lock.json ./
RUN npm ci --production


FROM docker.io/library/node:20.16.0-alpine
RUN apk upgrade --no-cache
RUN apk add --update docker openrc bash
RUN rc-update add docker boot

ARG GIT_SHA

WORKDIR /app
VOLUME /app/persist

COPY package.json ./
COPY --from=packages /build/node_modules ./node_modules
COPY --from=builder /build/dist ./
COPY config/ ./config

ENV GIT_SHA=${GIT_SHA}

CMD node --enable-source-maps index.js
