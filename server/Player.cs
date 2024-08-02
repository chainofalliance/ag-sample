using AllianceGames.Sample.TicTacToe.Grpc;
using Grpc.Core;
using System.Threading.Channels;

internal class Player
{
    public readonly string Address;
    public ChannelWriter<Request> Requests => channel.Writer;

    private readonly Channel<Request> channel;
    private readonly IAsyncStreamReader<Response> requestStream;
    private readonly IServerStreamWriter<Request> responseStream;
    private readonly ChannelWriter<Response> responseChannel;

    public Logic.Field Symbol { get; private set; }

    public Player(
        string address,
        IAsyncStreamReader<Response> requestStream,
        IServerStreamWriter<Request> responseStream,
        ChannelWriter<Response> responseChannel,
        Logic.Field symbol
    )
    {
        Address = address;
        this.requestStream = requestStream;
        this.responseStream = responseStream;
        channel = Channel.CreateUnbounded<Request>();
        this.responseChannel = responseChannel;
        Symbol = symbol;
    }

    public async Task Process(CancellationToken ct)
    {
        await Task.WhenAll(ClientToServer(), ServerToClient());

        async Task ClientToServer()
        {
            await foreach (var request in requestStream.ReadAllAsync(ct))
            {
                await responseChannel.WriteAsync(request, ct);
            }
        }

        async Task ServerToClient()
        {
            await foreach (var request in channel.Reader.ReadAllAsync(ct))
            {
                await responseStream.WriteAsync(request, ct);
            }
        }
    }
}
