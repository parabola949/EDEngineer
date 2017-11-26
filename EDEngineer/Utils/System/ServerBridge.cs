﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EDEngineer.Utils.UI;
using EDEngineer.Views;

namespace EDEngineer.Utils.System
{
    public class ServerBridge : IDisposable
    {
        private readonly MainWindowViewModel viewModel;
        private CancellationTokenSource cts;

        public bool Running { get; private set; }

        public bool Toggle()
        {
            if (Running)
            {
                Stop();
            }
            else
            {
                Start();
            }

            return Running;
        }

        public ServerBridge(MainWindowViewModel viewModel, bool autoRun)
        {
            this.viewModel = viewModel;

            if (autoRun)
            {
                Start(SettingsManager.ServerPort);
            }
        }

        private void Start(ushort? autoRunPort = null)
        {
            ushort port;
            if (autoRunPort.HasValue)
            {
                port = autoRunPort.Value;
            }
            else if (!TryGetPort(out port))
            {
                return;
            }

            SettingsManager.ServerPort = port;

            cts = new CancellationTokenSource();

            Task.Factory.StartNew(() =>
            {
                Server.start(cts.Token,
                    port,
                    viewModel.Languages,
                    () => viewModel.Commanders.ToDictionary(kv => kv.Key, kv => kv.Value.State),
                    () => viewModel.Commanders.ToDictionary(kv => kv.Key, kv => kv.Value.ShoppingList));
            }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            Running = true;
        }

        public void Stop()
        {
            if (cts?.IsCancellationRequested == false)
            {
                cts.Cancel();
            }

            SettingsManager.AutoRunServer = false;
            Running = false;
        }

        public void Dispose()
        {
            Stop();
        }

        private bool TryGetPort(out ushort port)
        {
            port = SettingsManager.ServerPort != 0 ? SettingsManager.ServerPort : (ushort)44405;

            return ServerPortPrompt.ShowDialog(port, out port);
        }
    }
}
