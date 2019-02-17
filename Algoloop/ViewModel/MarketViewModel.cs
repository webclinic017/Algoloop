﻿/*
 * Copyright 2018 Capnode AB
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Algoloop.Model;
using Algoloop.Service;
using Algoloop.ViewSupport;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using QuantConnect.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Algoloop.ViewModel
{
    public class MarketViewModel : ViewModelBase, ITreeViewModel
    {
        private readonly MarketsViewModel _parent;
        private readonly SettingsModel _settingsModel;
        private CancellationTokenSource _cancel;
        private MarketModel _model;
        private Isolated<Toolbox> _toolbox;

        public MarketViewModel(MarketsViewModel marketsViewModel, MarketModel marketModel, SettingsModel settingsModel)
        {
            _parent = marketsViewModel;
            Model = marketModel;
            _settingsModel = settingsModel;

            AddSymbolCommand = new RelayCommand(() => AddSymbol(), true);
            ImportSymbolsCommand = new RelayCommand(() => ImportSymbols(), true);
            DeleteCommand = new RelayCommand(() => _parent?.DeleteMarket(this), () => !Active);
            NewListCommand = new RelayCommand(() => Folders.Add(new FolderViewModel(this, new FolderModel())), () => !Active);
            ActiveCommand = new RelayCommand(() => OnActiveCommand(Model.Active), true);
            StartCommand = new RelayCommand(() => OnStartCommand(), () => !Active);
            StopCommand = new RelayCommand(() => OnStopCommand(), () => Active);

            DataFromModel();
        }

        public RelayCommand AddSymbolCommand { get; }
        public RelayCommand ImportSymbolsCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand NewListCommand { get; }
        public RelayCommand ActiveCommand { get; }
        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }

        public SyncObservableCollection<SymbolViewModel> Symbols { get; } = new SyncObservableCollection<SymbolViewModel>();
        public SyncObservableCollection<FolderViewModel> Folders { get; } = new SyncObservableCollection<FolderViewModel>();

        public bool Active
        {
            get => Model.Active;
            set
            {
                Model.Active = value;
                RaisePropertyChanged(() => Active);
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        public MarketModel Model
        {
            get => _model;
            set => Set(ref _model, value);
        }

        public void Refresh()
        {
            Model.Refresh();
            foreach (FolderViewModel folder in Folders)
            {
                folder.Refresh();
            }
        }

        internal void DataToModel()
        {
            Model.Symbols.Clear();
            foreach (SymbolViewModel symbol in Symbols)
            {
                Model.Symbols.Add(symbol.Model);
            }

            Model.Folders.Clear();
            foreach (FolderViewModel folder in Folders)
            {
                Model.Folders.Add(folder.Model);
                folder.DataToModel();
            }
        }

        internal void DataFromModel()
        {
            Active = Model.Active;
            Symbols.Clear();
            foreach (SymbolModel symbolModel in Model.Symbols)
            {
                var symbolViewModel = new SymbolViewModel(this, symbolModel);
                Symbols.Add(symbolViewModel);
            }

            Folders.Clear();
            foreach (FolderModel folderModel in Model.Folders)
            {
                var folderViewModel = new FolderViewModel(this, folderModel);
                Folders.Add(folderViewModel);
            }
        }

        internal bool DeleteFolder(FolderViewModel symbol)
        {
            return Folders.Remove(symbol);
        }


        internal bool DeleteSymbol(SymbolViewModel symbol)
        {
            return Symbols.Remove(symbol);
        }

        internal async Task StartTaskAsync()
        {
            DataToModel();
            MarketModel model = Model;
            _cancel = new CancellationTokenSource();
            while (!_cancel.Token.IsCancellationRequested && Model.Active)
            {
                Log.Trace($"{Model.Provider} download {Model.Resolution} {Model.FromDate:d}");
                try
                {
                    _toolbox = new Isolated<Toolbox>();
                    _cancel = new CancellationTokenSource();
                    await Task.Run(() => model = _toolbox.Value.Run(Model, _settingsModel, new HostDomainLogger()), _cancel.Token);
                    _toolbox.Dispose();
                    _toolbox = null;
                }
                catch (AppDomainUnloadedException)
                {
                    Log.Trace($"Market {Model.Name} canceled by user");
                    _toolbox = null;
                    Active = false;
                }
                catch (Exception ex)
                {
                    Log.Trace($"{ex.GetType()}: {ex.Message}");
                    _toolbox.Dispose();
                    _toolbox = null;
                    Active = false;
                }

                // Update view
                Model = null;
                Model = model;
                DataFromModel();
            }

            Log.Trace($"{Model.Provider} download complete");
            _cancel = null;
        }

        private void StopTask()
        {
            if (_cancel != null)
            {
                _cancel.Cancel();
            }

            if (_toolbox != null)
            {
                _toolbox.Dispose();
            }
        }

        private async void OnActiveCommand(bool value)
        {
            if (value)
            {
                await StartTaskAsync();
            }
            else
            {
                StopTask();
            }
        }

        private async void OnStartCommand()
        {
            Active = true;
            await StartTaskAsync();
        }

        private void OnStopCommand()
        {
            StopTask();
            Active = false;
        }

        private void AddSymbol()
        {
            var symbol = new SymbolViewModel(this, new SymbolModel());
            Symbols.Add(symbol);
            Folders.ToList().ForEach(m => m.Refresh());
        }

        private void ImportSymbols()
        {
        }
    }
}
