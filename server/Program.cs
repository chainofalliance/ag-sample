using AllianceGamesSdk.Common;
using AllianceGamesSdk.Common.Transport.WebSocket;
using AllianceGamesSdk.Server;
using Chromia;
using Serilog;

using Buffer = Chromia.Buffer;

const string DEFAULT_ADMIN_PRIVKEY = "854D8402085EC5F737B1BE63FFD980981EED2A0DA5FAC6B4468CB1F176BA0321";
var privKey = Buffer.From(DEFAULT_ADMIN_PRIVKEY);

var logger = Logger.Create("Server");
Log.Logger = logger;

var signatureProvider = SignatureProvider.Create(privKey);
var config = new InjectedNodeConfig(
    "mock-match-id",
    "",
    new Node(signatureProvider.PubKey, $"http://localhost:{INodeConfig.DEFAULT_PORT}/"),
    [
        Buffer.From("02466d7fcae563e5cb09a0d1870bb580344804617879a14949cf22285f1bae3f27"),
        Buffer.From("034f355bdcb7cc0af728ef3cceb9615d90684bb5b2ca5f859ab0f0b704075871aa"),
    ],
    [],
    privKey,
    logger
);
var server = await AllianceGamesServer.Create(
    new WebSocketTransport(logger),
    config
);

var logic = new Logic(server, true);
logic.OnGameEnd += result => server.Stop(result);
await logic.Run();
