using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TCPChannel;

namespace TCPServer
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpHost host = new TcpHost();
            host.Start(45459);
        }
    }
}
