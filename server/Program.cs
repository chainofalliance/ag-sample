using AllianceGamesSdk.Common;
using AllianceGamesSdk.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var logger = Logger.Create("Server");
Log.Logger = logger;

var logic = new Logic();

var builder = AllianceGamesServer.CreateBuilder(HttpProtocols.Http1, logger);
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
