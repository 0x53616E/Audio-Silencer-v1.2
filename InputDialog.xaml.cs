using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfAudioConverter
{
    public partial class InputDialog : Window
    {
        public string Answer { get; private set; }
        private readonly string _defaultValue;

        // Farben
        private readonly Brush _placeholderColor = new SolidColorBrush(Color.FromArgb(100, 255, 255, 255)); // Grau transparent
        private readonly Brush _textColor = Brushes.White; // Helles Weiß

        public InputDialog(string question, string defaultValue = "")
        {
            InitializeComponent();
            lblQuestion.Text = question;
            _defaultValue = defaultValue;

            txtAnswer.Text = _defaultValue;

            // LOGIK GEÄNDERT:
            // Wenn der Wert "0.0" ist -> Platzhalter (Grau)
            // Wenn der Wert etwas anderes ist (z.B. "-0.078") -> Echter Wert (Weiß)
            if (_defaultValue == "0.0")
            {
                txtAnswer.Foreground = _placeholderColor;
            }
            else
            {
                txtAnswer.Foreground = _textColor;
            }

            // Beim Starten Fokus setzen und Text markieren
            Loaded += (s, e) =>
            {
                txtAnswer.Focus();
                txtAnswer.SelectAll();
            };
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            // Wenn immer noch der graue "0.0" Platzhalter da steht, nehmen wir den Wert trotzdem
            this.Answer = txtAnswer.Text;
            this.DialogResult = true;
        }

        private void TxtAnswer_GotFocus(object sender, RoutedEventArgs e)
        {
            // Lösche den Text NUR, wenn es der Platzhalter "0.0" ist
            if (txtAnswer.Text == "0.0")
            {
                txtAnswer.Text = "";
                txtAnswer.Foreground = _textColor;
            }
        }

        private void TxtAnswer_LostFocus(object sender, RoutedEventArgs e)
        {
            // Wenn leer gelassen, setze Standard wieder ein
            if (string.IsNullOrWhiteSpace(txtAnswer.Text))
            {
                txtAnswer.Text = _defaultValue;

                if (_defaultValue == "0.0")
                    txtAnswer.Foreground = _placeholderColor;
                else
                    txtAnswer.Foreground = _textColor;
            }
        }
    }
}