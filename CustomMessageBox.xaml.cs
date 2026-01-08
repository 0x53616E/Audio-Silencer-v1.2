using System.Windows;
using System.Windows.Input;

namespace WpfAudioConverter
{
    public partial class CustomMessageBox : Window
    {
        // Constructor
        // Changed to 'public' to ensure WPF can instantiate it correctly if needed, 
        // though your static method handles the creation.
        public CustomMessageBox(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        // --- RESTORED STATIC METHOD ---
        // This allows you to call CustomMessageBox.Show("Message") just like before.
        public static void Show(string message)
        {
            var dialog = new CustomMessageBox(message);
            dialog.ShowDialog();
        }

        // --- NEW LOGIC FOR GLASS WINDOW ---

        // 1. Dragging Logic (Required because WindowStyle="None")
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        // 2. Close/OK Button Logic
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}