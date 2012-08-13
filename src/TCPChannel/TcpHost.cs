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
    public class TcpHost : IEventHandler
    {
        private bool isRunning = false;

        private int port;
        private ITransport transport;
        private TcpTransportListener tcpListener;
        private Queue<byte[]> rawMessageQ;

        private IProtocol tcpProtocol;

        // threads
        private Thread dataListener;
        private Thread messageProcessor; //processes raw data from the listen port  
        private EventWaitHandle processGate;

        static private object traceLock = new object();

        public TcpHost()
        {
            rawMessageQ = new Queue<byte[]>();
            processGate = new AutoResetEvent(false);
        }

        /// <summary>
        /// Start listener port and process messages received
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public bool Start(int port)
        {
            tcpProtocol = new TcpProcotol();
            tcpProtocol.RegisterHandler(this);
            this.port = port;
            try
            {
                if (!isRunning)
                {
                    Console.WriteLine("Start listen port:{0}", port);
                    isRunning = true;
                    ThreadStart ts = new ThreadStart(ListenForData);
                    dataListener = new Thread(ts);
                    dataListener.Start();

                    // Start message processing thread
                    ts = new ThreadStart(ProcessMessageList);
                    messageProcessor = new Thread(ts);
                    messageProcessor.Start();
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Can not start event controller, error:{0}", e.Message);
                return false;
            }
        }

        public void Stop()
        {
            try
            {
                isRunning = false;
                processGate.Set(); // make sure we aren't proccessing anything
                if (transport != null && ((messageProcessor != null && !messageProcessor.Join(1500)) ||
                    (dataListener != null && !dataListener.Join(1500))))
                {
                    transport.Close();
                    if (messageProcessor.ThreadState == ThreadState.Running && Thread.CurrentThread != messageProcessor)
                        messageProcessor.Abort();
                    if (dataListener.ThreadState == ThreadState.Running && Thread.CurrentThread != dataListener)
                        dataListener.Abort();
                }

                System.Console.WriteLine("Killing protocol");

                if (tcpProtocol != null)
                {
                    tcpProtocol.Close();
                }
            }
            catch (Exception e)
            {
                isRunning = false;
                if (transport != null)
                {
                    transport.Close();
                }
                if (tcpProtocol != null)
                    tcpProtocol.Close();
                throw new Exception("Disconnect exception", e);
            }
        }

        #region Message Processor Thread
        private void ProcessMessageList()
        {
            byte[] rawMsg = null;
            while (isRunning)
            {
                while (rawMessageQ.Count > 0)
                {
                    lock (rawMessageQ)
                    {
                        rawMsg = rawMessageQ.Dequeue();
                    }
                    try
                    {
                        if (tcpProtocol != null)
                            tcpProtocol.ProcessIncomingMessage(rawMsg);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Protocol process msg error:{0}", ex.Message);
                    }
                }
                processGate.WaitOne();
            }
        }
        #endregion

        #region Data Listener Thread
        /// <summary>
        /// Listen for incoming data on the port - creates messages from incoming data and adds it to processing list.
        /// </summary>
        private void ListenForData()
        {
            //start listening the port
            tcpListener = new TcpTransportListener(TcpTransport.LOCALHOST, port);
            tcpListener.Start();
            // Loop checking for data to become available
            while (isRunning)
            {
                try
                {
                    // wait for client to connect
                    TcpClient client = tcpListener.WaitForConnection();
                    Console.WriteLine("Client connected");
                    transport = new Transport.TcpTransport(client);
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
                }
                catch (Exception e)
                {
                    Console.WriteLine("Listen data error:{0}", e.Message);
                }
                processGate.Set(); // wake up the process thread to process new messages
            }
        }
        #endregion

        #region IEventHandler
        public void HandleEvent(IProtocol protocol, IEvent e)
        {
            EventId eid = (EventId)e.ID;
            switch (eid)
            {
                case EventId.Disconnect:
                    Stop();
                    break;
                case EventId.SendMessage:
                    if (!protocol.isClosed())
                    {
                        if (transport != null)
                        {
                            transport.Send(e.Data);
                        }
                    }
                    break;
                case EventId.Media:
                    Console.WriteLine("media event received, id={0}", eid);
                    break;
                case EventId.Content:
                    break;
                case EventId.Stack:
                    break;
                case EventId.Schedule:
                    break;
            }
        }
        #endregion
    }
}
