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
        /// Close/clean up the debug protocol
        /// </summary>
        void Close();

        /// <summary>
        /// Specifies if the debug protocol is closed for this session
        /// </summary>
        bool isClosed();

        /// <summary>
        /// Processes a raw message.
        /// </summary>
        /// <param name="rawMessage"></param>
        void ProcessIncomingMessage(byte[] rawMessage);

        /// <summary>
        /// Retrieves the header from the transport layer - required since
        /// the length of the header can vary from version to version.
        /// Especially an issue for dynamic length headers.
        /// </summary>
        /// <param name="transport">Transport layer to read header from</param>
        /// <returns>Byte array containing the header of the message</returns>
        byte[] GetHeader(ITransport transport);

        /// <summary>
        /// Retrieve the size of the payload, in bytes.
        /// </summary>
        /// <param name="rawHeader">Byte array of header data</param>
        int GetPayloadSize(byte[] rawHeader);

        /// <summary>
        /// Register a handler which is notified of Debug Events.
        /// Handlers required for the following:
        ///  - Sending messages (RuntimeController)
        ///  - Processing debug events (Visual Studio)
        ///  - Debug session events (RuntimeController)
        ///  - Application updates (Application Manager)
        /// </summary>
        /// <param name="deh">Handler to be added</param>
        void RegisterHandler(IEventHandler eh);

        /// <summary>
        /// Unregister a handler
        /// </summary>
        /// <param name="deh">Handler to be added</param>
        void UnregisterHandler(IEventHandler eh);
    }
}
