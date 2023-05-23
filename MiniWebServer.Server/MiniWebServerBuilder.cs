﻿using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using MimeMapping;
using MiniWebServer.Abstractions;
using MiniWebServer.Configuration;
using MiniWebServer.MiniApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MiniWebServer.Server
{
    public class MiniWebServerBuilder : IServerBuilder
    {
        private IPAddress address = IPAddress.Loopback;
        private int httpPort = 80;
        private readonly ILogger<MiniWebServerBuilder> logger;
        private IDistributedCache cache;
        private string certificateFile = string.Empty;
        private string certificatePassword = string.Empty;
        private readonly Dictionary<string, HostConfiguration> hosts = new();
        private readonly List<IRoutingServiceFactory> routingServiceFactories = new();
        private readonly List<IMimeTypeMapping> mimeTypeMappings = new();
        private int threadPoolSize = MiniWebServerConfiguration.DefaultThreadPoolSize;
        private ServiceProvider? serviceProvider;

        public MiniWebServerBuilder(ILogger<MiniWebServerBuilder> logger, IDistributedCache? cache)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public IServerBuilder UseOptions(ServerOptions serverOptions)
        {
            if (serverOptions == null)
            {
                throw new ArgumentNullException(nameof(serverOptions));
            }

            if (serverOptions.BindingOptions != null)
            {
                if (serverOptions.BindingOptions.Port > 0)
                {
                    UseHttpPort(serverOptions.BindingOptions.Port);
                }
                if (!string.IsNullOrEmpty(serverOptions.BindingOptions.Address))
                {
                    BindToAddress(serverOptions.BindingOptions.Address);
                }
                if (serverOptions.BindingOptions.SSL && !string.IsNullOrEmpty(serverOptions.BindingOptions.Certificate))
                {
                    // note that a certificate might have empty password
                    AddCertificate(serverOptions.BindingOptions.Certificate, serverOptions.BindingOptions.CertificatePassword);
                }
            }

            return this;
        }

        private void AddCertificate(string certificateFile, string certificatePassword)
        {
            if (File.Exists(certificateFile))
            {
                this.certificateFile = certificateFile;
                this.certificatePassword = certificatePassword;
            }
            else
            {
                throw new FileNotFoundException(nameof(certificateFile));
            }
        }

        public IServerBuilder BindToAddress(string ipAddress)
        {
            if (!IPAddress.TryParse(ipAddress, out IPAddress? ip))
                throw new ArgumentException(null, nameof(ipAddress));

            address = ip;

            return this;
        }

        public IServerBuilder UseHttpPort(int httpPort)
        {
            if (httpPort < 0 || httpPort > 65535)
            {
                throw new ArgumentOutOfRangeException(nameof(httpPort));
            }

            this.httpPort = httpPort;

            return this;
        }

        public IServerBuilder AddHost(string hostName, IMiniApp app)
        {
            hosts[hostName] = new HostConfiguration(hostName, app);

            return this;
        }

        public IServerBuilder AddRoutingServiceFactory(IRoutingServiceFactory routingServiceFactory)
        {
            routingServiceFactories.Add(routingServiceFactory);

            return this;
        }

        public IServerBuilder AddMimeTypeMapping(IMimeTypeMapping mimeTypeMapping)
        {
            mimeTypeMappings.Add(mimeTypeMapping);

            return this;
        }

        public IServerBuilder UseDependencyInjectionService(ServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            return this;
        }

        public IServerBuilder UseCache(IDistributedCache cache)
        {
            this.cache = cache;

            return this;
        }

        public IServerBuilder UseThreadPoolSize(int size)
        {
            if (size <= 0)
            {
                throw new ArgumentException("ThreadPool size must be >= 0", nameof(size));
            }

            threadPoolSize = size;

            return this;
        }

        public IServer Build()
        {
            Validate();
            AddDefaultValuesIfRequired();

            Dictionary<string, Host.Host> hostContainers = new();
            foreach (var host in hosts.Values)
            {
                hostContainers.Add(host.HostName, new Host.Host(host.HostName, host.App));
            }

            var services = serviceProvider!; // it should be a Non-Null value

            var server = new MiniWebServer(new MiniWebServerConfiguration()
            {
                HttpEndPoint = new IPEndPoint(address, httpPort),
                Hosts = hosts.Values.ToList(),
                Certificate = string.IsNullOrEmpty(certificateFile) ? null : new X509Certificate2(certificateFile, certificatePassword),
                ThreadPoolSize = threadPoolSize
            },
                services.GetService<IProtocolHandlerFactory>(),
                hostContainers,
                services.GetService<ILogger<MiniWebServer>>() ?? new NullLogger<MiniWebServer>()
            );

            return server;
        }

        private void AddDefaultValuesIfRequired()
        {
            if (!hosts.Any())
            {
                hosts.Add(string.Empty, new HostConfiguration(string.Empty, new DefaultMiniApp()));
            }

            if (!mimeTypeMappings.Any()) // we always need a mime type mapping
            {
                mimeTypeMappings.Add(StaticMimeMapping.Instance);
            }
        }

        private void Validate() // move validating routines here so we can make Build function shorter 
        {
            if (serviceProvider == null)
            {
                throw new InvalidOperationException("ServiceProvider required");
            }

            if (cache == null)
            {
                // Opps, this will never happen
                throw new InvalidOperationException("Cache required");
            }
        }
    }
}
