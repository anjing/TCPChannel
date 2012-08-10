using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TCPChannel;
using System.Threading;

namespace TCPChannelTest
{
    [TestClass]
    public class EventLoopTest
    {
        [TestMethod]
        public void EventLoopTests()
        {
            EventLoop el = new EventLoop();
            el.RegisterAction(() => { System.Diagnostics.Debug.WriteLine("Hello World"); });
            el.RegisterAction(() => { System.Console.WriteLine("Hello World"); });

            Thread t = new Thread(new ThreadStart(el.Start));
            t.Start();
            el.Close();
        }
    }
}
