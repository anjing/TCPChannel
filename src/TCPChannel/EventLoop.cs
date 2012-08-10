using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TCPChannel
{
    public class EventLoop
    {
        private List<Action> _actions;
        private bool loop;

        public EventLoop()
        {
            _actions = new List<Action>();
            loop = true;
        }

        public void RegisterAction(Action action)
        {
            _actions.Add(action);
        }

        public void Start()
        {
            while (loop)
            {
                foreach (var myThing in _actions)
                {
                    myThing();
                }
                _actions.Clear();
            }
        }

        public void Close()
        {
            loop = false;
        }
    }
}
