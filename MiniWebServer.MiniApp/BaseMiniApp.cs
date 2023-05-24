﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniWebServer.MiniApp
{
    public class BaseMiniApp : IMiniApp
    {
        internal readonly IDictionary<string, ActionDelegate> routes = new Dictionary<string, ActionDelegate>();

        public virtual Task Get(IMiniAppRequest request, IMiniAppResponse response, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual Task Post(IMiniAppRequest request, IMiniAppResponse response, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        public void MapGet(string route, RequestDelegate action)
        {
            var r = new ActionDelegate(route, action, Abstractions.Http.HttpMethod.Get);

            if (routes.ContainsKey(route)) 
                routes[route] = r;
            else 
                routes.Add(route, r);
        }

        public virtual ICallable? Find(IMiniAppRequest request)
        {
            foreach (var action in routes.Values)
            {
                if (action.IsMatched(request.Url, request.Method))
                {
                    return new ActionDelegateCallable(action);
                }
            }

            return null;
        }
    }
}
