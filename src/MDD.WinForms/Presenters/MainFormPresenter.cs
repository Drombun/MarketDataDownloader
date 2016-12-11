#region Usings

using System;
using System.Threading;
using System.Threading.Tasks;
using MDD.Library.Abstraction;
using MDD.Library.Abstraction.Client;
using MDD.Library.Abstraction.Manager;
using MDD.Library.Abstraction.Parser;
using MDD.Library.Abstraction.Saver;
using MDD.Library.Configuration;
using MDD.Library.Helpers;
using MDD.Library.Models;
using MDD.WinForms.Abstraction;
using MDD.WinForms.Resolver;

using Microsoft.Practices.Unity;
using Timer = System.Timers.Timer;

#endregion

namespace MDD.WinForms.Presenters
{
	public sealed class MainFormPresenter
	{
		private readonly IPathHelper _myPathHelper;
		private readonly IMainFormView _view;
		private IDataFeedManager _dataFeedManager;
		private Timer _updateTimer;

	    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

		public MainFormPresenter(IMainFormView view)
		{
			if (view == null)
			{
				throw new ArgumentNullException("view");
			}

			_view = view;
			_myPathHelper = DependencyFactory.Container.Resolve<IPathHelper>();
		}

		public bool IsConnected { get; private set; }

		public void Start()
		{
			_updateTimer = new Timer {AutoReset = true};
			_dataFeedManager.StartService();
		}

		public void Stop()
		{
			_updateTimer.Stop();
			_dataFeedManager.StopService();
		}

		public void InitDataFeedManager(string datafeed)
		{
			switch (datafeed)
			{
				case "Fidelity":
					_dataFeedManager = DependencyFactory.Container.Resolve<IDataFeedManager>("Fidelity",
																							 new ParameterOverrides
																							 {
																								 {"client", DependencyFactory.Container.Resolve<IDataFeedClient>("Fidelity")},
																								 {"saver", DependencyFactory.Container.Resolve<IMarketDataSaver>("Fidelity")},
																								 {"parser", DependencyFactory.Container.Resolve<IMarketDataParser>("Fidelity")},
																								 {"requestBuilder", DependencyFactory.Container.Resolve<IRequestBuilder>("Fidelity")}
																							 });
					break;

				case "IQFeed":
					_dataFeedManager = DependencyFactory.Container.Resolve<IDataFeedManager>("IQFeed",
																							 new ParameterOverrides
																							 {
																								 {"client", DependencyFactory.Container.Resolve<IDataFeedClient>("IQFeed")},
																								 {"saver", DependencyFactory.Container.Resolve<IMarketDataSaver>("IQFeed")},
																								 {"parser", DependencyFactory.Container.Resolve<IMarketDataParser>("IQFeed")},
																								 {"requestBuilder", DependencyFactory.Container.Resolve<IRequestBuilder>("IQFeed")}
																							 });
					break;
			}

			_dataFeedManager.ConnectionEstablished = UpdateWithConnection;
		}

		private void UpdateWithConnection(bool isConnected, string connectionMessage)
		{
			IsConnected = isConnected;
			_view.UpdateWithConnection(IsConnected, connectionMessage);
		}

		public bool CheckSavePath(string tbFolderText)
		{
			if (!_myPathHelper.CreateDirectory(tbFolderText))
			{
				_view.Logger.Error("[Wrong directory name]");
				return false;
			}

			return true;
		}

		public async Task DownloadMarketData(RequestBase request, Parameters programParameters, bool isRealTime)
		{
			await _dataFeedManager.DownloadMarketData(request, programParameters);

			if (isRealTime)
			{
				_view.Logger.Info("Realtime updating...");
				_updateTimer.Interval = Cfg.UpdateInterval();

				switch (programParameters.DataFeedName)
				{
					case Cfg.Fidelity:
						_updateTimer.Elapsed += (obj, eventArgs) => DownloadFidelityRealTime(request, programParameters);
						break;

					case Cfg.IQFeed:
						if (request.TimeFrameName == "Tick")
						{
							_updateTimer.Interval = Cfg.UpdateIntervalTick();
						}
				        _updateTimer.Elapsed += (obj, eventArgs) => DownloadIqFeedRealTime(request, programParameters);
						break;
				}

				_updateTimer.Start();
			}
			else
			{
				_view.Logger.Warn("Finished");
			}
		}

		private async void DownloadIqFeedRealTime(RequestBase request, Parameters programParameters)
		{
			using(await _semaphore.LockAsync())
			{
				request.TimeFrameType = "Interval";
				request.BeginDateTime = DateTimeHelper.SubstractOneMinute(request.EndDateTime, false);
				request.EndDateTime = DateTimeHelper.SubstractOneMinute(DateTimeHelper.ConvertLocalToEST(DateTime.Now), false);

				await _dataFeedManager.DownloadMarketData(request, programParameters);
			}
		}

		private async void DownloadFidelityRealTime(RequestBase request, Parameters programParameters)
		{
            using (await _semaphore.LockAsync())
            {
				request.TimeFrameType = "Interval";
				request.BeginDateTime = DateTimeHelper.SubstractOneMinute(request.EndDateTime, true);
				request.EndDateTime = DateTimeHelper.SubstractOneMinute(DateTimeHelper.ConvertLocalToEST(DateTime.Now), true);

                await _dataFeedManager.DownloadMarketData(request, programParameters);
			}
		}
	}
}
