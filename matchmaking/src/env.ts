import * as process from 'node:process';
import * as dotenv from 'dotenv';
import config from 'config';

export type Network =
    | 'devnet'
    | 'local'
    | 'prod'
    | 'staging'
    | 'dev';
export const NETWORKS: readonly Network[] = [
    'devnet',
    'local',
    'prod',
    'staging',
    'dev',
];

export interface MatchmakingQueue {
    buildAlias: string,
    enabled: boolean
}

dotenv.config();

export const NETWORK = network();
export const DAPP_PRIVATE_KEY = load('DAPP_PRIVATE_KEY');

export function DAPP_NAME() { return loadFromConfig<string>("application.dapp.name"); }
export function NODES_NEEDED() { return loadFromConfig<number>("application.consensus.node_amount"); }

function load(name: string) {
    const content = loadSilent(name);
    if (!content) {
        throw new Error('specify ENV variables or .env file for ' + name);
    }

    return content;
}

function loadFromConfig<T>(path: string) {
    const content = config.get(path) as T;
    if (!content) {
        throw new Error('specify variables in config ' + path);
    }

    return content;
}

function loadSilent(name: string) {
    return process.env[name];
}

export function network() {
    const network = process.argv[2] as Network;
    if (!network || !NETWORKS.includes(network)) {
        return null;
    }

    return network;
}

export function ipAddress() {
    return loadSilent("MATCH_IP_ADDRESS") || "localhost";
}

export function getNextPort() {
    const maxPort: number = config.get(`application.matchmaking.max_devnet_servers`);
    const currentPort = loadSilent('PORT_POSTFIX') || "0";
    const nextPort = (parseInt(currentPort) + 1) % maxPort;
    process.env['PORT_POSTFIX'] = nextPort.toString();
    return nextPort;
}
