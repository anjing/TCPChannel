using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TCPChannel.Event
{

    public interface IDisconnected : IEvent
    {
    }

    public class Disconnected : BaseEvent, IDisconnected
    {
        public Disconnected() : base(-1, null) { }
    }
}
