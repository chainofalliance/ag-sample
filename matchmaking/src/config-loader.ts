import path from 'path';
import * as cli from './env.js';
import config from 'config'
import { readFileSync } from 'fs';

export function load() {
    var network = cli.NETWORK;

    if (network) {
        var configPath = path.join(process.cwd(), '/config/', network);
        const data = readFileSync(`${configPath}.json`, 'utf8');

        config.util.extendDeep(config, JSON.parse(data))
    }
}