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

            var activeEndpoints = dbContext.Set<ApiEndpoint>()
                .Include(e => e.TargetHosts)
                .Where(e => e.IsActive)
                .AsNoTracking()
                .ToList()
                .DistinctBy(e => e.Id);

            var routes = new List<RouteConfig>();
            var clusters = new List<ClusterConfig>();

            foreach (var endpoint in activeEndpoints)
            {
                var routeId = $"route_{endpoint.Id}";
                var clusterId = $"cluster_{endpoint.Id}";

                var routeConfig = new RouteConfig
                {
                    RouteId = routeId,
                    ClusterId = clusterId,
                    Match = new RouteMatch
                    {
                        Path = endpoint.UpstreamPathTemplate,
                        Methods = string.IsNullOrWhiteSpace(endpoint.UpstreamHttpMethod)
                            ? null
                            : endpoint.UpstreamHttpMethod.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    },
                    Order = endpoint.Priority,
                    Timeout = TimeSpan.FromSeconds(endpoint.TimeoutSeconds > 0 ? endpoint.TimeoutSeconds : 30),
                    Transforms = new List<IReadOnlyDictionary<string, string>>
                    {
                        new Dictionary<string, string> { { "PathPattern", endpoint.DownstreamPathTemplate } }
                    }
                };

                var destinations = new Dictionary<string, DestinationConfig>();

                foreach (var host in endpoint.TargetHosts)
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