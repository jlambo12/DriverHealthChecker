using System;
using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace DriverHealthChecker.App
{
    public partial class App : Application
    {
        private const string RepoUrl = "https://github.com/jlambo12/Driver-checker-";

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // запуск Velopack
            VelopackApp.Build().Run();

            await CheckForUpdatesOnStartupAsync();
        }

        private async Task CheckForUpdatesOnStartupAsync()
        {
            try
            {
                var source = new GithubSource(RepoUrl, null, false);
                var updateManager = new UpdateManager(source);

                if (!updateManager.IsInstalled)
                    return;

                var update = await updateManager.CheckForUpdatesAsync();
                if (update == null)
                    return;

                var askDownload = MessageBox.Show(
                    $"Найдена новая версия: {update.TargetFullRelease.Version}\n\nСкачать?",
                    "Обновление",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (askDownload != MessageBoxResult.Yes)
                    return;

                await updateManager.DownloadUpdatesAsync(update);

                var askRestart = MessageBox.Show(
                    "Обновление скачано.\nПерезапустить?",
                    "Готово",
                    MessageBoxButton.YesNo);

                if (askRestart == MessageBoxResult.Yes)
                {
                    updateManager.ApplyUpdatesAndRestart(update);
                }
            }
            catch
            {
                // тихо игнорим для MVP
            }
        }
    }
}