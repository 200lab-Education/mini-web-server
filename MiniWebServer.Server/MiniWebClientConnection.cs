﻿using MiniWebServer.Abstractions;
using MiniWebServer.Abstractions.Http;
using MiniWebServer.Server.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MiniWebServer.Server
{
    public class MiniWebClientConnection
    {
        public enum States
        {
            Pending,
            BuildingRequestObject,
            RequestObjectReady,
            CallingResource,
            CallingResourceReady,
            ResponseObjectReady,
            ReadyToClose
        }

        public MiniWebClientConnection(int id, TcpClient tcpClient, Stream clientStream, IProtocolHandler connectionHandler, States initState, DateTime timeout, bool isKeepAlive)
        {
            Id = id;
            TcpClient = tcpClient;
            ClientStream = clientStream;
            State = initState;
            ProtocolHandler = connectionHandler;
            ConnectionTimeoutTime = timeout;
            ProtocolHandlerData = new ProtocolHandlerData();
            RequestObjectBuilder = new HttpWebRequestBuilder();
            ResponseObjectBuilder = new HttpWebResponseBuilder();
            KeepAlive = isKeepAlive;
        }

        public int Id { get; }
        public TcpClient TcpClient { get; }
        public Stream ClientStream { get; }
        public IProtocolHandler ProtocolHandler { get; }
        public ProtocolHandlerData ProtocolHandlerData { get; }
        public States State { get; set; }
        public DateTime ConnectionTimeoutTime { get; internal set; }
        public bool KeepAlive { get; set; }
        public IHttpRequestBuilder RequestObjectBuilder { get; }
        public IHttpResponseBuilder ResponseObjectBuilder { get; }
    }
}
