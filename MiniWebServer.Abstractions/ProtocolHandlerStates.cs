﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniWebServer.Abstractions
{
    public class ProtocolHandlerStates
    {
        public enum BuildRequestStates
        {
            InProgress,
            Succeeded,
            Failed
        }

        public enum WriteResponseStates
        {
            InProgress,
            Succeeded,
            Failed
        }
    }
}
