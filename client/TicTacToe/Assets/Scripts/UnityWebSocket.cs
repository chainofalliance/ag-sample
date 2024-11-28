
using AllianceGamesSdk.Common.Transport;
using Cysharp.Threading.Tasks;
using NativeWebSocket;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Channel = System.Threading.Channels.Channel;

public class UnityWebSocket : ITransport
{
    public async Task<ConnectionBase> Connect(Uri uri, CancellationToken ct)
    {
        return await UnityWebSocketConnection.Create(uri);
    }

    public IAsyncEnumerable<ConnectionBase> Listen(int port, CancellationToken ct)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {

    }
}

public class UnityWebSocketConnection : ConnectionBase
{
    private readonly WebSocket webSocket;
    private readonly System.Threading.Channels.Channel<byte[]> messages;

    public static async Task<UnityWebSocketConnection> Create(Uri uri)
    {
        var connection = new UnityWebSocketConnection(uri);
        var cs = new UniTaskCompletionSource();
        connection.webSocket.OnOpen += () =>
        {
            cs.TrySetResult();
        };
        await connection.webSocket.Connect();
        await cs.Task;
        return connection;
    }

    public UnityWebSocketConnection(Uri uri)
    {
        webSocket = new WebSocket(uri.ToString());

        messages = Channel.CreateUnbounded<byte[]>();
        webSocket.OnMessage += async (bytes) =>
        {
            UnityEngine.Debug.Log($"UnityWebSocketConnection: Received {bytes.Length} bytes");
            await messages.Writer.WriteAsync(bytes);
        };
    }

    protected override async Task SendInternal(byte[] message, CancellationToken ct)
    {
        await webSocket.Send(message);
    }

    protected override async IAsyncEnumerable<byte[]> ReceiveInternal(
        [EnumeratorCancellation] CancellationToken ct
    )
    {
        while (!ct.IsCancellationRequested)
        {
            yield return await messages.Reader.ReadAsync(ct).AsUniTask();
        }
    }

    protected override async Task DisconnectInternal(CancellationToken ct)
    {
        await webSocket.Close();
    }

    public override bool Equals(object obj)
    {
        return obj is UnityWebSocketConnection connection &&
               EqualityComparer<WebSocket>.Default.Equals(webSocket, connection.webSocket);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(webSocket);
    }
}