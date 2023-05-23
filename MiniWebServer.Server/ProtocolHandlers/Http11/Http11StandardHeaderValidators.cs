﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniWebServer.Server.ProtocolHandlers.Http11
{
    public class Http11StandardHeaderValidators
    {
        public class ContentLengthHeaderValidator : IHeaderValidator
        {
            private readonly long maxLength;

            public ContentLengthHeaderValidator(long maxLength)
            {
                this.maxLength = maxLength;
            }

            public bool Validate(string name, string value)
            {
                if ("Content-Length".Equals(name))
                {
                    if (long.TryParse(value, out long length))
                    {
                        if (length > maxLength)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false; // we only return false when it is an error, otherwise we return true to continue processing flow
                    }
                }

                return true;
            }
        }

        public class TransferEncodingHeaderValidator : IHeaderValidator
        {
            
            protected virtual bool IsSupportedEncoding(string encoding)
            {
                return true; // we virtually support everything for now :|
            }
            public bool Validate(string name, string value)
            {
                if (!string.IsNullOrEmpty(value) && "Transfer-Encoding".Equals(name))
                {
                    var encodings = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    foreach (var encoding in encodings)
                    {
                        if (!IsSupportedEncoding(encoding))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
        }

        public class FallbackHeadervalidator : IHeaderValidator
        {
            public bool Validate(string name, string value)
            {
                return true;
            }
        }
    }
}
