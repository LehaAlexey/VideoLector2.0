using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using VideoLecturer.Infrastructure.PythonAPI;
using VideoLecturer.Infrastructure.Storage;
using WpfApplication.Core.ViewModels;

namespace VideoLecturer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string SolutionPath = FindSolutionPath(BaseDirectory);

        private static string FindSolutionPath(string startPath)
        {
            var directory = new DirectoryInfo(startPath);
            while (directory != null && !directory.GetFiles("*.sln").Any())
            {
                directory = directory.Parent;
            }
            return directory?.FullName ?? startPath;
        }

        private static readonly string StoragePath = Path.Combine(SolutionPath, "projects");
        private static readonly string FragmentPath = Path.Combine(SolutionPath, "generated");
        private static readonly string LectorPath = Path.Combine(SolutionPath, "lectors");

        private static void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(StoragePath);
            Directory.CreateDirectory(FragmentPath);
            Directory.CreateDirectory(LectorPath);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            EnsureDirectoriesExist();

            var projectManager = new ProjectStorage(StoragePath, FragmentPath);
            var lectorManager = new LectorManager(LectorPath);

            var mainVm = new MainViewModel(projectManager, lectorManager, new PythonApiService());
            var mainWindow = new MainWindow { DataContext = mainVm };
            mainWindow.Show();
        }
    }

}
