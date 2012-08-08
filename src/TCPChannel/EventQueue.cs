using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TCPChannel
{
    public class EventQueue<T>
    {
        #region fields
        private ThreadSafeQueue<T> queue;
        private Thread queueThread;
        private AutoResetEvent queueGate;
        private IEventQueueCallback callback;
        private bool processing;

        #endregion

        #region ctors
        public EventQueue(IEventQueueCallback eventcallback)
        {
            processing = true;
            callback = eventcallback;
            queue = new ThreadSafeQueue<T>();
            queueGate = new AutoResetEvent(false);
            queueThread = new Thread(new ThreadStart(ProcessQueueEvents));
            queueThread.Start();
        }
        #endregion

        #region public methods

        public void QueueEvent(T e)
        {
#if TCPCTRACE
            Logger.Trace("ProtocolEventQueue:QueueEvent()", "called");
#endif
            queue.Push(e);
            queueGate.Set();
#if TCPCTRACE
            Logger.Trace("ProtocolEventQueue:QueueEvent()", "finished");
#endif
        }

        public void StopProcessing()
        {
#if TCPCTRACE
            Logger.Trace("ProtocolEventQueue:StopProcessing()", "called");
#endif
            processing = false;
            queueGate.Set();

#if TCPCTRACE
            Logger.Trace("ProtocolEventQueue:StopProcessing()", "finished");
#endif
        }
        #endregion

        #region private methods

        private void ProcessQueueEvents()
        {
#if TCPCTRACE
            Logger.Trace("ProtocolEventQueue:ProcessQueueEvents()", "called");
#endif
            while (processing)
            {
                if (queue.Count == 0)
                {
                    queueGate.WaitOne();
#if TCPCTRACE
                    Logger.Trace("ProtocolEventQueue:ProcessQueueEvents()", "continueing");
#endif
                    continue;
                }
                else
                {
#if TCPCTRACE
                    Logger.Trace("ProtocolEventQueue:ProcessQueueEvents()", "processing event");
#endif
                    callback.HandleEvent(queue.Pop());

#if TCPCTRACE
                    Logger.Trace("ProtocolEventQueue:ProcessQueueEvents()", "done processing event");
#endif
                }
            }
#if TCPCTRACE
            Logger.Trace("ProtocolEventQueue:ProcessQueueEvents()", "finished");
#endif
        }

        public interface IEventQueueCallback
        {
            void HandleEvent(T obj);
        }

        class ThreadSafeQueue<QT>
        {
            private Queue<QT> queue;

            public ThreadSafeQueue()
            {
                queue = new Queue<QT>();
            }

            public QT Pop()
            {
#if TCPCTRACE
                Logger.Trace("ThreadSafeQueue:Pop()", "called");
#endif
                lock (queue)
                {
                    return queue.Dequeue();
                }
            }

            public void Push(QT toPush)
            {
#if TCPCTRACE
                Logger.Trace("ThreadSafeQueue:Push()", "called");
#endif
                lock (queue)
                {
                    queue.Enqueue(toPush);
                }
#if TCPCTRACE
                Logger.Trace("ThreadSafeQueue:Push()", "finished");
#endif
            }

            public int Count
            {
                get
                {
                    lock (queue)
                    {
                        return queue.Count;
                    }
                }
            }
        }

        #endregion
    }

}
