using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DownloaderUI.ViewModels;
using DownloaderUI.Views;
using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;

namespace DownloaderUI
{
    public partial class App : Application
    {
        public static ReactiveCommand<Unit, Unit> OpenMainWindowCommand { get; private set; }
        public static ReactiveCommand<Unit, Unit> ExitCommand { get; private set; }

        private static bool IsHidden = false;
        private static bool IsExit = false;

        public static MainWindow MainWindow { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(),
                };
                App.MainWindow = desktop.MainWindow as MainWindow;
            }

            base.OnFrameworkInitializationCompleted();

            App.MainWindow.Closing += (s, e) =>
            {
                if (!IsExit)
                {
                    ((Window)s).Hide();
                    bool a = IsHidden;
                    IsHidden = true;
                    e.Cancel = true;
                }
            };
        }

        public App()
        {
            OpenMainWindowCommand = ReactiveCommand.CreateFromTask(OpenMainWindowAsync);
            ExitCommand = ReactiveCommand.CreateFromTask(ExitAsync);
        }

        public static async Task OpenMainWindowAsync()
        {
            try
            {
                if (IsHidden)
                {
                    IsHidden = false;
                    App.MainWindow.Show();
                }
            }
            catch (Exception ex)
            {

            }
        }

        // when exit from tray, set "IsExit = true" for closing the app
        public static async Task ExitAsync()
        {
            try
            {
                IsExit = true;
                App.MainWindow.Close();
            }
            catch (Exception ex)
            {

            }
        }
    }
}