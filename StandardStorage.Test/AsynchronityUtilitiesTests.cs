using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace StandardStorage.Test
{
    [TestClass]
    public class AsynchronityUtilitiesTests
    {
        [TestMethod]
        public async Task SwitchOffMainThreadAsync_OnMainThread()
        {
            // Make this thread look like the main thread by
            // setting up a synchronization context.
            SynchronizationContext dispatcher = new SynchronizationContext();
            SynchronizationContext original = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(dispatcher);
            try
            {
                Thread originalThread = Thread.CurrentThread;

                await AsynchronityUtilities.SwitchOffMainThreadAsync(CancellationToken.None);

                Assert.AreNotSame(originalThread, Thread.CurrentThread);
                Assert.IsNull(SynchronizationContext.Current);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(original);
            }
        }

        [TestMethod]
        public void SwitchOffMainThreadAsync_OffMainThread()
        {
            AsynchronityUtilities.TaskSchedulerAwaiter awaitable = AsynchronityUtilities.SwitchOffMainThreadAsync(CancellationToken.None);
            AsynchronityUtilities.TaskSchedulerAwaiter awaiter = awaitable.GetAwaiter();
            Assert.IsTrue(awaiter.IsCompleted); // guarantees the caller wouldn't have switched threads.
        }

        [TestMethod]
        public void SwitchOffMainThreadAsync_CanceledBeforeSwitch()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                cts.Cancel();
                try
                {
                    AsynchronityUtilities.SwitchOffMainThreadAsync(cts.Token);
                    Assert.Fail("Expected OperationCanceledException not thrown.");
                }
                catch (OperationCanceledException ex)
                {
                    Assert.AreEqual(cts.Token, ex.CancellationToken);
                }
            }
        }

        [TestMethod]
        public void SwitchOffMainThreadAsync_CanceledMidSwitch()
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                AsynchronityUtilities.TaskSchedulerAwaiter awaitable = AsynchronityUtilities.SwitchOffMainThreadAsync(cts.Token);
                AsynchronityUtilities.TaskSchedulerAwaiter awaiter = awaitable.GetAwaiter();

                cts.Cancel();

                try
                {
                    awaiter.GetResult();
                    Assert.Fail("Expected OperationCanceledException not thrown.");
                }
                catch (OperationCanceledException ex)
                {
                    Assert.AreEqual(cts.Token, ex.CancellationToken);
                }
            }
        }
    }
}
