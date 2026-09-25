using grzyClothTool.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window
    {
        public ObservableCollection<LogMessage> LogMessages { get; set; } = [];

        public LogWindow()
        {
            InitializeComponent();
            LocalizationHelper.ApplyTo(this);
            Closing += LogWindow_Closing;
            DataContext = this;
        }

        public void LogWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }

    public class LogMessage : INotifyPropertyChanged
    {
        private string _message;

        public LogMessage()
        {
            LocalizationHelper.LanguageChanged += (_, _) =>
                Message = LocalizationHelper.Translate(OriginalMessage);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Timestamp { get; set; }
        public string OriginalMessage { get; set; } = string.Empty;
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message)));
            }
        }

        public string TypeIcon { get; set; }
    }

    public enum LogType
    {
        Info,
        Warning,
        Error
    }
}
