using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCPChannel.Event;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace TCPChannel.Protocol
{
    public class TcpProcotol : BaseProtocol, EventQueue<IEvent>.IEventQueueCallback
    {
        private EventQueue<IEvent> queue;

        public TcpProcotol()
        {
            queue = new EventQueue<IEvent>(this);
        }

        #region implemenation of BaseProtocol
        public override void ProcessIncomingMessage(byte[] rawMessage)
        {
            queue.QueueEvent(GetEventFromMsg(rawMessage));
        }

        private IEvent GetEventFromMsg(byte[] rawMessage)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream mem = new MemoryStream(rawMessage);
            IEvent e = formatter.Deserialize(mem) as IEvent;
            return e;
        }

        #endregion

        #region IEventQueueCallback
        public void HandleEvent(IEvent obj)
        {
            OnEventThread(obj);
        }
        #endregion

        #region overrides
        public override void Close()
        {
            base.Close();
            queue.StopProcessing();
        }
        #endregion
    }
}
