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
                try
                {
                    var logPath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "AviationCrosshair", "crash.log");
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath)!);
                    System.IO.File.WriteAllText(logPath, args.Exception.ToString());
                }
                catch { /* best effort */ }

                MessageBox.Show(
                    "Aviation Crosshair ran into an unexpected error and recovered:\n\n" + args.Exception.ToString(),
                    "Aviation Crosshair", MessageBoxButton.OK, MessageBoxImage.Warning);
                args.Handled = true;
            };

            var main = new MainWindow();
            main.Show();
        }
    }
}
