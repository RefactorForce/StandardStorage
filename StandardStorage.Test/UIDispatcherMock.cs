using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StandardStorage.Test
{
    class UIDispatcherMock : SynchronizationContext
    {
        readonly object syncObject = new object();
        readonly Queue<KeyValuePair<SendOrPostCallback, object>> messageQueue = new Queue<KeyValuePair<SendOrPostCallback, object>>();
#if !NETFX_CORE
        readonly Thread mainThread;
#endif
        bool continueProcessingMessages;

        internal UIDispatcherMock()
        {
#if !NETFX_CORE
            mainThread = Thread.CurrentThread;
#endif
            continueProcessingMessages = true;
        }

        public bool Continue
        {
            get
            {
                return continueProcessingMessages;
            }

            set
            {
                if (continueProcessingMessages != value)
                {
                    continueProcessingMessages = value;
                    lock (syncObject)
                    {
                        Monitor.Pulse(syncObject);
                    }
                }
            }
        }

        public static void MainThreadEntrypoint(Action mainThreadEntrypoint)
        {
            MainThreadEntrypoint(delegate
            {
                mainThreadEntrypoint?.Invoke();
                return Task.FromResult(true);
            });
        }

        public static void MainThreadEntrypoint(Func<Task> mainThreadEntrypoint)
        {
            UIDispatcherMock syncContext = new UIDispatcherMock();
            SynchronizationContext oldSyncContext = Current;
            SetSynchronizationContext(syncContext);
            Exception unhandledException = null;
            syncContext.Post(
                async state =>
                {
                    try
                    {
                        await mainThreadEntrypoint?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        unhandledException = ex;
                    }
                    finally
                    {
                        syncContext.Continue = false;
                    }
                },
                null);
            syncContext.Loop();
            SetSynchronizationContext(oldSyncContext);
            if (unhandledException != null)
            {
                ExceptionDispatchInfo.Capture(unhandledException).Throw();
            }
        }

        public override void Send(SendOrPostCallback d, object state)
        {
#if NETFX_CORE
            d(state);
#else
            if (Thread.CurrentThread == mainThread)
            {
                d?.Invoke(state);
            }
            else
            {
                using (ManualResetEvent completed = new ManualResetEvent(false))
                {
                    Post(
_ =>
{
try
{
        d?.Invoke(state);
}
finally
{
completed.Set();
}
},
null);

                    completed.WaitOne();
                }
            }
#endif
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            lock (syncObject)
            {
                messageQueue.Enqueue(new KeyValuePair<SendOrPostCallback, object>(d, state));
                Monitor.Pulse(syncObject);
            }
        }

        public void Loop()
        {
#if !NETFX_CORE
            if (mainThread != Thread.CurrentThread)
            {
                throw new InvalidOperationException("Wrong thread!");
            }
#endif

            while (Continue)
            {
                KeyValuePair<SendOrPostCallback, object> message = DequeueMessage();
                message.Key?.Invoke(message.Value);
            }
        }

        KeyValuePair<SendOrPostCallback, object> DequeueMessage()
        {
            lock (syncObject)
            {
                while (messageQueue.Count == 0 && Continue)
                {
                    Monitor.Wait(syncObject);
                }

                return Continue ? messageQueue.Dequeue() : new KeyValuePair<SendOrPostCallback, object>();
            }
        }
    }
}
