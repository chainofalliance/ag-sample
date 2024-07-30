using AllianceGamesSdk.Server;
using Microsoft.Extensions.DependencyInjection;

var logic = new Logic();

var builder = AllianceGamesServer.CreateBuilder();
builder.Services.AddSingleton(_ => new Logic());
var app = builder.Build();
app.MapGrpcService<TicTacToeService>();

await app.Run(logic.CancellationToken);

await logic.Run(async result => await app.Stop(result, logic.CancellationToken));
