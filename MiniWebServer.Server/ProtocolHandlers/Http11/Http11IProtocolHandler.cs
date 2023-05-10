﻿using Microsoft.Extensions.Logging;
using MiniWebServer.Abstractions;
using MiniWebServer.Abstractions.Http;
using MiniWebServer.Abstractions.HttpParser.Http11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static MiniWebServer.Abstractions.ProtocolHandlerStates;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MiniWebServer.Server.ProtocolHandlers.Http11
{
    public class Http11IProtocolHandler : IProtocolHandler
    {
        private readonly ILogger logger;
        private readonly IHttp11Parser http11Parser;

        public Http11IProtocolHandler(ILogger? logger, IHttp11Parser? http11Parser)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.http11Parser = http11Parser ?? throw new ArgumentNullException(nameof(http11Parser));
        }

        public int ProtocolVersion => 101;

        public async Task<BuildRequestStates> ReadRequestAsync(Stream clientStream, IHttpRequestBuilder httpWebRequestBuilder, ProtocolHandlerData data)
        {
            BuildRequestStates state = BuildRequestStates.InProgressWithNoData;

            data.Data ??= new Http11ProtocolData(
                new StreamReader(clientStream),
                new StreamWriter(clientStream)
                );

            var d = (Http11ProtocolData)data.Data;

            if (d.CurrentReadingPart == Http11RequestMessageParts.Header) // Read request line and header
            {
                // here we use sync I/O, which is inefficient, CPU time-consuming, but it is easy to implement and easy to understand. 
                // We will upgrade to async I/O later
                if (d.Reader.Peek() != -1) 
                {
                    // todo: requestLine should be handled as a sequence of octets
                    string? requestLineText = await d.Reader.ReadLineAsync();

                    if (requestLineText != null)
                    {
                        logger.LogInformation("{requestLine}", requestLineText);

                        var requestLine = http11Parser.ParseRequestLine(requestLineText);

                        if (requestLine != null)
                        {
                            var method = GetHttpMethod(requestLine.Method);

                            // when implementing as a sequence of octets, if method length exceeds method buffer length, you should return 501 Not Implemented 
                            // if Url length exceeds Url buffer length, you should return 414 URI Too Long

                            d.HttpMethod = method;
                            httpWebRequestBuilder
                                .SetMethod(method)
                                .SetUrl(requestLine.Url);

                            string? headerLineText = d.Reader.ReadLine(); 
                            while (headerLineText != null && d.CurrentReadingPart == Http11RequestMessageParts.Header)
                            {
                                if (!string.IsNullOrEmpty(headerLineText))
                                {
                                    var headerLine = http11Parser.ParseHeaderLine(headerLineText);

                                    if (headerLine != null)
                                    {
                                        httpWebRequestBuilder.AddHeader(headerLine.Name, headerLine.Value);
                                    }
                                    else
                                    {
                                        logger.LogError("Invalid header line: {line}", headerLineText);
                                        state = BuildRequestStates.Failed;
                                    }

                                    headerLineText = d.Reader.ReadLine();
                                }
                                else
                                {
                                    // an empty line indicates the end of header
                                    d.CurrentReadingPart = Http11RequestMessageParts.Body;
                                }
                            }
                        }
                        else
                        {
                            logger.LogError("Invalid request line: {line}", requestLineText);
                            state = BuildRequestStates.Failed;
                        }
                    }
                    else
                    {
                        logger.LogError("Invalid request line (empty)");
                        state = BuildRequestStates.Failed;
                    }
                }
            }
            else if (d.CurrentReadingPart == Http11RequestMessageParts.Body)
            {
                // read body parts
                if (d.Reader.Peek() != -1)
                {
                    if (d.HttpMethod == HttpMethod.Get) // GET doesn't have body
                    {
                        logger.LogError("GET cannot have a body");
                        state = BuildRequestStates.Failed;
                    }
                }

                d.CurrentReadingPart = Http11RequestMessageParts.Done;
            }
            else
            {
                //SendResponseStatus(d.Writer, HttpStatusCode.OK, "OK");
                //d.Writer.WriteLine("Content-Type: text/html; charset=UTF-8");
                //d.Writer.WriteLine("Content-Length: 0");
                //d.Writer.WriteLine("Connection: close");
                //d.Writer.WriteLine();
                //d.Writer.WriteLine();
                //d.Writer.Flush();

                state = BuildRequestStates.Succeeded;
            }

            return state;
        }

        public async Task SendResponseAsync(Stream clientStream, HttpResponse response, ProtocolHandlerData protocolHandlerData)
        {
            if (protocolHandlerData.Data is not Http11ProtocolData d)
            {
                logger.LogError("Invalid ProtocolHandlerData");
                await SendResponseStatus(new StreamWriter(clientStream), HttpStatusCode.InternalServerError, "Internal Server Error");
            }
            else
            {
                await SendResponseStatus(d.Writer, response.StatusCode, response.ReasonPhrase);
                foreach (var header in response.Headers) { 
                    await d.Writer.WriteLineAsync($"{header.Key}: {string.Join(',', header.Value.Value)}");
                }

                await d.Writer.WriteLineAsync();
                await d.Writer.FlushAsync();
                await response.Content.WriteToAsync(clientStream);
            }
        }

        private static async Task SendResponseStatus(StreamWriter writer, HttpStatusCode statusCode, string errorMessage)
        {
            await writer.WriteLineAsync($"HTTP/1.1 {((int)statusCode)} {errorMessage}");
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
            else if (HttpMethod.Patch.Method == method)
                return HttpMethod.Patch;
            else if (HttpMethod.Post.Method == method)
                return HttpMethod.Post;
            else if (HttpMethod.Put.Method == method)
                return HttpMethod.Put;
            else if (HttpMethod.Trace.Method == method)
                return HttpMethod.Trace;

            throw new ArgumentException(null, nameof(method));
        }

        public void Reset(ProtocolHandlerData data)
        {
            if (data.Data != null)
            {
                var d = (Http11ProtocolData)data.Data;
                d.CurrentReadingPart = Http11RequestMessageParts.Header;
            }
        }
    }

    public class Http11ProtocolData
    {
        public Http11ProtocolData(StreamReader reader, StreamWriter writer)
        {
            Reader = reader ?? throw new ArgumentNullException(nameof(reader));
            Writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public StreamReader Reader { get; set; }
        public StreamWriter Writer { get; set; }
        public Http11RequestMessageParts CurrentReadingPart { get; set; } = Http11RequestMessageParts.Header;
        public HttpMethod HttpMethod { get; set; } = HttpMethod.Get;
        public bool KeepAlive { get; set; } = true; // Keep-Alive is true by default in HTTP 1.1
    }

    public enum Http11RequestMessageParts
    {
        Header,
        Body,
        Done
    }
}
