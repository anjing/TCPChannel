using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TCPChannel.Transport
{
    /// <summary>
    /// Interface to connect, send and read data from an endpoint.
    /// </summary>
    public interface ITransport
    {
        /// <summary>
        /// Sends the contents of the byte array over the network stream.
        /// </summary>
        /// <param name="data">Data to send.</param>
        void Send(byte[] data);

        /// <summary>
        /// Blocks until the given number of bytes is read, or the timeout is reached.
        /// </summary>
        /// <param name="numBytesToRead">Number of bytes to read.</param>
        /// <param name="data">The uninitialized byte array where data will be read into.</param>
        /// <param name="timeout">The max timeout to wait (in milliseconds) for data to be received before timing out.</param>
        /// <returns>A byte array of the data that was read.</returns>
        int Read(int numBytesToRead, out byte[] data, int timeout);
        //read all data 
        byte[] Read();

        /// <summary>
        /// Closes the connection.
        /// </summary>
        void Close();

        /// <summary>
        /// Determines if the socket connection is still alive.
        /// </summary>
        /// <returns>True if the connection is available; false otherwise.</returns>
        bool IsConnected();

        /// <summary>
        /// Checks if there is any data to be read.  Indicates that if Read() is called,
        /// data will be immediately returned.
        /// </summary>
        /// <returns>True if data is available; false otherwise.</returns>
        bool IsDataAvailable();
    }
}
