﻿/*
 * Copyright 2020 Capnode AB
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
using Algoloop.Wpf.Common;
using QuantConnect;
using QuantConnect.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;

namespace Algoloop.Wpf.ViewModel
{
    public class ResearchViewModel : ViewModel, IDisposable
    {
        private const string _notebook = "Notebook";

        private readonly SettingModel _settings;
        private string _htmlText;
        private string _source;
        private ConfigProcess _process;
        private bool _initialized;
        private bool _disposed;

        public ResearchViewModel(SettingModel settings)
        {
            _settings = settings;
            Debug.Assert(IsUiThread(), "Not UI thread!");
        }

        ~ResearchViewModel()
        {
            Dispose(false);
        }

        public string HtmlText
        {
            get => _htmlText;
            set => Set(ref _htmlText, value);
        }

        public string Source
        {
            get => _source;
            set => Set(ref _source, value);
        }

        public bool Initialized
        {
            get => _initialized;
            set => Set(ref _initialized, value);
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed state (managed objects).
                _process.Dispose();
            }

            StopJupyter();
            _disposed = true;
        }

        public void Initialize()
        {
            try
            {
                StartJupyter();
                Initialized = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Python must also be installed to use Research page.\nSee: https://github.com/QuantConnect/Lean/tree/master/Algorithm.Python#quantconnect-python-algorithm-project \n");
                Initialized = false;
            }
        }

        private void StartJupyter()
        {
            StopJupyter();
            SetNotebookFolder();
            _process = new ConfigProcess(
                "jupyter.exe",
                $"notebook --no-browser",
                _settings.Notebook,
                (line) => Log.Trace(line),
                (line) =>
                    {
                        if (string.IsNullOrEmpty(line)) return;

                        int pos = line.IndexOf("http", StringComparison.OrdinalIgnoreCase);
                        if (string.IsNullOrEmpty(Source) && pos > 0)
                        {
                            Source = line.Substring(pos);
                        }

                        Log.Trace(line);
                    });

            // Set PYTHONPATH
            StringDictionary environment = _process.Environment;
            string pythonpath = environment["PYTHONPATH"];
            if (string.IsNullOrEmpty(pythonpath))
            {
                environment["PYTHONPATH"] = MainService.GetProgramFolder();
            }
            else
            {
                environment["PYTHONPATH"] = MainService.GetProgramFolder() + ";" + pythonpath;
            }

            // Set config file
            IDictionary<string, string> config = _process.Config;
            string exeFolder = MainService.GetProgramFolder();
            config["algorithm-language"] = Language.Python.ToString();
            config["composer-dll-directory"] = exeFolder.Replace("\\", "/");
            config["data-folder"] = _settings.DataFolder.Replace("\\", "/");
            config["api-handler"] = "QuantConnect.Api.Api";
            config["job-queue-handler"] = "QuantConnect.Queues.JobQueue";
            config["messaging-handler"] = "QuantConnect.Messaging.Messaging";

            // Start process
            _process.Start();
        }

        public void StopJupyter()
        {
            if (!Initialized) return;

            bool stopped = _process.Stop();
            Log.Trace($"Jupyter process exit: {stopped}");
        }

        private void SetNotebookFolder()
        {
            if (string.IsNullOrEmpty(_settings.Notebook))
            {
                string userDataFolder = MainService.GetUserDataFolder();
                _settings.Notebook = Path.Combine(userDataFolder, _notebook);
                Directory.CreateDirectory(_settings.Notebook);
            }
        }
    }
}
