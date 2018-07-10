﻿using Microsoft.Extensions.Logging;
using Microsoft.Owin;
using MySql.Data.MySqlClient;
using Owin;
using Steeltoe.CloudFoundry.Connector;
using Steeltoe.CloudFoundry.Connector.MySql;
using Steeltoe.CloudFoundry.Connector.Relational;
using Steeltoe.CloudFoundry.Connector.Relational.MySql;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Trace;
using Steeltoe.Management.EndpointOwin.CloudFoundry;
using Steeltoe.Management.EndpointOwin.Env;
using Steeltoe.Management.EndpointOwin.Health;
using Steeltoe.Management.EndpointOwin.HeapDump;
using Steeltoe.Management.EndpointOwin.Info;
using Steeltoe.Management.EndpointOwin.Loggers;
using Steeltoe.Management.EndpointOwin.Metrics;
using Steeltoe.Management.EndpointOwin.Refresh;
using Steeltoe.Management.EndpointOwin.ThreadDump;
using Steeltoe.Management.EndpointOwin.Trace;
using System.Collections.Generic;

[assembly: OwinStartup(typeof(CloudFoundryOwin.Startup))]

namespace CloudFoundryOwin
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // create a trace repository for use in both a request tracing middleware and the middleware for the /trace endpoint that returns those traces
            var traceRepository = new OwinTraceRepository(new TraceOptions(ApplicationConfig.Configuration), ((LoggerFactory)ApplicationConfig.LoggerFactory).CreateLogger<OwinTraceRepository>());

            app
                .UseRequestTracingMiddleware(traceRepository, ApplicationConfig.LoggerFactory)
                .UseCloudFoundrySecurityMiddleware(ApplicationConfig.Configuration, ApplicationConfig.LoggerFactory)
                .UseCloudFoundryEndpointMiddleware(ApplicationConfig.Configuration, ApplicationConfig.LoggerFactory)
                .UseEnvEndpointMiddleware(ApplicationConfig.Configuration, ApplicationConfig.LoggerFactory)
                .UseHealthEndpointMiddleware(new HealthOptions(ApplicationConfig.Configuration), new DefaultHealthAggregator(), GetHealthContributors(), ApplicationConfig.LoggerFactory)
                .UseHeapDumpEndpointMiddleware(ApplicationConfig.Configuration, ApplicationConfig.GetContentRoot(), ApplicationConfig.LoggerFactory)
                .UseInfoEndpointMiddleware(ApplicationConfig.Configuration, ApplicationConfig.LoggerFactory)
                .UseLoggersEndpointMiddleware(ApplicationConfig.Configuration, ApplicationConfig.LoggerProvider, ApplicationConfig.LoggerFactory)
                // .UseMappingEndpointMiddleware(ApplicationConfig.Configuration, ApplicationConfig.LoggerFactory); // not even started!
                .UseMetricsEndpointMiddleware(ApplicationConfig.Configuration, ApplicationConfig.LoggerFactory)
                .UseRefreshEndpointMiddleware(ApplicationConfig.Configuration, ApplicationConfig.LoggerFactory)
                .UseThreadDumpEndpointMiddleware(ApplicationConfig.Configuration, ApplicationConfig.LoggerFactory)
                .UseTraceEndpointMiddleware(ApplicationConfig.Configuration, traceRepository, ApplicationConfig.LoggerFactory);
        }

        private IEnumerable<IHealthContributor> GetHealthContributors()
        {
            var info = ApplicationConfig.Configuration.GetSingletonServiceInfo<MySqlServiceInfo>();
            var mySqlConfig = new MySqlProviderConnectorOptions(ApplicationConfig.Configuration);
            var factory = new MySqlProviderConnectorFactory(info, mySqlConfig, MySqlTypeLocator.MySqlConnection);

            var healthContributors = new List<IHealthContributor>
            {
                new DiskSpaceContributor(),
                new RelationalHealthContributor(new MySqlConnection(factory.CreateConnectionString()))
            };

            return healthContributors;
        }
    }
}