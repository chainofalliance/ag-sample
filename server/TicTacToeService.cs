using AllianceGames.Sample.TicTacToe.Grpc;
using AllianceGamesSdk;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

internal class TicTacToeService : AllianceGames.Sample.TicTacToe.Grpc.TicTacToeService.TicTacToeServiceBase
{
    private Logic logic;
    public TicTacToeService(Logic logic)
    {
        this.logic = logic;
    }

    public override Task ServerRequests(IAsyncStreamReader<Response> requestStream, IServerStreamWriter<Request> responseStream, ServerCallContext context)
    {
        return logic.ServerRequests(requestStream, responseStream, context);
    }

    public override async Task GetPlayerData(Empty request, IServerStreamWriter<PlayerData> responseStream, ServerCallContext context)
    {
        await TaskUtil.WaitWhile(() => logic.Players.Count == 2);

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
