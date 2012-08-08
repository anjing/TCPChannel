using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCPChannel.Event;
using System.Threading;

namespace TCPChannel.Protocol
{
    public abstract class BaseProtocol : IProtocol
    {
        public event Event.BaseEvent.TcpEventHandler eventHandler;
        private bool closed = false;
        private int numHandlers = 0;

        public virtual void Close()
        {
            if (!this.isClosed())
            {
                closed = true;
                OnEvent(new Disconnected());
            }            
        }

        protected void OnEvent(IEvent e)
        {
            // Fork a thread to avoid event processing/message sending to deadlock
            Thread thread = new Thread(new ParameterizedThreadStart(OnEventThread));
            thread.Start(e);
        }

        protected void OnEventThread(object obj)
        {
            try
            {
                IEvent e = obj as IEvent;
                // avoid race condition by copying
                BaseEvent.TcpEventHandler handler = this.eventHandler;
                if (handler != null)
                    handler(this, e);
            }
            catch (Exception ex)
            {                
            }
        }

        public bool isClosed()
        {
            return closed;
        }

        public abstract void ProcessIncomingMessage(byte[] rawMessage);

        public abstract byte[] GetHeader(Transport.ITransport transport);

        public abstract int GetPayloadSize(byte[] rawHeader);

        public void RegisterHandler(IEventHandler eh)
        {
            this.eventHandler +=new BaseEvent.TcpEventHandler(eh.HandleEvent);
            numHandlers++;
        }

        public void UnregisterHandler(Event.IEventHandler eh)
        {
            this.eventHandler -= new BaseEvent.TcpEventHandler(eh.HandleEvent);
            numHandlers--;
        }
    }
}
