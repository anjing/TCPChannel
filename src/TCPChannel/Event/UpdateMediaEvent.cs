using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TCPChannel.Event
{
    public interface IUpdateMediaEvent : IEvent
    {
    }

    [Serializable]
    public class UpdateMediaEvent : BaseEvent, IUpdateMediaEvent
    {
        public UpdateMediaEvent(int id)
            : base(id, null)
        {
        }

    }
}
