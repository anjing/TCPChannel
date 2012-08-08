using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCPChannel.Protocol;

namespace TCPChannel.Event
{
    /// <summary>
    /// Interface to be implemented if the object wants to receive events from the 
    /// Protocol
    /// </summary>
    public interface IEventHandler
    {
        void HandleEvent(IProtocol protocol, IEvent e);
    }
}
