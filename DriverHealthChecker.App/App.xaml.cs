using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace DriverHealthChecker.App
{
    public partial class App : Application
    {
        private const string RepoUrl = "https://github.com/jlambo12/DriverHealthChecker";

        public App()
        {
            VelopackApp.Build().Run();
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Не блокируем запуск UI.
            _ = CheckForUpdatesOnStartupAsync();
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

                // Тихая загрузка без popup окон.
                await updateManager.DownloadUpdatesAsync(update);

                var askRestart = MessageBox.Show(
                    $"Обновление {update.TargetFullRelease.Version} уже скачано.\n\nПерезапустить приложение и установить его сейчас?",
                    "Обновление готово",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (askRestart == MessageBoxResult.Yes)
                {
                    updateManager.ApplyUpdatesAndRestart(update);
                }
            }
            catch
            {
                // На старте молча пропускаем ошибки проверки и скачивания обновлений.
            }
        }
    }
}