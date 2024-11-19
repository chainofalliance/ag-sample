import * as matchmakingListener from '../listener/matchmaking.js';
import * as matchmakingService from '../services/matchmaking.js';
import * as configLoader from './../config-loader.js';
import { PostgresService } from '../postgres.js';
import { NETWORK, ipAddress } from '../env.js';
import bodyParser from 'koa-bodyparser';
import { logger } from '../logger.js';
import Router from '@koa/router';
import cors from '@koa/cors';
import Koa from 'koa';

if (require.main === module) {
    configLoader.load();
    startup();
}

const app = new Koa();
const router = new Router();

async function startup() {
    const network = NETWORK!;
    const postgres = await PostgresService.connectMatchmaking(network);
    await postgres.closePendingTickets();
    postgres.addListener();

    matchmakingListener.init(postgres);
    matchmakingService.init(postgres);

    router.use('/', matchmakingService.router.routes(), matchmakingService.router.allowedMethods());

    app.use(cors());
    app.use(bodyParser());
    app.use(router.routes());
    app.use(router.allowedMethods());
    router.get('/healthz', ctx => {
        ctx.response.type = 'text/plain';
        ctx.response.set(
            'Cache-Control',
            'no-store',
        );
        ctx.response.body = `ok`;
    });
    app.use(ctx => {
        ctx.response.body
            = 'Not Found\n\nWant to check out https://www.ttt.com ?\n';
        ctx.response.status = 404;
    });

    const port = 8080;
    logger.log('info', `HTTP webserver running on port ${port}`);
    app.listen(port);

    logger.log('info', `Running matchmaking for ${network}`);
    matchmakingListener.run();

    if (network === 'local' || network === 'devnet')
        logger.log('info', `Server url: ${ipAddress()}`);
}
