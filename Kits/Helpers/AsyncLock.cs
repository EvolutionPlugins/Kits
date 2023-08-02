using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kits.Helpers;
internal class AsyncLock : IDisposable
{
    private readonly SemaphoreSlim m_SemaphoreSlim;
    private readonly ReleaserLock m_ReleaserLock;

    public AsyncLock()
    {
        m_SemaphoreSlim = new SemaphoreSlim(1);
        m_ReleaserLock = new ReleaserLock(this);
    }

    public async ValueTask<ReleaserLock> GetLockAsync()
    {
        await m_SemaphoreSlim.WaitAsync();
        return m_ReleaserLock;
    }

    public ReleaserLock GetLock()
    {
        m_SemaphoreSlim.Wait();
        return m_ReleaserLock;
    }

    internal void Release()
    {
        m_SemaphoreSlim.Release();
    }

    public void Dispose()
    {
        m_SemaphoreSlim.Dispose();
    }

    public readonly struct ReleaserLock : IDisposable
    {
        private readonly AsyncLock m_Lock;

        internal ReleaserLock(AsyncLock @lock)
        {
            m_Lock = @lock;
        }

        public readonly void Dispose()
        {
            m_Lock.Release();
        }
    }
}
