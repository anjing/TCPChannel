using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCPChannel.Protocol;

namespace TCPChannel.Event
{
    public abstract class BaseEvent : IEvent
    {
        public delegate void TcpEventHandler(IProtocol protocol, IEvent e);

        private int id;

        public BaseEvent(int id)
        {
            this.id = id;
        }

        public int ID { get { return id; } }
    }
}
