using System.Windows;

namespace WpfAudioConverter
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Lizenzprüfung entfernt. Direktes Starten des Hauptfensters.
            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(sender as DependencyObject).WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(sender as DependencyObject).Close();
        }
    }
}