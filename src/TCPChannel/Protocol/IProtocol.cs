using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCPChannel.Transport;
using TCPChannel.Event;

namespace TCPChannel.Protocol
{
    public interface IProtocol
    {
        /// <summary>
        /// Close/clean up the protocol
        /// </summary>
        void Close();

        /// <summary>
        /// Specifies if the protocol is closed for this session
        /// </summary>
        bool isClosed();

        /// <summary>
        /// Processes a raw message.
        /// </summary>
        /// <param name="rawMessage"></param>
        void ProcessIncomingMessage(byte[] rawMessage);

        /// <summary>
        /// Register a handler 
        /// </summary>
        void RegisterHandler(IEventHandler eh);

        /// <summary>
        /// Unregister a handler
        /// </summary>
        void UnregisterHandler(IEventHandler eh);
    }
}
