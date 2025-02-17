﻿using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniWebServer.MiniApp
{
    public interface IRequestForm : IEnumerable<KeyValuePair<string, StringValues>>
    {
        public int Count { get; }
        ICollection<string> Keys { get; }
        StringValues this[string key] { get; }
    }
}
