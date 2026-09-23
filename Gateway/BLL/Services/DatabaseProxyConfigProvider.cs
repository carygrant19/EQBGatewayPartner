using Gateway.BLL.Helper;
using Gateway.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Yarp.ReverseProxy.Configuration;

namespace Gateway.Proxy.Helper
{
    public class DatabaseProxyConfigProvider : IProxyConfigProvider
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private CustomMemoryConfig _config;

        public DatabaseProxyConfigProvider(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _config = LoadConfigFromDatabase();
        }

        public IProxyConfig GetConfig() => _config;

        public void UpdateConfig()
        {
            var oldConfig = _config;
            _config = LoadConfigFromDatabase();

            // IISIPAIN NI YARP NA MAY BAGO AT I-RE-BUILD ANG ROUTING ENGINE
            oldConfig.SignalChange();
        }

        private CustomMemoryConfig LoadConfigFromDatabase()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EFDbContext>();

            var routes = new List<RouteConfig>();
            var clusters = new List<ClusterConfig>();

            var dbRoutes = dbContext.Set<Gateway.Data.Models.Route>()
                .Include(r => r.TargetHosts)
                .Where(r => r.IsActive)
                .AsNoTracking()
                .ToList();

            foreach (var r in dbRoutes)
            {
                // Parse Allowed Methods (GET, POST, etc.)
                var rawMethods = !string.IsNullOrWhiteSpace(r.AllowedMethods) ? r.AllowedMethods : r.UpstreamHttpMethod;
                var methods = string.IsNullOrWhiteSpace(rawMethods)
                    ? null
                    : rawMethods.Split(new[] { ',', '|', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(m => m.Trim().ToUpper())
                                .ToList();

                // 1. YARP ROUTE MATCH CONFIG
                routes.Add(new RouteConfig
                {
                    RouteId = r.Code,
                    ClusterId = r.Code,
                    Match = new RouteMatch
                    {
                        Path = r.UpstreamPathTemplate,
                        Methods = methods
                    }
                });

                // 2. YARP CLUSTER CONFIG
                var destinations = new Dictionary<string, DestinationConfig>();
                if (r.TargetHosts != null && r.TargetHosts.Count > 0)
                {
                    int index = 1;
                    foreach (var host in r.TargetHosts)
                    {
                        // SANITIZE: Tanggalin ang wildcards sa downstream path address
                        string cleanDownstreamPath = (r.DownstreamPathTemplate ?? "")
                            .Replace("/{**catch-all}", "")
                            .Replace("{**catch-all}", "")
                            .Replace("/{**remainder}", "")
                            .Replace("{**remainder}", "");

                        string addressUrl = $"{r.DownstreamScheme}://{host.Host}";
                        if (host.Port > 0) addressUrl += $":{host.Port}";
                        addressUrl += cleanDownstreamPath;

                        destinations[$"destination_{index}"] = new DestinationConfig { Address = addressUrl };
                        index++;
                    }
                }

                clusters.Add(new ClusterConfig
                {
                    ClusterId = r.Code,
                    Destinations = destinations
                });
            }

            return new CustomMemoryConfig(routes, clusters);
        }
    }

    public class CustomMemoryConfig : IProxyConfig
    {
        private readonly CancellationTokenSource _cts = new();

        public CustomMemoryConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            Routes = routes;
            Clusters = clusters;
            ChangeToken = new CancellationChangeToken(_cts.Token);
        }

        public IReadOnlyList<RouteConfig> Routes { get; }
        public IReadOnlyList<ClusterConfig> Clusters { get; }
        public IChangeToken ChangeToken { get; }

        public void SignalChange()
        {
            _cts.Cancel();
        }
    }
}