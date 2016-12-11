#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MDD.Library.Helpers;
using MDD.Library.Logging;

#endregion

namespace MDD.Library.Abstraction.Saver
{
    public abstract class MarketDataSaverBase
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        protected MarketDataSaverBase(IPathHelper pathHelper, IMyLogger logger, IFileContentHelper fileContent)
        {
            if (pathHelper == null)
            {
                throw new ArgumentNullException("pathHelper");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (fileContent == null)
            {
                throw new ArgumentNullException("fileContent");
            }

            Logger = logger;
            PathHelper = pathHelper;
            FileContent = fileContent;
        }

        private IFileContentHelper FileContent { get; }

        private IPathHelper PathHelper { get; }

        protected IMyLogger Logger { get; }

        protected async Task<DateTime?> GetLastTimestampInFile(string storageFolder, string currentSymbol, char delimiter)
        {
            try
            {
                using (await _semaphore.LockAsync())
                {
                    using (var writer = PathHelper.GetWriterToFile(storageFolder, currentSymbol))
                    {
                        return FileContent.GetLastTimestampInFile(writer, delimiter);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("[GetLastTimestamp] {0}", ex.Message));
            }

            return null;
        }

        protected async Task WriteToFile(IList<string> data, string storageFolder, string currentSymbol)
        {
            try
            {
                using (await _semaphore.LockAsync())
                {
                    using (var writer = PathHelper.GetWriterToFile(storageFolder, currentSymbol))
                    {
                        foreach (var d in data)
                        {
                            await writer.WriteLineAsync(d);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("[WriteToFile] {0}", ex.Message));
            }
        }
    }
}
