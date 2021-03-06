﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Ocelot.Logging;
using Ocelot.Requester.QoS;

namespace Ocelot.Requester
{
    internal class HttpClientBuilder
    {
        private readonly Dictionary<int, Func<DelegatingHandler>> _handlers = new Dictionary<int, Func<DelegatingHandler>>();

        public HttpClientBuilder WithQoS(IQoSProvider qoSProvider, IOcelotLogger logger, HttpMessageHandler innerHandler)
        {
            _handlers.Add(5000, () => new PollyCircuitBreakingDelegatingHandler(qoSProvider, logger, innerHandler));
            return this;
        }

        internal HttpClient Build(HttpMessageHandler innerHandler)
        {
            return _handlers.Any() ? 
                new HttpClient(CreateHttpMessageHandler()) : 
                new HttpClient(innerHandler);
        }

        private HttpMessageHandler CreateHttpMessageHandler()
        {
            HttpMessageHandler httpMessageHandler = new HttpClientHandler();

            _handlers
                .OrderByDescending(handler => handler.Key)
                .Select(handler => handler.Value)
                .Reverse()
                .ToList()
                .ForEach(handler =>
            {
                var delegatingHandler = handler();
                delegatingHandler.InnerHandler = httpMessageHandler;
                httpMessageHandler = delegatingHandler;
            });

            return httpMessageHandler;
        }
    }
}
