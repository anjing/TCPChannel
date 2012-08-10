using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TCPChannel.Protocol
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
            queue.Push(e);
            queueGate.Set();
        }

        public void StopProcessing()
        {
            processing = false;
            queueGate.Set();
        }
        #endregion

        #region private methods

        private void ProcessQueueEvents()
        {
            while (processing)
            {
                if (queue.Count == 0)
                {
                    queueGate.WaitOne();
                    continue;
                }
                else
                {
                    callback.HandleEvent(queue.Pop());
                }
            }
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
                lock (queue)
                {
                    return queue.Dequeue();
                }
            }

            public void Push(QT toPush)
            {
                lock (queue)
                {
                    queue.Enqueue(toPush);
                }
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
