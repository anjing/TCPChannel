using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCPChannel.Event;
using TCPChannel.Transport;
using System.Threading;
using TCPChannel.Protocol;
using System.Net.Sockets;

namespace TCPChannel
{
    public class TcpEventController : IEventHandler
    {
        private const int PORT = 45654;
        private bool isRunning = false;
        private State state = State.SUCCESS;

        private int port;
        private ITransport transport;
        private TcpTransportListener tcpListener;
        private Queue<byte[]> rawMessageQ;

        private IProtocol tcpProtocol;
        // threads
        private Thread dataListener;                            // processes raw data from the debug port - creates MDS debug protocol messages
        private Thread messageProcessor;                        // processes MDS debug protocol messages
        private EventWaitHandle processGate;
        static private object traceLock = new object();

        public TcpEventController()
        {
            // initialize
            rawMessageQ = new Queue<byte[]>();
            processGate = new AutoResetEvent(false);
        }

        /// <summary>
        /// Start listener port and process messages received
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public State Start(int port)
        {
            tcpProtocol = new TcpProcotol();
            tcpProtocol.RegisterHandler(this);

            Console.WriteLine("Start listen port:{0}", port);
            this.port = port;
            try
            {
                if (!isRunning)
                {
                    System.Console.Out.WriteLine("Starting listen threads...");
                    isRunning = true;
                    ThreadStart ts = new ThreadStart(ListenForData);
                    dataListener = new Thread(ts);
                    dataListener.Start();

                    // Start message processing thread
                    ts = new ThreadStart(ProcessMessageList);
                    messageProcessor = new Thread(ts);
                    messageProcessor.Start();

                }
                state = State.SUCCESS;
            }
            catch (Exception e)
            {
                state = State.ERROR;
            }
            return state;
        }

        #region Message Processor Thread
        private void ProcessMessageList()
        {
            bool error = false;
            byte[] rawMsg = null;
            try
            {
                while (isRunning)
                {
                    while (rawMessageQ.Count > 0)
                    {
                        // Get Message
                        lock (rawMessageQ)
                        {
                            rawMsg = rawMessageQ.Peek();
                        }

                        try
                        {
                            if (tcpProtocol != null)
                                tcpProtocol.ProcessIncomingMessage(rawMsg);
                        }
                        catch (Exception ex)
                        {
                        }

                        // Remove Message
                        lock (rawMessageQ)
                        {
                            rawMessageQ.Dequeue();
                        }
                    }
                    processGate.WaitOne();
                }
            }
            catch (ThreadAbortException)
            {

            }
            catch (ThreadInterruptedException)
            {

            }
            catch (Exception e)
            {
                // Set ERROR state
                error = true;
            }
            finally
            {
                Disconnect();
                if (error)
                    state = State.ERROR;
            }
        }
        #endregion

        #region Data Listener Thread
        /// <summary>
        /// Listen for incoming data on the socket - creates messages from incoming data and adds it to 
        /// processing list.
        /// </summary>
        private void ListenForData()
        {
            bool error = false;
            try
            {
                tcpListener = new TcpTransportListener(TcpTransport.LOCALHOST, port);
                tcpListener.Start();

                // Loop checking for data to become available
                while (isRunning)
                {
                    // wait for client to connect
                    TcpClient client = tcpListener.WaitForConnection();
                    transport = new TcpTransport(client);
                    if (!transport.IsConnected())
                        break;

                    if (tcpProtocol == null)
                        break;
                    byte[] messageRaw = transport.Read();
                    // Add new message to inbound message queue
                    lock (rawMessageQ)
                    {
                        rawMessageQ.Enqueue(messageRaw);
                    }
                    processGate.Set(); // wake up the process thread to process new messages
                }
            }
            //catch (ThreadInterruptedException)
            //{

            //}
            //catch (ThreadAbortException)
            //{

            //}
            catch (Exception e)
            {
                error = true;
            }
            finally
            {
                Disconnect();
                if (error)
                    state = State.ERROR;
            }
        }

        public void Disconnect()
        {
            try
            {
                isRunning = false;
                processGate.Set();  // make sure we aren't proccessing anything
                if (transport != null && ((messageProcessor != null && !messageProcessor.Join(1500)) || (dataListener != null && !dataListener.Join(1500))))
                {
                    transport.Close();
                    if (messageProcessor.ThreadState == ThreadState.Running && Thread.CurrentThread != messageProcessor)
                        messageProcessor.Abort();
                    if (dataListener.ThreadState == ThreadState.Running && Thread.CurrentThread != dataListener)
                        dataListener.Abort();
                }

                System.Console.Out.WriteLine("Killing protocol");

                if (tcpProtocol != null)
                {
                    tcpProtocol.Close();
                }

                state = State.CANCELED;

            }
            catch (Exception e)
            {
                isRunning = false;
                if (transport != null)
                {
                    transport.Close();
                }
                state = State.ERROR;
                if (tcpProtocol != null)
                    tcpProtocol.Close();
                throw new Exception("Disconnect exception", e);
            }
        }

        #endregion


        private ITransport CreateTransport()
        {
            TcpClient client = new TcpClient();
            client.Connect(TcpTransport.LOCALHOST, port);
            if (client.Connected)
            {
                return new TcpTransport(TcpTransport.LOCALHOST, port, 0);
            }
            else
            {
                System.Console.Out.WriteLine("Unable to connect to target port {0}", port);
                return null;
            }
        }

        public void Stop()
        {
            Disconnect();
        }

        #region IEventHandler
        public void HandleEvent(IProtocol protocol, IEvent e)
        {
            if (e is ISendMessage)
            {
                if (!protocol.isClosed())
                {
                    if (transport != null)
                    {
                        ISendMessage sm = e as ISendMessage;
                        transport.Send(sm.GetBytesToSend());
                    }
                }
                else if (e is IDisconnected)
                {
                    protocol.UnregisterHandler(this);
                    if (dataListener != null)
                        dataListener.Abort();
                    tcpProtocol = null;
                }
            }
        }
        #endregion
    }
}
