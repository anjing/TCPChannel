using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TCPChannel.Event
{
    public interface ISendMessage : IEvent
    {
    }

    [Serializable]
    public class SendMessage : BaseEvent, ISendMessage
    {

        public SendMessage(byte[] bytesToSend)
            : base(-1, bytesToSend)
        {
        }

    }
}
