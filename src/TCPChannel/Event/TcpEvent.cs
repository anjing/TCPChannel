using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCPChannel.Protocol;

namespace TCPChannel.Event
{
    [Serializable]
    public class TcpEvent : IEvent
    {
        public delegate void TcpEventHandler(IProtocol protocol, IEvent e);

        private int id;
        protected byte[] data;

        public TcpEvent(int id, byte[] data)
        {
            this.id = id;
            this.data = data;
        }

        public int ID { get { return id; } }

        public byte[] Data { get { return data; } }
    }

    public enum EventId
    {
        Disconnect,
        SendMessage,
        Media,
        Content,
        Stack,
        Schedule
    }
}
