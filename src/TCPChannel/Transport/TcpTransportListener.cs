using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Diagnostics;

namespace TCPChannel.Transport
{
    /// <summary>
    /// Wraper class that handles all connection and data fetching for a "server" connection
    /// </summary>
    public class TcpTransportListener 
    {
        /// <summary>
        /// state of the transort/socket
        /// </summary>
        public enum LISTENERSTATE
        {
            STOPPED,
            STOPPING,
            LISTENING,
            WAITING,
            CONNECTED
        }

        #region fields
        private String host;
        private int port;
        private TcpListener listener = null;
        private LISTENERSTATE state = LISTENERSTATE.STOPPED;

        #endregion

        #region ctors
        public TcpTransportListener(string host, int port)
        {
            this.host = host;
            this.port = port;
        }
        #endregion

        #region public methods

        public LISTENERSTATE State { get { return state; } }
        /// <summary>
        /// start the socket and begin listening
        /// </summary>
        public void Start()
        {
            if (state == LISTENERSTATE.STOPPED)
            {
                /// Create our listening socket to wait for connection/ping
                if (listener == null)
                {
                    try
                    {
                        IPAddress localAddr = IPAddress.Parse(host);
                        listener = new TcpListener(localAddr, port);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("can not start listener, error{0}", e.Message);
                        state = LISTENERSTATE.STOPPED;
                        return;
                    }
                }
                listener.Start();

                state = LISTENERSTATE.LISTENING;
            }
        }

        /// <summary>
        /// Stop the transport/socket
        /// </summary>
        public void Stop()
        {
            switch (state)
            {
                case LISTENERSTATE.STOPPED:
                case LISTENERSTATE.STOPPING:
                /// listener is being stopped or is stopped, no action
                case LISTENERSTATE.WAITING:
                    state = LISTENERSTATE.STOPPING;
                    /// The listener is in a safe state to stop
                    if (listener != null)
                    {
                        lock (listener)
                        {
                            listener.Stop();
                        }
                    }
                    state = LISTENERSTATE.STOPPED;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Wait for a client to connect to the socket
        /// </summary>
        /// <returns>TcpClient structure for the newly connected client</returns>
        public TcpClient WaitForConnection()
        {
            TcpClient client = null;
            if (state == LISTENERSTATE.LISTENING)
            {
                state = LISTENERSTATE.WAITING;
                while (state == LISTENERSTATE.WAITING)
                {
                    lock (listener)
                    {
                        if (listener.Pending())
                        {
                            client = listener.AcceptTcpClient();
                            state = LISTENERSTATE.CONNECTED;
                            return client;
                        }
                    }
                    Thread.Sleep(100);  //TODO: Add timout
                }
            }
            return null;
            ///TODO: Throw transport exception if not in listening state and attempting
            ///to wait for connection.                
        }
        #endregion
    }
}
