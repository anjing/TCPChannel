using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCPChannel.Event;

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
            queue.QueueEvent(new UpdateMediaEvent(1));
        }

        public override byte[] GetHeader(Transport.ITransport transport)
        {
            throw new NotImplementedException();
        }

        public override int GetPayloadSize(byte[] rawHeader)
        {
            throw new NotImplementedException();
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
