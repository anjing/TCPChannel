using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCPChannel;
using TCPChannel.Transport;
using TCPChannel.Event;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace TCPClient
{
    class Program
    {
        static void Main(string[] args)
        {
            ITransport transport = TcpTransport.CreateTransport(45459);
            if (transport != null)
            {
                UpdateMediaEvent e = new UpdateMediaEvent(100);
                BinaryFormatter bf = new BinaryFormatter();
                MemoryStream mem = new MemoryStream();
                bf.Serialize(mem, e);
                transport.Send(mem.GetBuffer());
            }
        }
    }
}
