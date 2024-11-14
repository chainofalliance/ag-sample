using AllianceGamesSdk.Common;
using AllianceGamesSdk.Common.Transport.WebSocket;
using AllianceGamesSdk.Server;
using Serilog;

var logger = Logger.Create("Server");
Log.Logger = logger;

var server = await AllianceGamesServer.Create(
    new WebSocketTransport(logger),
    null
);
await Task.Delay(10000);
await server.Stop(null, default);

