using AllianceGames.Sample.TicTacToe.Grpc;
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
}
