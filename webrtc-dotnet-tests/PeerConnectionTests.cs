using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WonderMediaProductions.WebRtc
{
    [TestClass]
    public class PeerConnectionTests
    {
        [TestMethod]
        public void SingleThreadedLifetime()
        {
            for (int iteration = 0; iteration < 2; ++iteration)
            {
                // Test if a single peer-connection creation/destruction auto-shutdowns the global factory
                using (new PeerConnection(new PeerConnectionOptions()))
                {
                    Assert.IsTrue(PeerConnection.HasFactory);
                }

                Assert.IsFalse(PeerConnection.HasFactory);

                // Test if we can still create a peer-connection after a previous shutdown
                using (new PeerConnection(new PeerConnectionOptions()))
                {
                    Assert.IsTrue(PeerConnection.HasFactory);
                }

                Assert.IsFalse(PeerConnection.HasFactory);

                // Test if we can create multiple peer connections
                using (new PeerConnection(new PeerConnectionOptions()))
                using (new PeerConnection(new PeerConnectionOptions()))
                {
                    Assert.IsTrue(PeerConnection.HasFactory);
                }

                Assert.IsFalse(PeerConnection.HasFactory);

                // Disable auto-shutdown
                PeerConnection.Configure(new GlobalOptions { AutoShutdown = false });

                // Test if a single peer-connection creation/destruction does not auto-shutdown the global factory anymore
                using (new PeerConnection(new PeerConnectionOptions()))
                {
                    Assert.IsTrue(PeerConnection.HasFactory);
                }

                Assert.IsTrue(PeerConnection.HasFactory);

                // Test if we can still create a peer-connection
                using (new PeerConnection(new PeerConnectionOptions()))
                {
                    Assert.IsTrue(PeerConnection.HasFactory);
                }

                Assert.IsTrue(PeerConnection.HasFactory);

                // Test if we can create multiple peer connections
                using (new PeerConnection(new PeerConnectionOptions()))
                using (new PeerConnection(new PeerConnectionOptions()))
                {
                    Assert.IsTrue(PeerConnection.HasFactory);
                }

                // Shutdown manually
                PeerConnection.Shutdown();

                Assert.IsFalse(PeerConnection.HasFactory);

                // Enable auto-shutdown
                PeerConnection.Configure(new GlobalOptions { AutoShutdown = true });
            }
        }

        [TestMethod]
        public void MultiThreadedLifetime()
        {
            for (int iteration = 0; iteration < 2; ++iteration)
            {
                // Now check if connections can be created/destroyed on multiple threads without crashing
                var threadIds = new HashSet<int>();

                Parallel.For(0, 16, i =>
                {
                    using (new PeerConnection(new PeerConnectionOptions()))
                    using (new PeerConnection(new PeerConnectionOptions()))
                    {
                        Assert.IsTrue(PeerConnection.HasFactory);
                    }

                    threadIds.Add(Thread.CurrentThread.ManagedThreadId);
                });

                Assert.IsTrue(threadIds.Count >= 2);
                Assert.IsFalse(PeerConnection.HasFactory);

                // Disable auto-shutdown
                PeerConnection.Configure(new GlobalOptions { AutoShutdown = false });

                Parallel.For(0, 16, i =>
                {
                    using (new PeerConnection(new PeerConnectionOptions()))
                    using (new PeerConnection(new PeerConnectionOptions()))
                    {
                        Assert.IsTrue(PeerConnection.HasFactory);
                    }

                    threadIds.Add(Thread.CurrentThread.ManagedThreadId);
                });

                // Shutdown manually
                PeerConnection.Shutdown();

                Assert.IsFalse(PeerConnection.HasFactory);

                // Enable auto-shutdown
                PeerConnection.Configure(new GlobalOptions { AutoShutdown = true });
            }
        }
    }
}
