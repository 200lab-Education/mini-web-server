﻿using MiniWebServer.Abstractions.Http;
using MiniWebServer.Server.Abstractions.HttpParser.Http11;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using HttpMethod = MiniWebServer.Abstractions.Http.HttpMethod;

namespace MiniWebServer.HttpParser.Http11
{
    /// <summary>
    /// this class uses samples from: https://stackoverflow.com/questions/27457949/check-pattern-of-http-get-using-regexc
    /// we will need a ABNF-based parser 
    /// </summary>
    public partial class RegexHttp11Parsers : IHttpComponentParser
    {
        public virtual HttpRequestLine? ParseRequestLine(string text)
        {
            try
            {
                var httpRegex = HttpRequestLineRegex();
                var match = httpRegex.Match(text.Replace("{", "%7B").Replace("}", "%7D")); // these special characters make RegEx stops responding
                if (match.Success)
                {
                    var paramNameGroup = match.Groups["paramName"];
                    var paramValueGroup = match.Groups["paramValue"];

                    HttpParameters httpParameters = new();
                    for (int i = 0; i < paramNameGroup.Captures.Count; i++)
                    {
                        httpParameters.Add(new HttpParameter(paramNameGroup.Captures[i].Value, paramValueGroup.Captures[i].Value));
                    }

                    return new HttpRequestLine(
                        GetHttpMethod(match.Groups["method"].Value),
                        match.Groups["url"].Value,
                        match.Groups["hash"].Value,
                        match.Groups["queryString"].Value,
                        new HttpProtocolVersion(match.Groups["major"].Value, match.Groups["minor"].Value),
                        Array.Empty<string>(),
                        httpParameters
                        );
                }
            } catch { }

            return null;
        }

        public virtual HttpHeaderLine? ParseHeaderLine(string text)
        {
            var httpRegex = HttpHeaderLineRegex();
            var match = httpRegex.Match(text);
            if (match.Success)
            {
                return new HttpHeaderLine(
                    match.Groups["name"].Value,
                    match.Groups["value"].Value
                    );
            }

            return null;
        }

        public virtual IEnumerable<HttpCookie>? ParseCookieHeader(string text)
        {
            var httpRegex = HttpCookieValueRegex();
            var match = httpRegex.Match(text);
            if (match.Success)
            {
                var paramNameGroup = match.Groups["cookieName"];
                var paramValueGroup = match.Groups["cookieValue"];

                var cookies = new List<HttpCookie>();
                for (int i = 0; i < paramNameGroup.Captures.Count; i++)
                {
                    cookies.Add(new HttpCookie(paramNameGroup.Captures[i].Value, paramValueGroup.Captures[i].Value));
                }

                return cookies;
            }

            return null;
        }

        private static HttpMethod GetHttpMethod(string method)
        {
            if (HttpMethod.Connect.Method == method)
                return HttpMethod.Connect;
            else if (HttpMethod.Delete.Method == method)
                return HttpMethod.Delete;
            else if (HttpMethod.Get.Method == method)
                return HttpMethod.Get;
            else if (HttpMethod.Head.Method == method)
                return HttpMethod.Head;
            else if (HttpMethod.Options.Method == method)
                return HttpMethod.Options;
            else if (HttpMethod.Post.Method == method)
                return HttpMethod.Post;
            else if (HttpMethod.Put.Method == method)
                return HttpMethod.Put;
            else if (HttpMethod.Trace.Method == method)
                return HttpMethod.Trace;

            throw new ArgumentException("Unknown method", nameof(method));
        }


        // https://regex101.com/r/QLes5d/1
        [GeneratedRegex("^(?<method>[a-zA-Z]+)\\s(?<url>/[^\\r\\n\\?]*)(?<queryString>\\?((?<params>(?<paramName>\\w+)+=(?<paramValue>[\\w|%_-]*))&?)*)?(?<hash>#\\w*)?\\sHTTP/(?<major>\\d)\\.(?<minor>\\d+)$")]
        private static partial Regex HttpRequestLineRegex();

        [GeneratedRegex(@"(?<name>[\w-_]+): ?(?<value>[\w\-  _ :;.,\\/'?!(){}\[\]@<>=\-+\*#$&`|~^%""]+) ?")]
        private static partial Regex HttpHeaderLineRegex();

        [GeneratedRegex(@"^(((?<cookieName>[\w-_\.]+)=(?<cookieValue>[\w\-  _ :.,\\/'?!(){}\[\]@<>=\-+\*#$&`|~^%""]*))(; )?)*$")]
        private static partial Regex HttpCookieValueRegex();

        public virtual HttpRequestLine? ParseRequestLine(ReadOnlySequence<byte> lineBytes)
        {
            throw new NotImplementedException();
        }
    }
}
