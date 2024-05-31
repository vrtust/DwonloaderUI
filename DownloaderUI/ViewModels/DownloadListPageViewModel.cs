﻿using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Downloader;
using DownloaderUI.Models;
using DownloaderUI.Views;
using DynamicData;
using FluentAvalonia.UI.Controls;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DownloaderUI.ViewModels
{
    public class DownloadListPageViewModel : ViewModelBase
    {
        private SourceCache<DownloadItem, Guid> _sourceCache = new(i => i.Id);

        private readonly ReadOnlyObservableCollection<DownloadItem> _downloadList;
        public ReadOnlyObservableCollection<DownloadItem> DownloadList => _downloadList;

        private readonly ReadOnlyObservableCollection<DownloadItem> _downloadingList;
        public ReadOnlyObservableCollection<DownloadItem> DownloadingList => _downloadingList;

        private readonly ReadOnlyObservableCollection<DownloadItem> _completedList;
        public ReadOnlyObservableCollection<DownloadItem> CompletedList => _completedList;

        private readonly ReadOnlyObservableCollection<DownloadItem> _errorList;
        public ReadOnlyObservableCollection<DownloadItem> ErrorList => _errorList;

        private string _searchText;

        public string SearchText
        {
            get => _searchText;
            set => this.RaiseAndSetIfChanged(ref _searchText, value);
        }

        private DownloadCollection _downloadCollection;

        public DownloadCollection DownloadCollection
        {
            get => _downloadCollection;
            set => this.RaiseAndSetIfChanged(ref _downloadCollection, value);
        }

        private Dictionary<string, DownloadCollection> _downloadCollections = new();

        public Dictionary<string, DownloadCollection> DownloadCollections
        {
            get => _downloadCollections;
            set => this.RaiseAndSetIfChanged(ref _downloadCollections, value);
        }


        private string _downloadServicesJson;
        public string DownloadServicesJson
        {
            get => _downloadServicesJson;
            set => this.RaiseAndSetIfChanged(ref _downloadServicesJson, value);
        }

        private string _testMessage;

        public string TestMessage
        {
            get => _testMessage;
            set => this.RaiseAndSetIfChanged(ref _testMessage, value);
        }

        private DownloadItem _selectedDownloadItem;

        public DownloadItem SelectedDownloadItem
        {
            get => _selectedDownloadItem;
            set => this.RaiseAndSetIfChanged(ref _selectedDownloadItem, value);
        }

        public ReactiveCommand<Unit, Unit> NewLinkCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenCommand { get; }

        public ReactiveCommand<Unit, Unit> OpenFolderCommand { get; }

        public ReactiveCommand<Unit, Unit> CopyNameCommand { get; }

        public ReactiveCommand<Unit, Unit> CopyPathCommand { get; }

        public ReactiveCommand<Unit, Unit> CopyUrlCommand { get; }

        public ReactiveCommand<Unit, Unit> CopyExMessageCommand { get; }

        public ReactiveCommand<Unit, Unit> PauseCommand { get; }

        public ReactiveCommand<Unit, Unit> ResumeCommand { get; }

        public ReactiveCommand<Unit, Unit> DeleteCommand { get; }

        public ReactiveCommand<Unit, Unit> DeleteFromListCommand { get; }

        public ReactiveCommand<Unit, Unit> DeleteFromFileCommand { get; }

        public DownloadListPageViewModel()
        {
            NewLinkCommand = ReactiveCommand.CreateFromTask(NewLinkAsync);
            OpenCommand = ReactiveCommand.CreateFromTask(OpenAsync);
            OpenFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);
            CopyNameCommand = ReactiveCommand.CreateFromTask(CopyNameAsync);
            CopyPathCommand = ReactiveCommand.CreateFromTask(CopyPathAsync);
            CopyUrlCommand = ReactiveCommand.CreateFromTask(CopyUrlAsync);
            CopyExMessageCommand = ReactiveCommand.CreateFromTask(CopyExMessageAsync);
            PauseCommand = ReactiveCommand.CreateFromTask(PauseAsync);
            ResumeCommand = ReactiveCommand.CreateFromTask(ResumeAsync);
            DeleteCommand = ReactiveCommand.CreateFromTask(DeleteAsync);
            DeleteFromListCommand = ReactiveCommand.CreateFromTask(DeleteFromListAsync);
            DeleteFromFileCommand = ReactiveCommand.CreateFromTask(DeleteFromFileAsync);

            DownloadItem downloadItem1 = new()
            {
                Id = Guid.NewGuid(),
                FileName = "a",
                FileSize = "aa",
                Status = "Downloading",
                ProgressPercentage = 50,
                Path = Path.Combine("F:\\a", "asd.txt")
            };

            /*DownloadItem downloadItem2 = new()
            {
                Id = Guid.NewGuid(),
                FileName = "阿斯蒂芬",
                FileSize = "bb",
                Status = "Downloading",
                ProgressPercentage = 50
            };*/

            DownloadItem downloadItem3 = new()
            {
                Id = Guid.NewGuid(),
                FileName = "c",
                FileSize = "cc",
                Status = "Completed",
                ProgressPercentage = 50
            };

            DownloadItem downloadItem4 = new()
            {
                Id = Guid.NewGuid(),
                FileName = "d",
                FileSize = "dd",
                Status = "Error",
                ProgressPercentage = 50
            };

            DownloadItem downloadItem5 = new()
            {
                Id = Guid.NewGuid(),
                FileName = "e",
                FileSize = "ee",
                Status = "Pause",
                ProgressPercentage = 50
            };

            DownloadItem downloadItem6 = new()
            {
                Id = Guid.NewGuid(),
                FileName = "f",
                FileSize = "ff",
                Status = "Error",
                ExMessage = "asdaaaaa",
                ProgressPercentage = 50
            };

            /*DownloadItem downloadItem3 = new()
            {
                Id = Guid.NewGuid(),
                FileName = "~!@#$%^&*()_+`1234567890-=[]\\;',./{}:\"<>?·1234567890-=~！@#￥%……&*（）——+【】、；‘，。/{}|：“《》？",
                FileSize = "bb",
                ProgressPercentage = 50
            };*/

            _sourceCache.AddOrUpdate(downloadItem1);
            //_sourceCache.AddOrUpdate(downloadItem2);
            _sourceCache.AddOrUpdate(downloadItem3);
            _sourceCache.AddOrUpdate(downloadItem4);
            _sourceCache.AddOrUpdate(downloadItem5);
            _sourceCache.AddOrUpdate(downloadItem6);
            // _sourceCache.AddOrUpdate(downloadItem3);

            // Monitor changes of the SearchText property
            this.WhenAnyValue(vm => vm.SearchText)
                .Throttle(TimeSpan.FromMilliseconds(200)) // Reduce trigger frequency
                .Subscribe(searchText =>
                {
                    _sourceCache.Refresh();
                });

            _sourceCache.Connect()
                .Filter(x => string.IsNullOrWhiteSpace(SearchText) || x.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _downloadList)
                .DisposeMany()
                .Subscribe();

            _sourceCache.Connect()
                .Filter(x => (!string.IsNullOrWhiteSpace(x.Status) && x.Status.Equals("Downloading")) && (string.IsNullOrWhiteSpace(SearchText) || x.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _downloadingList)
                .DisposeMany()
                .Subscribe();

            _sourceCache.Connect()
                .Filter(x => (!string.IsNullOrWhiteSpace(x.Status) && x.Status.Equals("Completed")) && (string.IsNullOrWhiteSpace(SearchText) || x.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _completedList)
                .DisposeMany()
                .Subscribe();

            _sourceCache.Connect()
                .Filter(x => (!string.IsNullOrWhiteSpace(x.Status) && x.Status.Equals("Error")) && (string.IsNullOrWhiteSpace(SearchText) || x.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _errorList)
                .DisposeMany()
                .Subscribe();
        }

        public async Task ExDialog(string ex, string From)
        {
            var dialog = new ContentDialog()
            {
                Title = $"Error in {From}",
                Content = $"Error in {From}: {ex}",
                PrimaryButtonText = "Copy Error Message",
                PrimaryButtonCommand = CopyExMessageCommand,
                CloseButtonText = "Close",
            };

            await dialog.ShowAsync();
        }

        private async Task OpenAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SelectedDownloadItem.Path))
                {
                    await ExDialog("Can't Find The File", "OpenAsync");
                }
                else
                {
                    Process proc = new Process();
                    proc.StartInfo = new ProcessStartInfo()
                    {
                        FileName = SelectedDownloadItem.Path,
                        UseShellExecute = true
                    };
                    proc.Start();
                }
            }
            catch (Exception ex)
            {
                await ExDialog(ex.Message, "OpenAsync");
            }
        }

        private async Task OpenFolderAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SelectedDownloadItem.Path))
                {
                    await ExDialog("Can't Find The Folder", "OpenFolderAsync");
                }
                else
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select, \"{SelectedDownloadItem.Path}\"",
                            UseShellExecute = true,
                            CreateNoWindow = true
                        });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = "xdg-open", // for Linux
                            Arguments = SelectedDownloadItem.Path,
                            UseShellExecute = true,
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                await ExDialog(ex.Message, "OpenFolderAsync");
            }
        }

        private async Task CopyExMessageAsync()
        {
            try
            {
                var clipboard = App.MainWindow.Clipboard;

                var dataObject = new DataObject();
                dataObject.Set(DataFormats.Text, SelectedDownloadItem.ExMessage);
                await clipboard.SetDataObjectAsync(dataObject);
            }
            catch (Exception ex)
            {
                await ExDialog(ex.Message, "CopyExMessageAsync");
            }
        }

        private async Task DeleteFromFileAsync()
        {
            try
            {
                await PauseAsync();
                File.Delete(SelectedDownloadItem.Path);
                File.Delete(SelectedDownloadItem.Path + ".json");
            }
            catch (Exception ex)
            {
                await ExDialog(ex.Message, "DeleteFromFileAsync");
            }
        }

        private async Task DeleteFromListAsync()
        {
            try
            {
                this.SelectedDownloadItem = SelectedDownloadItem;
                DownloadCollections.Remove(this.SelectedDownloadItem.FileName);
                _sourceCache.Remove(this.SelectedDownloadItem);
                _sourceCache.Refresh();
                await SaveAsync();
                _sourceCache.Refresh();
            }
            catch (Exception ex)
            {
                await ExDialog(ex.Message, "DeleteFromListAsync");
            }
        }

        private async Task DeleteAsync()
        {
            try
            {
                await DeleteFromFileAsync();
            }
            catch (Exception ex)
            {
                await ExDialog(ex.Message, "DeleteAsync");
            }
            try
            {
                await DeleteFromListAsync();
            }
            catch (Exception ex)
            {
                await ExDialog(ex.Message, "DeleteAsync");
            }
        }

        public async Task CopyNameAsync()
        {
            try
            {
                var clipboard = App.MainWindow.Clipboard;

                var dataObject = new DataObject();
                dataObject.Set(DataFormats.Text, SelectedDownloadItem.FileName);
                await clipboard.SetDataObjectAsync(dataObject);
            }
            catch (Exception ex)
            {
                await ExDialog(ex.Message, "CopyNameAsync");
            }
        }

        public async Task CopyPathAsync()
        {
            try
            {
                var clipboard = App.MainWindow.Clipboard;

                var dataObject = new DataObject();
                dataObject.Set(DataFormats.Text, SelectedDownloadItem.Path);
                await clipboard.SetDataObjectAsync(dataObject);
            }
            catch (Exception ex)
            {
                await ExDialog(ex.Message, "CopyPathAsync");
            }
        }

        public async Task CopyUrlAsync()
        {
            try
            {
                var clipboard = App.MainWindow.Clipboard;

                var dataObject = new DataObject();
                dataObject.Set(DataFormats.Text, SelectedDownloadItem.Url);
                await clipboard.SetDataObjectAsync(dataObject);
            }
            catch (Exception ex)
            {
                await ExDialog(ex.Message, "CopyUrlAsync");
            }
        }

        public async Task NewLinkAsync()
        {
            DownloadItem downloadItem = new();

            var newWindow = new Window
            {
                Width = 625,
                Height = 325,
                Content = new DownloadPage(),
                ExtendClientAreaToDecorationsHint = true,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            // set current window as owner of new window
            newWindow.ShowDialog(App.MainWindow);

            _ = MessageBus.Current.Listen<DownloadFileAddedMessage>().Subscribe((Action<DownloadFileAddedMessage>)(async message =>
            {
                downloadItem = message.DownloadItem;

                newWindow.Close();

                if (downloadItem != null)
                {
                    if (Enumerable.Any<DownloadItem>(this.DownloadList, (Func<DownloadItem, bool>)(df => df.FileName == downloadItem.FileName)))
                    {
                        var dialog = new ContentDialog()
                        {
                            Title = $"{downloadItem.FileName} is already existed!",
                            PrimaryButtonText = "Ok",
                            CloseButtonText = "Close"
                        };

                        await dialog.ShowAsync();
                    }
                    else
                    {
                        long MaximumBytesPerSecond = 0;
                        switch (DownloadSettings.Instance.UnitForMaximumBytesPerSecond)
                        {
                            case "B":
                                MaximumBytesPerSecond = DownloadSettings.Instance.MaximumBytesPerSecond;
                                break;
                            case "KB":
                                MaximumBytesPerSecond = DownloadSettings.Instance.MaximumBytesPerSecond * 1024;
                                break;
                            case "MB":
                                MaximumBytesPerSecond = DownloadSettings.Instance.MaximumBytesPerSecond * 1024 * 1024;
                                break;
                            case "GB":
                                MaximumBytesPerSecond = DownloadSettings.Instance.MaximumBytesPerSecond * 1024 * 1024 * 1024;
                                break;
                        }
                        long MaximumMemoryBufferBytes = 0;
                        switch (DownloadSettings.Instance.UnitForMaximumMemoryBufferBytes)
                        {
                            case "B":
                                MaximumBytesPerSecond = DownloadSettings.Instance.MaximumBytesPerSecond;
                                break;
                            case "KB":
                                MaximumBytesPerSecond = DownloadSettings.Instance.MaximumBytesPerSecond * 1024;
                                break;
                            case "MB":
                                MaximumBytesPerSecond = DownloadSettings.Instance.MaximumBytesPerSecond * 1024 * 1024;
                                break;
                            case "GB":
                                MaximumBytesPerSecond = DownloadSettings.Instance.MaximumBytesPerSecond * 1024 * 1024 * 1024;
                                break;
                        }

                        DownloadConfiguration downloadOpt;

                        if (!string.IsNullOrEmpty(DownloadSettings.Instance.ProxyUri))
                        {
                            downloadOpt = new DownloadConfiguration()
                            {
                                BufferBlockSize = DownloadSettings.Instance.BufferBlockSize,
                                ChunkCount = DownloadSettings.Instance.ChunkCount,
                                MaximumBytesPerSecond = MaximumBytesPerSecond,
                                MaxTryAgainOnFailover = DownloadSettings.Instance.MaxTryAgainOnFailover,
                                MaximumMemoryBufferBytes = DownloadSettings.Instance.MaximumMemoryBufferBytes,
                                ParallelDownload = DownloadSettings.Instance.ParallelDownload,
                                ParallelCount = DownloadSettings.Instance.ParallelCount,
                                Timeout = DownloadSettings.Instance.Timeout,
                                RangeDownload = DownloadSettings.Instance.RangeDownload,
                                RangeLow = DownloadSettings.Instance.RangeLow,
                                RangeHigh = DownloadSettings.Instance.RangeHigh,
                                ClearPackageOnCompletionWithFailure = DownloadSettings.Instance.ClearPackageOnCompletionWithFailure,
                                MinimumSizeOfChunking = DownloadSettings.Instance.MinimumSizeOfChunking,
                                ReserveStorageSpaceBeforeStartingDownload = DownloadSettings.Instance.ReserveStorageSpaceBeforeStartingDownload,
                                RequestConfiguration =
                                {
                                    UserAgent = DownloadSettings.Instance.UserAgent,
                                    Proxy = new WebProxy() {
                                       Address = new Uri(DownloadSettings.Instance.ProxyUri)
                                    }
                                }
                            };
                        }
                        else
                        {
                            downloadOpt = new DownloadConfiguration()
                            {
                                BufferBlockSize = DownloadSettings.Instance.BufferBlockSize,
                                ChunkCount = DownloadSettings.Instance.ChunkCount,
                                MaximumBytesPerSecond = MaximumBytesPerSecond,
                                MaxTryAgainOnFailover = DownloadSettings.Instance.MaxTryAgainOnFailover,
                                MaximumMemoryBufferBytes = DownloadSettings.Instance.MaximumMemoryBufferBytes,
                                ParallelDownload = DownloadSettings.Instance.ParallelDownload,
                                ParallelCount = DownloadSettings.Instance.ParallelCount,
                                Timeout = DownloadSettings.Instance.Timeout,
                                RangeDownload = DownloadSettings.Instance.RangeDownload,
                                RangeLow = DownloadSettings.Instance.RangeLow,
                                RangeHigh = DownloadSettings.Instance.RangeHigh,
                                ClearPackageOnCompletionWithFailure = DownloadSettings.Instance.ClearPackageOnCompletionWithFailure,
                                MinimumSizeOfChunking = DownloadSettings.Instance.MinimumSizeOfChunking,
                                ReserveStorageSpaceBeforeStartingDownload = DownloadSettings.Instance.ReserveStorageSpaceBeforeStartingDownload,
                                RequestConfiguration =
                                {
                                    UserAgent = DownloadSettings.Instance.UserAgent
                                }
                            };
                        }

                        DownloadService CurrentDownloadService = new(downloadOpt);

                        CurrentDownloadService.DownloadStarted += (s, e) =>
                        {
                            downloadItem.FileName = GetFileNameFromUrl(e.FileName);
                            downloadItem.Path = e.FileName;
                            downloadItem.FileSize = FormatBytesFromDouble(e.TotalBytesToReceive);

                            Dispatcher.UIThread.InvokeAsync((Action)(async () =>
                            {
                                try
                                {
                                    if (!Enumerable.Any<DownloadItem>(this.DownloadList, (Func<DownloadItem, bool>)(df => df.FileName == downloadItem.FileName)))
                                    {
                                        downloadItem.Status = "Downloading";
                                        downloadItem.Pack = CurrentDownloadService.Package;
                                        _sourceCache.AddOrUpdate(downloadItem);
                                        DownloadCollection downloadCollection = new DownloadCollection()
                                        {
                                            DownloadItemInfo = DownloadItemToDownloadItemInfo(downloadItem),
                                            DownloadService = CurrentDownloadService,
                                        };
                                        DownloadCollections.Add(downloadItem.FileName, downloadCollection);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    downloadItem.ExMessage = $"{downloadItem.ExMessage}. \nDownloadStarted:{ex.Message}";
                                }

                            }));

                        };

                        DateTime lastUpdate = DateTime.MinValue;

                        CurrentDownloadService.ChunkDownloadProgressChanged += (s, e) =>
                        {

                        };

                        CurrentDownloadService.DownloadProgressChanged += (s, e) =>
                        {
                            if (e.ProgressPercentage == 100)
                            {
                                downloadItem.ProgressPercentage = e.ProgressPercentage;
                                downloadItem.BytesPerSecondSpeed = "0 MB/s";
                                downloadItem.ReceivedBytesSize = FormatBytesFromLong(e.ReceivedBytesSize);
                            }
                            if (DateTime.Now - lastUpdate > TimeSpan.FromMilliseconds(200))
                            {
                                downloadItem.ProgressPercentage = e.ProgressPercentage;
                                downloadItem.BytesPerSecondSpeed = FormatBytesFromDoubleWithSpeed(e.BytesPerSecondSpeed);
                                downloadItem.ReceivedBytesSize = FormatBytesFromLong(e.ReceivedBytesSize);
                                lastUpdate = DateTime.Now;
                            }
                        };

                        CurrentDownloadService.DownloadFileCompleted += async (s, e) =>
                        {
                            downloadItem.Status = "Completed";

                            if (e.Error != null && !string.IsNullOrEmpty(e.Error.Message))
                            {
                                if (e.Error.Message.Equals("A task was canceled."))
                                {
                                    downloadItem.Status = "Pause";
                                }
                                else
                                {
                                    downloadItem.Status = "Error";
                                    downloadItem.Pack = CurrentDownloadService.Package;
                                    DownloadCollection downloadCollection = new DownloadCollection()
                                    {
                                        DownloadItemInfo = DownloadItemToDownloadItemInfo(downloadItem),
                                        DownloadService = CurrentDownloadService,
                                    };

                                    if (!string.IsNullOrEmpty(downloadItem.ExMessage))
                                    {
                                        downloadItem.ExMessage = $"{downloadItem.ExMessage} \nDownloadFileCompleted: {e.Error.Message}";
                                    }
                                    else
                                    {
                                        downloadItem.ExMessage = $"DownloadFileCompleted: {e.Error.Message}";
                                    }
                                }
                            }

                            _sourceCache.Refresh();

                            _ = SaveAsync();

                            if (downloadItem.IsOpen == true)
                            {
                                try
                                {
                                    if (string.IsNullOrEmpty(downloadItem.Path))
                                    {
                                        _ = Dispatcher.UIThread.InvokeAsync((Action)(async () =>
                                        {
                                            await ExDialog("Can't Find The File", "DownloadFileCompleted.IsOpenFolder");
                                        }));
                                    }
                                    else
                                    {
                                        Process proc = new Process();
                                        proc.StartInfo = new ProcessStartInfo()
                                        {
                                            FileName = downloadItem.Path,
                                            UseShellExecute = true
                                        };
                                        proc.Start();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _ = Dispatcher.UIThread.InvokeAsync((Action)(async () =>
                                    {
                                        await ExDialog(ex.Message, "DownloadFileCompleted.IsOpen");
                                    }));
                                }
                            }
                            if (downloadItem.IsOpenFolder == true)
                            {
                                try
                                {
                                    if (string.IsNullOrEmpty(downloadItem.Path))
                                    {
                                        _ = Dispatcher.UIThread.InvokeAsync((Action)(async () =>
                                        {
                                            await ExDialog("Can't Find The Folder", "DownloadFileCompleted.IsOpenFolder");
                                        }));
                                    }
                                    else
                                    {
                                        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                        {
                                            Process.Start(new ProcessStartInfo()
                                            {
                                                FileName = "explorer.exe",
                                                Arguments = $"/select, \"{downloadItem.Path}\"",
                                                UseShellExecute = true,
                                                CreateNoWindow = true
                                            });
                                        }
                                        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                                        {
                                            string directoryPath = Path.GetDirectoryName(downloadItem.Path);
                                            var processStartInfo = new ProcessStartInfo
                                            {
                                                FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "xdg-open" : "open",
                                                Arguments = directoryPath,
                                                UseShellExecute = true,
                                            };

                                            Process.Start(processStartInfo);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _ = Dispatcher.UIThread.InvokeAsync((Action)(async () =>
                                    {
                                        await ExDialog(ex.Message, "DownloadFileCompleted.IsOpenFolder");
                                    }));

                                }
                            }
                        };

                        await Task.Run(() =>
                        {
                            CurrentDownloadService.DownloadFileTaskAsync(downloadItem.Url, new DirectoryInfo(downloadItem.FolderPath));
                        });

                    }
                }
            }));

            // https://xx.xx/xx.zip -> xx.zip
            // F:/xx/xx.zip -> xx.zip
            static string GetFileNameFromUrl(string url)
            {
                Uri uri = new(url);
                string fileName = Path.GetFileName(uri.LocalPath);

                return fileName;
            }
        }

        public async Task SaveAsync()
        {
            string DataPath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DataPath = @".\DownloadData";
            }
            else
            {
                DataPath = @"./DownloadData";
            }

            if (!Directory.Exists("./DownloadData"))
            {
                Directory.CreateDirectory("./DownloadData");
            }

            DownloadServicesJson = JsonConvert.SerializeObject(DownloadCollections);
            
            await File.WriteAllTextAsync(Path.Combine(DataPath, "DownloadServices") + ".json", DownloadServicesJson);

        }

        // xxxxxxxxx -> xx.xxx MB 
        static string FormatBytesFromLong(long bytes)
        {
            string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
            int suffixIndex = 0;
            double formattedValue = bytes;

            while (formattedValue >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                formattedValue /= 1024;
                suffixIndex++;
            }

            return $"{formattedValue:F3} {suffixes[suffixIndex]}";
        }

        static string FormatBytesFromDouble(double bytes)
        {
            string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
            int suffixIndex = 0;
            double formattedValue = bytes;

            while (formattedValue >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                formattedValue /= 1024;
                suffixIndex++;
            }

            return $"{formattedValue:F3} {suffixes[suffixIndex]}";
        }

        static string FormatBytesFromDoubleWithSpeed(double bytes)
        {
            string[] suffixes = ["B/s", "KB/s", "MB/s", "GB/s", "TB/s"];
            int suffixIndex = 0;
            double formattedValue = bytes;

            while (formattedValue >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                formattedValue /= 1024;
                suffixIndex++;
            }

            return $"{formattedValue:F3} {suffixes[suffixIndex]}";
        }

        public async Task PauseAsync()
        {
            try
            {
                if (DownloadCollections.TryGetValue(SelectedDownloadItem.FileName, out var downloadCollection) && SelectedDownloadItem.Status != "Pause")
                {
                    var downloadService = downloadCollection.DownloadService;
                    SelectedDownloadItem.Status = "Pause";
                    await downloadService.CancelTaskAsync();
                    SelectedDownloadItem.PackageJson = JsonConvert.SerializeObject(SelectedDownloadItem.Pack);
                    File.WriteAllText(SelectedDownloadItem.Path + ".json", SelectedDownloadItem.PackageJson);

                }
            }
            catch (Exception ex)
            {
                SelectedDownloadItem.ExMessage = ex.Message;

                await ExDialog(ex.Message, "PauseAsync");
            }
        }

        public async Task ResumeAsync()
        {
            try
            {
                if (DownloadCollections.TryGetValue(SelectedDownloadItem.FileName, out var downloadCollection) && SelectedDownloadItem.Status != "Downloading")
                {
                    var downloadService = downloadCollection.DownloadService;
                    SelectedDownloadItem.Status = "Downloading";
                    await downloadService.DownloadFileTaskAsync(SelectedDownloadItem.Pack);
                    SelectedDownloadItem.PackageJson = File.ReadAllText(SelectedDownloadItem.Path + ".json");
                    SelectedDownloadItem.Pack = JsonConvert.DeserializeObject<DownloadPackage>(SelectedDownloadItem.PackageJson);
                }
            }
            catch (Exception ex)
            {
                SelectedDownloadItem.ExMessage = ex.Message;
                await ExDialog(ex.Message, "ResumeAsync");
            }

        }

        public DownloadItemInfo DownloadItemToDownloadItemInfo(DownloadItem downloadItem)
        {
            return new DownloadItemInfo
            {
                Id = downloadItem.Id,
                FileName = downloadItem.FileName,
                ProgressPercentage = downloadItem.ProgressPercentage,
                FileSize = downloadItem.FileSize,
                FolderPath = downloadItem.FolderPath,
                Path = downloadItem.Path,
                Url = downloadItem.Url,
                Status = downloadItem.Status,
                Pack = downloadItem.Pack,
                PackageJson = downloadItem.PackageJson,
                ExMessage = downloadItem.ExMessage,
                IsOpen = downloadItem.IsOpen,
                IsOpenFolder = downloadItem.IsOpenFolder
            };
        }
    }
}