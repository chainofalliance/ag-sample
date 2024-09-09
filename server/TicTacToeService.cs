using AllianceGames.Sample.TicTacToe.Grpc;
using AllianceGamesSdk;
using AllianceGamesSdk.Common;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

internal class TicTacToeService : AllianceGames.Sample.TicTacToe.Grpc.TicTacToeService.TicTacToeServiceBase
{
    private readonly Logic logic;
    public TicTacToeService(Logic logic)
    {
        this.logic = logic;
    }

    public override async Task<Empty> Forfeit(Empty request, ServerCallContext context)
    {
        await logic.Forfeit(context.GetAddress());
        return new Empty();
    }

    public override Task ServerRequests(IAsyncStreamReader<Response> requestStream, IServerStreamWriter<Request> responseStream, ServerCallContext context)
    {
        return logic.ServerRequests(requestStream, responseStream, context);
    }

    public override async Task GetPlayerData(Empty request, IServerStreamWriter<PlayerData> responseStream, ServerCallContext context)
    {
        await TaskUtil.WaitUntil(() => logic.Players.Count == 2);

        foreach (var player in logic.Players.Values)
        {
            var playerData = new PlayerData
            {
                Address = player.Address,
                HasX = player.Symbol == Logic.Field.X,
            };
            await responseStream.WriteAsync(playerData);
        }
    }
}
