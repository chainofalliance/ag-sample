using AllianceGamesSdk.Common;
using AllianceGamesSdk.Server;
using Chromia;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Buffer = Chromia.Buffer;

var logger = Logger.Create("Server");
Log.Logger = logger;

var sessionId = "mock-match-id";
var serverPrivKey = KeyPair.GeneratePrivKey();
var serverSigner = SignatureProvider.Create(serverPrivKey);
var client1Signer = SignatureProvider.Create(Buffer.From("1111111111111111111111111111111111111111111111111111111111111111"));
var client2Signer = SignatureProvider.Create(Buffer.From("2222222222222222222222222222222222222222222222222222222222222222"));
//var bcInfo = new BlockchainInfo() { Url = "http://localhost:7740", ChainId = 0 };
var participants = new object[]
{
    new Participant { PubKey = serverSigner.PubKey.Parse(), Role = ParticipantRole.Main },
    new Participant { PubKey = client1Signer.PubKey.Parse(), Role = ParticipantRole.Client },
    new Participant { PubKey = client2Signer.PubKey.Parse(), Role = ParticipantRole.Client }
};
Environment.SetEnvironmentVariable("TEST_MODE", "true");
Environment.SetEnvironmentVariable("DAPP_NAME", "Chain of Alliance");
Environment.SetEnvironmentVariable("SESSION_ID", sessionId);
Environment.SetEnvironmentVariable("PRIVATE_KEY", serverPrivKey.Parse());
//Environment.SetEnvironmentVariable("POSTCHAIN", JsonConvert.SerializeObject(bcInfo));
Environment.SetEnvironmentVariable("PARTICIPANTS", ChromiaClient.EncodeToGtv(participants).Parse());
Environment.SetEnvironmentVariable("MATCH_DATA", "");

var logic = new Logic();

var builder = AllianceGamesServer.CreateBuilder(HttpProtocols.Http1,logger);
builder.Services.AddSingleton(_ => logic);

var app = builder.Build();
app.App.UseWebSockets();
app.App.UseGrpcWebSocketRequestRoutingEnabler();
app.App.UseRouting();
// NOTE: `UseGrpcWebSocketBridge` must be called after calling `UseRouting`.
app.App.UseGrpcWebSocketBridge();

app.MapGrpcService<TicTacToeService>();

await app.Run(logic.CancellationToken);

await logic.Run(async result => await app.Stop(result, logic.CancellationToken));
