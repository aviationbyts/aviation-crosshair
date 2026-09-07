using System;
using System.Windows;
using System.Windows.Threading;

namespace AviationCrosshair
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Belt-and-braces: per the "app must never just crash" requirement,
            // catch anything unhandled, show a message, and keep running where possible.
            DispatcherUnhandledException += (_, args) =>
            {
                MessageBox.Show(
                    "Aviation Crosshair ran into an unexpected error and recovered:\n\n" + args.Exception.Message,
                    "Aviation Crosshair", MessageBoxButton.OK, MessageBoxImage.Warning);
                args.Handled = true;
            };

            var main = new MainWindow();
            main.Show();
        }
    }
}
