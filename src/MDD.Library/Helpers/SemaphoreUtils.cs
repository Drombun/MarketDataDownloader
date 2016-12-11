using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MDD.Library.Helpers
{
    public static class SemaphoreUtils
    {
        public class SemaphoreReleaseDisposable : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public SemaphoreReleaseDisposable(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }

        public static async Task<SemaphoreReleaseDisposable> LockAsync(this SemaphoreSlim semaphore, CancellationToken cancellationToken = new CancellationToken())
        {
            await semaphore.WaitAsync(cancellationToken);
            return new SemaphoreReleaseDisposable(semaphore);
        }

        public static SemaphoreReleaseDisposable Lock(this SemaphoreSlim semaphore)
        {
            semaphore.Wait();
            return new SemaphoreReleaseDisposable(semaphore);
        }
    }
}
