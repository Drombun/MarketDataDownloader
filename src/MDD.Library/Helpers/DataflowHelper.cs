#region Usings

using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using MDD.Library.Abstraction;
using MDD.Library.Configuration;

#endregion

namespace MDD.Library.Helpers
{
    public static class DataflowHelper
    {
        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1);

        static DataflowHelper()
        {
            MarketDataQueue = new BufferBlock<MarketDataBase>();
            ResponsesQueue = new BufferBlock<string>();
        }

        public static BufferBlock<MarketDataBase> MarketDataQueue { get; private set; }

        public static BufferBlock<string> ResponsesQueue { get; private set; }

        public static Task ResponsesQueueCompletion
        {
            get { return ResponsesQueue.Completion; }
        }

        public static Task MarketDataQueueCompletion
        {
            get { return MarketDataQueue.Completion; }
        }

        public static async Task<bool> ResponsesQueuePost(string str)
        {
            if (MarketDataQueue.Count > Cfg.StreamWriterSavingInterval())
            {
                await SemaphoreSlim.WaitAsync();
            }


            return ResponsesQueue.Post(str);
        }

        public static Task<MarketDataBase> MarketDataQueueReceiveAsync(CancellationToken token)
        {
            if (SemaphoreSlim.CurrentCount == 0 && MarketDataQueue.Count < Cfg.StreamWriterSavingInterval())
                SemaphoreSlim.Release();

            return MarketDataQueue.ReceiveAsync(token);
        }

        public static void RearmDataflowQueues()
        {
            MarketDataQueue = new BufferBlock<MarketDataBase>();
            ResponsesQueue = new BufferBlock<string>();

            SemaphoreSlim.Release();
        }
    }
}
