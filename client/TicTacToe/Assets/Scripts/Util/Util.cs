using System.Threading;

public static class Util
{
    public static string FormatAddress(string address)
    {
        return $"{address.Substring(0, 6)}...{address.Substring(address.Length - 4)}";
    }

    public static void CancelAndDispose(this CancellationTokenSource cts)
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}
