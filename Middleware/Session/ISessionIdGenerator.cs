﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniWebServer.Session
{
    public interface ISessionIdGenerator
    {
        string GenerateNewId();
    }
}
