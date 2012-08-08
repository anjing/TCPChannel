using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TCPChannel.Transport
{
    /// <summary>
    /// Exception that is thrown when any of the transport classes fail
    /// </summary>
    public class TransportException : ApplicationException
    {
        public TransportException()
            : base()
        {
        }

        public TransportException(String msg)
            : base(msg)
        {
        }

        public TransportException(String msg, System.Exception innerException)
            : base(msg, innerException)
        {
        }
    }
}
