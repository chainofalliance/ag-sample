using AllianceGames.Sample.TicTacToe.Grpc;
using Grpc.Core;
using System.Threading.Channels;
using Serilog;
using System.Net.WebSockets;
using Chromia;

using Buffer = Chromia.Buffer;

internal class Player
{
    public readonly string Address;
    public Buffer PubKey => Buffer.From(Address);
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
        Log.Information($"Start {Address} Process task");
        await Task.WhenAll(ClientToServer(), ServerToClient());

        async Task ClientToServer()
        {
            try
            {
                await foreach (var request in requestStream.ReadAllAsync(ct))
                {
                    Log.Information($"Got client request from {Address}");
                    await responseChannel.WriteAsync(request, ct);
                }
            }
            catch (WebSocketException) { }
            catch (OperationCanceledException) { }
        }

        async Task ServerToClient()
        {
            try
            {
                await foreach (var request in channel.Reader.ReadAllAsync(ct))
                {
                    Log.Information($"Send server response to {Address}");
                    await responseStream.WriteAsync(request, ct);
                }
            }
            catch (WebSocketException) { }
            catch (OperationCanceledException) { }
        }
    }
}
