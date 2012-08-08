using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TCPChannel.Event
{
    public class UpdateMediaEvent : BaseEvent
    {
        public UpdateMediaEvent(int id)
            : base(id)
        {
        }

    }
}
