using AllianceGamesSdk.Common;
using AllianceGamesSdk.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Chromia;

using Buffer = Chromia.Buffer;

var MOCK = false;

var logger = Logger.Create("Server");
Log.Logger = logger;

var logic = new Logic();

Buffer? serverPrivKey = null;
if (MOCK)
{
    serverPrivKey = KeyPair.GeneratePrivKey();
    var serverSigner = SignatureProvider.Create(serverPrivKey.Value);

    var client1Signer = SignatureProvider.Create(Buffer.From("1111111111111111111111111111111111111111111111111111111111111111"));
    var client2Signer = SignatureProvider.Create(Buffer.From("2222222222222222222222222222222222222222222222222222222222222222"));

    MockEnv.SetTestMode(true);
    MockEnv.Setup(
        "TicTacToe",
        "mock-match-id",
        "{}",
        new()
        {
            new()
            {
                Host = "http://localhost:5172",
                PubKey = serverSigner.PubKey,
                Role = ParticipantRole.Main
            },
            new()
            {
                Host = "",
                PubKey = client1Signer.PubKey,
                Role = ParticipantRole.Player
            },
            new()
            {
                Host = "",
                PubKey = client2Signer.PubKey,
                Role = ParticipantRole.Player
            },
        }
    );
}

var builder = AllianceGamesServer.CreateBuilder(HttpProtocols.Http1, serverPrivKey, logger);
builder.Services.AddSingleton(_ => logic);
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // WARN: Do not apply following policies to your production.
        //       If not configured carefully, it may cause security problems.
        policy.AllowAnyMethod();
        policy.AllowAnyOrigin();
        policy.AllowAnyHeader();

        // NOTE: "grpc-status" and "grpc-message" headers are required by gRPC. so, we need expose these headers to the client.
        policy.WithExposedHeaders("grpc-status", "grpc-message");
    });
});
var app = builder.Build();

app.App.UseCors();
app.App.UseWebSockets();
app.App.UseGrpcWebSocketRequestRoutingEnabler();
app.App.UseRouting();
// NOTE: `UseGrpcWebSocketBridge` must be called after calling `UseRouting`.
app.App.UseGrpcWebSocketBridge();

app.MapGrpcService<TicTacToeService>();

await app.Run(logic.CancellationToken);

await logic.Run(async result => await app.Stop(result, logic.CancellationToken));
