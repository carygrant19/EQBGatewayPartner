using Microsoft.Extensions.Primitives;
using System.Threading;

namespace Gateway.Proxy.Helper
{
    public static class GatewayCacheSignal
    {
        private static CancellationTokenSource _cts = new();

        public static IChangeToken GetToken() => new CancellationChangeToken(_cts.Token);

        public static void ResetAllCaches()
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }
}