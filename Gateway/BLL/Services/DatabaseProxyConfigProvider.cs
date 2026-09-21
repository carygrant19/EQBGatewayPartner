using Gateway.BLL.Helper;
using Gateway.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.BLL.Services
{
    public class DatabaseProxyConfigProvider : IProxyConfigProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private CustomMemoryProxyConfig _config;
        private CancellationTokenSource _cts = new();

        public DatabaseProxyConfigProvider(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _config = LoadFromDb();
        }

        public IProxyConfig GetConfig() => _config;

        public void UpdateConfig()
        {
            var oldCts = _cts;
            _cts = new CancellationTokenSource();
            _config = LoadFromDb();
            oldCts.Cancel();
        }

        private CustomMemoryProxyConfig LoadFromDb()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EFDbContext>();

            // Kunin lamang ang Active endpoints na naka-set bilang PROXY (HINDI Auth o Mock)
            var activeEndpoints = dbContext.Set<Route>()
                .Include(e => e.TargetHosts)
                .Include(e => e.Transforms)
                .Where(e => e.IsActive && (e.IntegrationType == "PROXY" || string.IsNullOrEmpty(e.IntegrationType)))
                .AsNoTracking()
                .ToList()
                .DistinctBy(e => e.Id);

            var routes = new List<RouteConfig>();
            var clusters = new List<ClusterConfig>();

            foreach (var endpoint in activeEndpoints)
            {
                // Gagamitin ang endpoint.Code bilang RouteId para sa instant matching sa SecurityMiddleware
                var routeId = endpoint.Code;
                var clusterId = $"cluster_{endpoint.Id}";

                // 1. SETUP ROUTE TRANSFORMS
                var transforms = new List<IReadOnlyDictionary<string, string>>();

                // Downstream Path Rewriting
                if (!string.IsNullOrWhiteSpace(endpoint.DownstreamPathTemplate))
                {
                    transforms.Add(new Dictionary<string, string>
                    {
                        { "PathPattern", endpoint.DownstreamPathTemplate }
                    });
                }

                // Preserve Host Header
                if (endpoint.PreserveHostHeader)
                {
                    transforms.Add(new Dictionary<string, string>
                    {
                        { "RequestHeaderOriginalHost", "true" }
                    });
                }

                // Dynamic Header Modifications mula sa Map_Endpoint_Transform table
                if (endpoint.Transforms != null && endpoint.Transforms.Count > 0)
                {
                    foreach (var t in endpoint.Transforms)
                    {
                        if (t.TransformPhase.Equals("Request", StringComparison.OrdinalIgnoreCase))
                        {
                            if (t.Action.Equals("Add", StringComparison.OrdinalIgnoreCase) || t.Action.Equals("Append", StringComparison.OrdinalIgnoreCase))
                                transforms.Add(new Dictionary<string, string> { { "RequestHeader", t.HeaderName }, { "Set", t.HeaderValue ?? "" } });
                            else if (t.Action.Equals("Remove", StringComparison.OrdinalIgnoreCase))
                                transforms.Add(new Dictionary<string, string> { { "RequestHeaderRemove", t.HeaderName } });
                        }
                        else if (t.TransformPhase.Equals("Response", StringComparison.OrdinalIgnoreCase))
                        {
                            if (t.Action.Equals("Add", StringComparison.OrdinalIgnoreCase) || t.Action.Equals("Append", StringComparison.OrdinalIgnoreCase))
                                transforms.Add(new Dictionary<string, string> { { "ResponseHeader", t.HeaderName }, { "Set", t.HeaderValue ?? "" } });
                            else if (t.Action.Equals("Remove", StringComparison.OrdinalIgnoreCase))
                                transforms.Add(new Dictionary<string, string> { { "ResponseHeaderRemove", t.HeaderName } });
                        }
                    }
                }

                // 2. HTTP METHODS PARSING (Sumusuporta sa Comma o Pipe separators)
                string rawMethods = !string.IsNullOrWhiteSpace(endpoint.AllowedMethods)
                    ? endpoint.AllowedMethods
                    : endpoint.UpstreamHttpMethod;

                string[]? parsedMethods = string.IsNullOrWhiteSpace(rawMethods) || rawMethods.Trim() == "*"
                    ? null
                    : rawMethods.Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                var routeConfig = new RouteConfig
                {
                    RouteId = routeId,
                    ClusterId = clusterId,
                    Match = new RouteMatch
                    {
                        Path = endpoint.UpstreamPathTemplate,
                        Methods = parsedMethods
                    },
                    Order = endpoint.Priority,
                    Timeout = TimeSpan.FromSeconds(endpoint.TimeoutSeconds > 0 ? endpoint.TimeoutSeconds : 30),
                    Transforms = transforms
                };

                // 3. TARGET HOSTS DESTINATIONS
                var destinations = new Dictionary<string, DestinationConfig>();

                if (endpoint.TargetHosts != null)
                {
                    foreach (var host in endpoint.TargetHosts.Where(h => h.IsHealthy))
                    {
                        var destKey = $"dest_{host.Id}";
                        if (!destinations.ContainsKey(destKey))
                        {
                            destinations.Add(destKey, new DestinationConfig
                            {
                                Address = $"{endpoint.DownstreamScheme}://{host.Host}:{host.Port}"
                            });
                        }
                    }
                }

                // Idadagdag lang sa YARP kung may nakakabit na active target hosts
                if (destinations.Count > 0)
                {
                    routes.Add(routeConfig);
                    clusters.Add(new ClusterConfig
                    {
                        ClusterId = clusterId,
                        LoadBalancingPolicy = string.IsNullOrWhiteSpace(endpoint.LoadBalancingPolicy) ? "RoundRobin" : endpoint.LoadBalancingPolicy,
                        Destinations = destinations
                    });
                }
            }

            return new CustomMemoryProxyConfig(routes, clusters, _cts.Token);
        }

        private class CustomMemoryProxyConfig : IProxyConfig
        {
            public CustomMemoryProxyConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters, CancellationToken cancellationToken)
            {
                Routes = routes;
                Clusters = clusters;
                ChangeToken = new CancellationChangeToken(cancellationToken);
            }

            public IReadOnlyList<RouteConfig> Routes { get; }
            public IReadOnlyList<ClusterConfig> Clusters { get; }
            public IChangeToken ChangeToken { get; }
        }
    }
}