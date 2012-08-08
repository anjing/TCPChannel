using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TCPChannel.Event
{
    public interface ISendMessage : IEvent
    {
        byte[] GetBytesToSend();
    }

    public class SendMessage : BaseEvent, ISendMessage
    {
        private byte[] bytesToSend;

        public SendMessage(byte[] bytesToSend)
            : base(-1)
        {
            this.bytesToSend = bytesToSend;
        }

        public byte[] GetBytesToSend()
        {
            return this.bytesToSend;
        }
    }
}
