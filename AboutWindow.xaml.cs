using System.Diagnostics;
using System.Windows;
using System.Windows.Input; // Required for MouseButtonEventArgs
using System.Windows.Navigation;

namespace WpfAudioConverter
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        // 1. Dragging Logic (Required for the new borderless design)
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // 2. Close Window (Used by both the OK button and the 'X' button)
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // 3. Hyperlink Logic
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // UseShellExecute=true ensures the link opens in the default browser on modern .NET
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch
            {
                // Silently fail if no browser is found
            }
        }
    }
}