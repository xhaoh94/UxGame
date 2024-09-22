﻿using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ux
{
    public class OneThreadSynchronizationContext : SynchronizationContext
    {
        public static OneThreadSynchronizationContext Instance { get; } = new OneThreadSynchronizationContext();        

        // 线程同步队列,发送接收socket回调都放到该队列,由poll线程统一执行
        private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

        private Action a;

        public OneThreadSynchronizationContext()
        {
            SynchronizationContext.SetSynchronizationContext(this);
            GameMethod.Update += _Update;
        }

        void _Update()
        {
            while (true)
            {
                if (!this.queue.TryDequeue(out a))
                {
                    return;
                }

                try
                {
                    a();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }
        public void Post(Action action)
        {
            this.queue.Enqueue(action);
        }
        public override void Post(SendOrPostCallback callback, object state)
        {
            this.Post(() => callback(state));
        }

        public void Clear()
        {
            while (this.queue.Count > 0)
            {
                this.queue.TryDequeue(out a);
            }
        }
    }
}