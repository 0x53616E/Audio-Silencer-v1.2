using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfAudioConverter
{
    public class LogEntry
    {
        public string Text { get; set; }
        public Brush Color { get; set; }
    }

    public class FfmpegResult
    {
        public bool Success { get; set; }
        public string Output { get; set; }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        // ... (Properties & Commands wie gehabt) ...
        private string _statusMessage;
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); } }

        private ObservableCollection<LogEntry> _consoleOutput = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> ConsoleOutput { get => _consoleOutput; set { _consoleOutput = value; OnPropertyChanged(nameof(ConsoleOutput)); } }

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(nameof(IsBusy)); OnPropertyChanged(nameof(IsNotBusy)); } }
        public bool IsNotBusy => !_isBusy;

        private bool _applyLufsNormalization = true;
        public bool ApplyLufsNormalization { get => _applyLufsNormalization; set { _applyLufsNormalization = value; OnPropertyChanged(nameof(ApplyLufsNormalization)); } }

        private bool _calculateMadiValues = true;
        public bool CalculateMadiValues { get => _calculateMadiValues; set { _calculateMadiValues = value; OnPropertyChanged(nameof(CalculateMadiValues)); } }

        private string _licenseStatus;
        public string LicenseStatus { get => _licenseStatus; set { _licenseStatus = value; OnPropertyChanged(nameof(LicenseStatus)); } }

        public ICommand StartConversionCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand GitHubCommand { get; }

        public MainViewModel()
        {
            StartConversionCommand = new RelayCommand(StartConversion, _ => IsNotBusy);
            AboutCommand = new RelayCommand(ShowAbout);
            GitHubCommand = new RelayCommand(OpenGitHub);

            StatusMessage = "Ready";
            LicenseStatus = "Public Version";
        }

        private void Log(string message, Brush color = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (color == null) color = Brushes.White;
                ConsoleOutput.Add(new LogEntry { Text = message, Color = color });
            });
        }

        private void StartConversion(object obj)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Audio File",
                Filter = "Audio Files|*.flac;*.wav;*.ogg;*.mp3|FLAC Files|*.flac|WAV Files|*.wav|OGG Files|*.ogg|MP3 Files|*.mp3"
            };

            if (openFileDialog.ShowDialog() != true) return;

            ProcessFile(openFileDialog.FileName);
        }

        public async void ProcessFile(string inputFile)
        {
            string extension = Path.GetExtension(inputFile).ToLowerInvariant();

            string[] validExtensions = { ".flac", ".wav", ".ogg", ".mp3" };
            if (!validExtensions.Contains(extension))
            {
                Log($"Error: Unsupported file type '{extension}'.", Brushes.Red);
                StatusMessage = "Invalid File";
                return;
            }

            IsBusy = true;
            ConsoleOutput.Clear();
            StatusMessage = "Processing...";

            await Task.Run(() =>
            {
                switch (extension)
                {
                    case ".flac":
                        ConvertFlacToWav(inputFile);
                        break;
                    case ".ogg":
                        ConvertOggToWav(inputFile);
                        break;
                    case ".mp3":
                        ConvertMp3ToWav(inputFile);
                        break;
                    case ".wav":
                        ConvertWavToSpecialOgg(inputFile);
                        break;
                }
            });

            IsBusy = false;
        }

        private void ConvertToStandardWav(string inputFile)
        {
            string outputPath = Path.GetDirectoryName(inputFile);
            string outputFile = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(inputFile) + ".wav");
            string arguments = $"-i \"{inputFile}\" -c:a pcm_s16le -ar 44100 -rf64 never -map_metadata -1 -fflags +bitexact -y \"{outputFile}\"";

            var result = RunFfmpeg(arguments);
            HandleConversionResult(result, outputFile);
        }

        private void ConvertFlacToWav(string inputFile) => ConvertToStandardWav(inputFile);
        private void ConvertOggToWav(string inputFile) => ConvertToStandardWav(inputFile);
        private void ConvertMp3ToWav(string inputFile) => ConvertToStandardWav(inputFile);

        // --- SILENCER LOGIK (WAV -> OGG mit 96kbps / 44.1kHz) ---
        private void ConvertWavToSpecialOgg(string inputFile)
        {
            double manualOffsetSeconds = 0, bpm = 0, time = 0;
            bool cancelled = false;

            // 1. OFFSET
            string defaultOffset = "0.0";
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        string clipText = Clipboard.GetText();
                        var match = Regex.Match(clipText, @"(?<!\d)-?\d+[\.,]\d{3}(?!\d)");
                        if (match.Success) defaultOffset = match.Value.Replace(",", ".");
                    }
                }
                catch { }

                if (!PromptForValue("Enter Manual Offset (e.g. 1.234)", defaultOffset, out manualOffsetSeconds))
                {
                    cancelled = true;
                }
            });

            if (cancelled) { Application.Current.Dispatcher.Invoke(() => StatusMessage = "Cancelled."); return; }

            // 2. BPM & TIME
            if (CalculateMadiValues)
            {
                Application.Current.Dispatcher.Invoke(() => StatusMessage = "Detecting BPM...");

                double detectedBpm = GetBpmFromAudio(inputFile);
                bpm = detectedBpm > 0 ? detectedBpm : 120.0;
                string defaultBpmString = FormatBpmToString(bpm);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!PromptForValue("Enter BPM", defaultBpmString, out bpm)) { cancelled = true; return; }
                    if (!PromptForValue("Enter Time (First Space)", "0.0", out time)) { cancelled = true; return; }
                });
            }

            if (cancelled) { Application.Current.Dispatcher.Invoke(() => StatusMessage = "Cancelled."); return; }

            // 3. LOGIC
            Application.Current.Dispatcher.Invoke(() => StatusMessage = "Processing...");

            string originalName = Path.GetFileNameWithoutExtension(inputFile);
            string outputPath = Path.GetDirectoryName(inputFile);
            string newFileName;
            var audioFilters = new List<string>();

            // --- LUFS ---
            if (ApplyLufsNormalization)
            {
                var analysisResult = RunFfmpeg($"-i \"{inputFile}\" -af ebur128 -f null -");

                if (analysisResult.Success && !string.IsNullOrEmpty(analysisResult.Output))
                {
                    if (ParseAndLogEbur128(analysisResult.Output, out double measuredLufs))
                    {
                        double targetLufs = -8.0;
                        double requiredGainDb = targetLufs - measuredLufs;
                        audioFilters.Add($"volume={requiredGainDb.ToString("F2", CultureInfo.InvariantCulture)}dB");
                        Log($"\tLUFS: \t\t{targetLufs}", Brushes.LightGreen);
                    }
                }
            }

            // --- OFFSET ---
            if (manualOffsetSeconds > 0)
            {
                int offsetMs = (int)(manualOffsetSeconds * 1000);
                audioFilters.Insert(0, $"adelay={offsetMs}|{offsetMs}");
            }

            if (CalculateMadiValues)
            {
                double songLengthSeconds = GetAudioDuration(inputFile);
                if (songLengthSeconds <= 0)
                {
                    Log("Error: Could not read duration.", Brushes.Red);
                    return;
                }

                decimal bpmDec = (decimal)bpm;
                decimal timeDec = (decimal)time;
                timeDec += (decimal)manualOffsetSeconds;

                decimal startmadiCalc = (bpmDec * (1m / 60m) * timeDec / 4m) - 1m;
                decimal totalmadiCalc = (bpmDec * (decimal)songLengthSeconds) / (4m * 60m);

                decimal startmadi = Math.Ceiling(startmadiCalc);
                decimal totalmadi = Math.Ceiling(totalmadiCalc);

                Brush headerColor = Brushes.LightCoral;
                Brush lightGreen = Brushes.LightGreen;

                Log($"\tBPM: \t\t{FormatBpmToString(bpm)}", lightGreen);
                Log("Time Signature", headerColor);
                Log($"\tStartMadi: \t{startmadi}", lightGreen);
                Log($"\tTotalMadi: \t{totalmadi}\n", lightGreen);

                string bpmStr = FormatBpmToString(bpm);
                string startmadiStr = startmadi.ToString(CultureInfo.InvariantCulture);
                string totalmadiStr = totalmadi.ToString(CultureInfo.InvariantCulture);

                newFileName = $"S_{originalName} {bpmStr} {startmadiStr} {totalmadiStr}.ogg";
            }
            else
            {
                newFileName = $"edited_{originalName}.ogg";
            }

            string outputFile = Path.Combine(outputPath, newFileName);

            // --- FFMPEG ARGUMENTE ---

            string seekArg = "";
            if (manualOffsetSeconds < 0)
            {
                seekArg = $"-ss {Math.Abs(manualOffsetSeconds).ToString("F3", CultureInfo.InvariantCulture)}";
            }

            string filterArg = "";
            if (audioFilters.Count > 0)
            {
                filterArg = $"-af \"{string.Join(",", audioFilters)}\"";
            }

            // WICHTIG: Hier sind die gewünschten Settings:
            // -b:a 96k  -> 96 kbps Bitrate
            // -ar 44100 -> 44100 Hz Sample Rate
            string arguments = $"{seekArg} -i \"{inputFile}\" {filterArg} -b:a 96k -ar 44100 -map_metadata -1 -y \"{outputFile}\"";

            var finalResult = RunFfmpeg(arguments);
            HandleConversionResult(finalResult, outputFile);
        }

        private void HandleConversionResult(FfmpegResult result, string outputFile)
        {
            if (result.Success)
            {
                Log("\nConversion successful.", Brushes.LimeGreen);
                Application.Current.Dispatcher.Invoke(() => StatusMessage = "Done.");
            }
            else
            {
                Log("\nError: Conversion failed.", Brushes.Red);
                Log(result.Output, Brushes.Gray);
                Application.Current.Dispatcher.Invoke(() => StatusMessage = "Error.");
            }
        }

        private FfmpegResult RunFfmpeg(string arguments)
        {
            var result = new FfmpegResult();
            try
            {
                var processInfo = new ProcessStartInfo { FileName = "ffmpeg.exe", Arguments = arguments, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                using (var process = Process.Start(processInfo))
                {
                    result.Output = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    result.Success = process.ExitCode == 0;
                    return result;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Output = ex.Message;
                return result;
            }
        }

        private bool ParseAndLogEbur128(string analysisOutput, out double measuredLufs)
        {
            measuredLufs = 0;
            Brush headerColor = Brushes.LightCoral;
            Brush lightGreen = Brushes.LightGreen;

            Log("EBU R128 Measurement", headerColor);

            string GetValue(string pattern)
            {
                var m = Regex.Match(analysisOutput, pattern);
                return m.Success ? m.Groups[1].Value : "N/A";
            }

            string iString = GetValue(@"Integrated loudness:\s+I:\s+(-?[\d\.]+)");
            Log($"\tI: \t\t{iString}", lightGreen);

            if (double.TryParse(iString, NumberStyles.Any, CultureInfo.InvariantCulture, out measuredLufs))
            {
                return true;
            }
            return false;
        }

        private void ShowAbout(object obj)
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }

        private void OpenGitHub(object obj)
        {
            try { Process.Start(new ProcessStartInfo("https://github.com/0x53616E") { UseShellExecute = true }); }
            catch { }
        }

        private bool PromptForValue(string question, string defaultValue, out double result)
        {
            var inputDialog = new InputDialog(question, defaultValue);
            if (inputDialog.ShowDialog() == true && double.TryParse(inputDialog.Answer, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return true;
            }
            result = 0;
            return false;
        }

        private double GetAudioDuration(string inputFile)
        {
            string arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{inputFile}\"";
            try
            {
                var pInfo = new ProcessStartInfo { FileName = "ffprobe.exe", Arguments = arguments, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
                using (var process = Process.Start(pInfo))
                {
                    string durationStr = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    if (double.TryParse(durationStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double duration))
                        return duration;
                }
            }
            catch { }
            return -1;
        }

        private double GetBpmFromAudio(string inputFile)
        {
            string arguments = $"-i \"{inputFile}\" -af bpm -f null -";
            List<double> detectedBpms = new List<double>();
            try
            {
                var pInfo = new ProcessStartInfo { FileName = "ffmpeg.exe", Arguments = arguments, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                using (var process = Process.Start(pInfo))
                {
                    string output = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    var matches = Regex.Matches(output, @"BPM:[\s]*([\d\.]+)");
                    foreach (Match m in matches)
                    {
                        if (double.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                        {
                            if (val > 40 && val < 300) detectedBpms.Add(val);
                        }
                    }
                }
                if (detectedBpms.Count > 0)
                {
                    detectedBpms.Sort();
                    int count = detectedBpms.Count;
                    double median = 0;
                    if (count % 2 == 0) median = (detectedBpms[count / 2 - 1] + detectedBpms[count / 2]) / 2.0;
                    else median = detectedBpms[count / 2];
                    return median;
                }
            }
            catch { }
            return 0;
        }

        private string FormatBpmToString(double bpm)
        {
            if (Math.Abs(bpm % 1) <= (Double.Epsilon * 100))
                return bpm.ToString("0", CultureInfo.InvariantCulture);
            return bpm.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}