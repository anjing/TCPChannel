using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;

namespace TCPChannel.Transport
{
    /// <summary>
    /// TCP implementation of the transport interface.
    /// </summary>
    public class TcpTransport : ITransport
    {
        #region public static members
        public static readonly String LOCALHOST = "127.0.0.1";
        #endregion

        #region private fields
        private const int WRITE_TIMEOUT = 15000;
        private TcpClient client;
        private NetworkStream dataStream;
        #endregion

        #region ctors
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="retryCount">Number of times to try connecting to host:port - 0 for infinite</param>
        public TcpTransport(String host, int port, int retryCount)
        {
            // Open our connection to the specified endpoint and get data stream
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(host), port);
            client = new TcpClient();

            ///Try to make the connection several times before reporting
            ///an error
            for (int a = 0; a < retryCount || retryCount == 0; a++)
            {
                try
                {
                    client.Connect(endPoint);
                    break;
                }
                catch (SocketException se)
                {
                    /// If the exception is NOT a "Connection Refused" exception
                    /// OR we have passed the retry count - throw the SE back up.
                    if (se.SocketErrorCode != SocketError.ConnectionRefused
                        || (a < retryCount))
                    {
                        throw se;
                    }
                }
                Thread.Sleep(500);
            }
            // Get client stream
            dataStream = client.GetStream();

            // Set write timeout
            dataStream.WriteTimeout = WRITE_TIMEOUT;
        }

        public TcpTransport(TcpClient client)
        {
            this.client = client;
            this.dataStream = this.client.GetStream();

            // Set write timeout
            dataStream.WriteTimeout = WRITE_TIMEOUT;
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~TcpTransport()
        {
            Close();
        }
        #endregion

        #region public methods
        /// <summary>
        /// See ITransport.
        /// </summary>
        public void Send(byte[] data)
        {
            if (client != null)
            {
                dataStream.Write(data, 0, data.Length);
                dataStream.Flush();
            }
            else
            {
                throw new TransportException("Send failed: client not available");
            }
        }

        /// <summary>
        /// See ITransport.
        /// </summary>
        public int Read(int numBytesToRead, out byte[] data, int timeout)
        {
            // Block on Read() call for timeout specified, as long as the client is connected
            data = new byte[numBytesToRead];
            int offset = 0;
            // Set timeout
            dataStream.ReadTimeout = timeout;
            try
            {
                while (offset < numBytesToRead)
                {
                    int read = dataStream.Read(data, offset, numBytesToRead - offset);

                    if (read <= 0)
                    {
                        return 0;
                    }
                    offset += read;
                }
            }
            catch (IOException)
            {
                return 0;
            }
            catch (ThreadInterruptedException)
            {
                /// Thread interrupted - try to terminate gracefully
                return 0;
            }
            return offset;
        }


        public byte[] Read()
        {
            if (dataStream.CanRead)
            {
                MemoryStream mem = new MemoryStream();
                byte[] myReadBuffer = new byte[1024];
                int numberOfBytesRead = 0;
                do
                {
                    numberOfBytesRead = dataStream.Read(myReadBuffer, 0, myReadBuffer.Length);
                    mem.Write(myReadBuffer, 0, numberOfBytesRead);
                }
                while (dataStream.DataAvailable);
                return mem.GetBuffer();
            }
            else
            {
                Console.WriteLine("Sorry.  You cannot read from this NetworkStream.");
                return null;
            } 
        }


        /// <summary>
        /// See ITransport
        /// </summary>
        public bool IsDataAvailable()
        {
            return (dataStream != null && dataStream.DataAvailable);
        }

        /// <summary>
        /// See ITransport
        /// </summary>
        public bool IsConnected()
        {
            /// This method polls the socket and determines if there is data available,
            /// but the amount available is 0 - which indicates the connection has been closed
            /// on the other side. 
            /// For more information on this backward approach:
            /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpref/html/frlrfsystemnetsocketssocketclassconnectedtopic.asp
            if (client != null
                && client.Client.Poll(10, SelectMode.SelectRead)
                && client.Client.Available == 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Close()
        {
            if (dataStream != null)
            {
                dataStream.Close();
            }
            if (client != null)
            {
                client.Close();
            }
            dataStream = null;
            client = null;
        }
        #endregion
    }
}
