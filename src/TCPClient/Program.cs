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
            string quit = "n";
            ITransport transport = TcpTransport.CreateTransport(45459);
            if (transport != null)
            {
                while (quit == "n")
                {

                    IEvent e = new TcpEvent((int)EventId.Media, null);
                    BinaryFormatter bf = new BinaryFormatter();
                    MemoryStream mem = new MemoryStream();
                    bf.Serialize(mem, e);
                    transport.Send(mem.GetBuffer());
                    Console.WriteLine("Quit?(y/n)");
                    quit = Console.ReadLine();
                }

                IEvent d = new TcpEvent((int)EventId.Disconnect, null);
                BinaryFormatter bfd = new BinaryFormatter();
                MemoryStream memd = new MemoryStream();
                bfd.Serialize(memd, d);
                transport.Send(memd.GetBuffer());
            }
        }
    }
}
