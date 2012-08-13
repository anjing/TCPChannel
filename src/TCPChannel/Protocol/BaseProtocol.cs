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
        public event Event.TcpEvent.TcpEventHandler eventHandler;
        private bool closed = false;

        public virtual void Close()
        {
            if (!this.isClosed())
            {
                closed = true;
                OnEvent(new TcpEvent((int)EventId.Disconnect, null));
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
                TcpEvent.TcpEventHandler handler = this.eventHandler;
                if (handler != null)
                    handler(this, e);
            }
            catch
            {                
            }
        }

        public bool isClosed()
        {
            return closed;
        }

        public abstract void ProcessIncomingMessage(byte[] rawMessage);

        public void RegisterHandler(IEventHandler eh)
        {
            this.eventHandler +=new TcpEvent.TcpEventHandler(eh.HandleEvent);
        }

        public void UnregisterHandler(Event.IEventHandler eh)
        {
            this.eventHandler -= new TcpEvent.TcpEventHandler(eh.HandleEvent);
        }
    }
}
