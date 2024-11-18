using AllianceGamesSdk.Common;
using AllianceGamesSdk.Common.Transport.WebSocket;
using AllianceGamesSdk.Server;
using Serilog;

var logger = Logger.Create("Server");
Log.Logger = logger;

var server = await AllianceGamesServer.Create(
    new WebSocketTransport(logger)
);

var logic = new Logic(server);
logic.OnGameEnd += server.Stop;
await logic.Run();
