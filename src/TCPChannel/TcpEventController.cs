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
        private Queue<byte[]> rawMessageQ;

        private IProtocol tcpProtocol;
        // threads
        private Thread dataListener;                            // processes raw data from the debug port - creates MDS debug protocol messages
        private Thread messageProcessor;                        // processes MDS debug protocol messages
        private EventWaitHandle connectGate;                    // gate used to synchronize the start of the debug session
        private EventWaitHandle listenGate;                     // gate used to guarantee listeners are running before sending messages
        private EventWaitHandle processGate;
        static private object traceLock = new object();

        public TcpEventController()
        {
            // initialize
            rawMessageQ = new Queue<byte[]>();
            connectGate = new AutoResetEvent(false);
            listenGate = new AutoResetEvent(false);
            processGate = new AutoResetEvent(false);
        }

        public State Start(int port)
        {
            Console.WriteLine("Start listen port:{0}", port);
            this.port = port;

            tcpProtocol = new TcpProcotol();
            tcpProtocol.RegisterHandler(this);
            try
            {
                if ( !isRunning )
                {
                    transport = CreateTransport();
                    if (transport == null)
                        return State.ERROR;
                    else
                        Console.WriteLine("Connected");                    
                    System.Console.Out.WriteLine("Starting listen threads...");
                    isRunning = true;
                    ThreadStart ts = new ThreadStart(ListenForData);
                    dataListener = new Thread(ts);
                    dataListener.Start();

                    // Start message processing thread
                    ts = new ThreadStart(ProcessMessageList);
                    messageProcessor = new Thread(ts);
                    messageProcessor.Start();


                    System.Console.Out.WriteLine("Starting data processors...");
                    try
                    {
                        System.Console.Out.WriteLine("Started listening for messages...");

                        listenGate.WaitOne();

                        /// Send event to start session
                        System.Console.Out.WriteLine("Starting debug session...");

                        if (tcpProtocol == null)
                        {
                            Disconnect();
                            System.Console.Out.WriteLine("Error sending handshake: no protocol");
                            return State.ERROR;
                        }

                        // Block waiting for session reset
                        System.Console.Out.WriteLine("Wait for handshake response...");
                        connectGate.WaitOne();
                        System.Console.Out.WriteLine("DONE!");
                    }
                    catch (ThreadInterruptedException tie)
                    {
                        Disconnect();
                        throw;
                    }

                }
            }
            catch(Exception e)
            {
                state = State.ERROR;
            }
            return State.SUCCESS;
        }

        #region Message Processor Thread
        /// <summary>
        /// This method is run in a thread to process all the messages that
        /// come in from the RE.
        /// </summary>
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
                // Open listen gate - listener's are up.
                listenGate.Set();

                // Loop checking for data to become available
                while (isRunning)
                {
                    // Check to see if the client connection has been lost
                    if (!transport.IsConnected())
                        break; // Update state, notify other threads - exit gracefully

                    // Retrieve header
                    if (tcpProtocol == null)
                        break;
                    byte[] headerRaw = tcpProtocol.GetHeader(transport);

                    if (headerRaw == null)
                        break;  // Connection to client has been lost - stop processing
                    // Retrieve body size, read in remainder of message  
                    int payloadSize = tcpProtocol.GetPayloadSize(headerRaw);
                    byte[] bodyRaw;
                    int bytesRead = transport.Read(payloadSize, out bodyRaw, Timeout.Infinite);
                    if (bytesRead == 0)
                        break; // Connection to client has been lost - stop processing
                    // Combine the header and body of the message
                    byte[] messageRaw = new byte[headerRaw.Length + bodyRaw.Length];
                    headerRaw.CopyTo(messageRaw, 0);
                    bodyRaw.CopyTo(messageRaw, headerRaw.Length);

                    // Process Message
                    if (tcpProtocol == null || bytesRead < payloadSize)
                        break; // Connection to client has been lost - stop processing

                    // Add new message to inbound message queue
                    lock (rawMessageQ)
                    {
                        rawMessageQ.Enqueue(messageRaw);
                    }
                    processGate.Set(); // wake up the process thread to process new messages
                }
            }
            catch (ThreadInterruptedException)
            {

            }
            catch (ThreadAbortException)
            {

            }
            catch (Exception e)
            {
                // Set ERROR state
                error = true;
            }
            finally
            {
                // Tell processor thread that no new data is being listened for
                Disconnect();
                if (error)
                    state = State.ERROR;
            }
        }

        public void Disconnect()
        {
            try
            {
                switch (state)
                {
                    case State.ERROR:
                        isRunning = false;
                        processGate.Set();  // make sure we aren't proccessing anything
                        if (transport != null && ((messageProcessor != null && !messageProcessor.Join(1500)) || (dataListener != null && !dataListener.Join(1500))))
                        {
                            transport.Close();
                            if (messageProcessor.ThreadState == ThreadState.Running && Thread.CurrentThread != messageProcessor)
                                messageProcessor.Abort();
                            if(dataListener.ThreadState == ThreadState.Running && Thread.CurrentThread != dataListener)
                                dataListener.Abort();
                        }

                        System.Console.Out.WriteLine("Killing protocol");

                        if (tcpProtocol != null)
                        {
                            tcpProtocol.Close();
                        }

                        state = State.CANCELED;
                        break;
                }

            }
            catch (Exception e)
            {
                isRunning = false;
                if (transport != null)
                {
                    transport.Close();
                }
                state = State.ERROR;
                if(tcpProtocol != null)
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
