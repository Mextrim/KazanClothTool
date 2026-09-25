using grzyClothTool.Helpers;
using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for CustomMessageBox.xaml
    /// </summary>
    public partial class CustomMessageBox : Window
    {
        private CustomMessageBoxResult _result = CustomMessageBoxResult.Cancel;
        public string TextBoxValue => CMBTextBox.Text;

        // Buttons defined as properties, because couldn't be created (initialized) with event subscription at same time "on-the-fly".
        // You can add new different buttons by adding new one as property here
        // and to CustomMessageBoxButtons and CustomMessageBoxResult enums   
        private Button OK
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = LocalizationHelper.Translate("ОК");
                b.IsDefault = true;
                b.Click += delegate { _result = CustomMessageBoxResult.OK; Close(); };
                return b;
            }
        }
        private Button Cancel
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = LocalizationHelper.Translate("Отмена");
                b.IsCancel = true;
                b.Click += delegate { _result = CustomMessageBoxResult.Cancel; Close(); };
                return b;
            }
        }
        private Button Yes
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = LocalizationHelper.Translate("Да");
                b.IsDefault = true;
                b.Click += delegate { _result = CustomMessageBoxResult.Yes; Close(); };
                return b;
            }
        }
        private Button No
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = LocalizationHelper.Translate("Нет");
                b.IsCancel = true;
                b.Click += delegate { _result = CustomMessageBoxResult.No; Close(); };
                return b;
            }
        }
        private Button OpenFolder
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = LocalizationHelper.Translate("Открыть папку");
                b.Click += delegate { _result = CustomMessageBoxResult.OpenFolder; Close(); };
                return b;
            }
        }

        private Button Delete
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = LocalizationHelper.Translate("Удалить");
                b.Click += delegate { _result = CustomMessageBoxResult.Delete; Close(); };
                return b;
            }
        }

        private Button Replace
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = LocalizationHelper.Translate("Заменить");
                b.Click += delegate { _result = CustomMessageBoxResult.Replace; Close(); };
                return b;
            }
        }

        private Button Male
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = LocalizationHelper.Translate("Мужской");
                b.Click += delegate { _result = CustomMessageBoxResult.Male; Close(); };
                return b;
            }
        }

        private Button Female
        {
            get
            {
                var b = GetDefaultButton();
                b.Content = LocalizationHelper.Translate("Женский");
                b.Click += delegate { _result = CustomMessageBoxResult.Female; Close(); };
                return b;
            }
        }
        // Add another if you wish


        // There is no empty constructor. As least "message" should be passed to this CustomMessageBox
        // Also constructor is private to prevent create its instances somewhere and force to use only static Show methods
        private CustomMessageBox(string message,
                                 string caption = "",
                                 CustomMessageBoxButtons cmbButtons = CustomMessageBoxButtons.OKOnly,
                                 CustomMessageBoxIcon cmbIcon = CustomMessageBoxIcon.None,
                                 string path = "",
                                 bool showTextBox = false)
        {
            InitializeComponent();

            var workArea = SystemParameters.WorkArea;
            MaxWidth = Math.Max(MinWidth, Math.Min(820, workArea.Width - 48));
            MaxHeight = Math.Max(MinHeight, Math.Min(760, workArea.Height - 48));
            Width = Math.Min(Width, MaxWidth);

            LocalizationHelper.ApplyTo(this);
            Window? activeOwner = Application.Current?.Windows
                .OfType<Window>()
                .FirstOrDefault(window => window.IsActive && window != this);
            Window? mainOwner = Application.Current?.MainWindow;
            if (activeOwner != null)
            {
                Owner = activeOwner;
            }
            else if (mainOwner != this)
            {
                Owner = mainOwner;
            }

            // Handle Ctrl+C press to copy message from CustomMessageBox
            KeyDown += (sender, args) =>
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.C))
                    Clipboard.SetText(CMBMessage.Text);
            };

            // Set message. Keep raw file-system/CLR text out of the dialog;
            // the complete exception is still available in the diagnostic log.
            CMBMessage.Text = ErrorMessageHelper.Clean(LocalizationHelper.Translate(message ?? string.Empty));
            // Set caption
            string localizedCaption = string.IsNullOrWhiteSpace(caption)
                ? LocalizationHelper.Translate("Диалог сообщения")
                : LocalizationHelper.Translate(caption);
            CMBCaption.Text = localizedCaption;
            Title = localizedCaption;
            CMBTextBox.Text = "";
            CMBTextBox.Visibility = showTextBox ? Visibility.Visible : Visibility.Collapsed;

            // Setup Buttons (depending on specified CustomMessageBoxButtons value)
            // As StackPanel FlowDirection set as RightToLeft - we should add items in reverse
            switch (cmbButtons)
            {
                case CustomMessageBoxButtons.OKOnly:
                    _ = CMBButtons.Children.Add(OK);
                    break;
                case CustomMessageBoxButtons.OKCancel:
                    _ = CMBButtons.Children.Add(Cancel);
                    _ = CMBButtons.Children.Add(OK);
                    break;
                case CustomMessageBoxButtons.YesNo:
                    _ = CMBButtons.Children.Add(No);
                    _ = CMBButtons.Children.Add(Yes);
                    break;
                case CustomMessageBoxButtons.YesNoCancel:
                    _ = CMBButtons.Children.Add(Cancel);
                    _ = CMBButtons.Children.Add(No);
                    _ = CMBButtons.Children.Add(Yes);
                    break;
                case CustomMessageBoxButtons.OpenFolder:
                    _ = CMBButtons.Children.Add(OK);
                    _ = CMBButtons.Children.Add(OpenFolder);
                    break;
                case CustomMessageBoxButtons.DeleteReplaceCancel:
                    _ = CMBButtons.Children.Add(Cancel);
                    _ = CMBButtons.Children.Add(Replace);
                    _ = CMBButtons.Children.Add(Delete);
                    break;
                case CustomMessageBoxButtons.MaleFemaleCancel:
                    _ = CMBButtons.Children.Add(Cancel);
                    _ = CMBButtons.Children.Add(Female);
                    _ = CMBButtons.Children.Add(Male);
                    break;
                // Add another if you wish                 
                default:
                    _ = CMBButtons.Children.Add(OK);
                    break;
            }

            // Set icon (depending on specified CustomMessageBoxIcon value)
            // From C# 8.0 could be converted to switch-expression
            switch (cmbIcon)
            {
                case CustomMessageBoxIcon.Information:
                    CMBIcon.Source = FromSystemIcon(SystemIcons.Information);
                    break;
                case CustomMessageBoxIcon.Warning:
                    CMBIcon.Source = FromSystemIcon(SystemIcons.Warning);
                    break;
                case CustomMessageBoxIcon.Question:
                    CMBIcon.Source = FromSystemIcon(SystemIcons.Question);
                    break;
                case CustomMessageBoxIcon.Error:
                    CMBIcon.Source = FromSystemIcon(SystemIcons.Error);
                    break;
                case CustomMessageBoxIcon.None:
                default:
                    CMBIcon.Source = null;
                    break;
            }
        }

        // Show methods create new instance of CustomMessageBox window and shows it as Dialog (blocking thread)

        // Shows CustomMessageBox with specified message and default "OK" button
        public static CustomMessageBoxResult Show(string message)
        {
            var dialog = new CustomMessageBox(message);
            dialog.ShowDialog();
            return dialog._result;
        }

        // Shows CustomMessageBox with specified message, caption and default "OK" button
        public static CustomMessageBoxResult Show(string message, string caption)
        {
            var dialog = new CustomMessageBox(message, caption);
            dialog.ShowDialog();
            return dialog._result;
        }

        // Shows CustomMessageBox with specified message, caption and button(s)
        public static CustomMessageBoxResult Show(string message, string caption, CustomMessageBoxButtons cmbButtons)
        {
            var dialog = new CustomMessageBox(message, caption, cmbButtons);
            dialog.ShowDialog();
            return dialog._result;
        }

        // Shows CustomMessageBox with specified message, caption and button(s) and path
        public static CustomMessageBoxResult Show(string message, string caption, CustomMessageBoxButtons cmbButtons, string path)
        {
            var dialog = new CustomMessageBox(message, caption, cmbButtons, path: path);
            dialog.ShowDialog();

            if (dialog._result == CustomMessageBoxResult.OpenFolder)
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
            return dialog._result;
        }

        // Shows CustomMessageBox with specified message, caption, button(s) and icon.
        public static CustomMessageBoxResult Show(string message, string caption, CustomMessageBoxButtons cmbButtons, CustomMessageBoxIcon cmbIcon)
        {
            var dialog = new CustomMessageBox(message, caption, cmbButtons, cmbIcon);
            dialog.ShowDialog();
            return dialog._result;
        }

        public static (CustomMessageBoxResult result, string textBoxValue) Show(string message, string caption, CustomMessageBoxButtons cmbButtons, CustomMessageBoxIcon cmbIcon, bool showTextBox)
        {
            var dialog = new CustomMessageBox(message, caption, cmbButtons, cmbIcon, showTextBox: showTextBox);
            dialog.ShowDialog();

            // If the TextBox is visible, return its value along with the button result
            return showTextBox
                ? (dialog._result, dialog.TextBoxValue)
                : (dialog._result, null);
        }


        // Defines button(s), which should be displayed
        public enum CustomMessageBoxButtons
        {
            OKOnly,
            OKCancel,
            YesNo,
            YesNoCancel,
            OpenFolder,
            DeleteReplaceCancel,
            MaleFemaleCancel

            // Add another if you wish
        }

        // Defines icon, which should be displayed
        public enum CustomMessageBoxIcon
        {
            None,
            Question,
            Information,
            Warning,
            Error
        }

        // Defines button, pressed by user as result
        public enum CustomMessageBoxResult
        {
            OK,
            Cancel,
            Yes,
            No,
            OpenFolder,
            Delete,
            Replace,
            TextBoxValue,
            Male,
            Female


            // Add another if you wish
        }


        // Returns simple Button with pre-defined properties
        private static Button GetDefaultButton() => new Button
        {
            MinWidth = 86,
            Height = 36,
            Margin = new Thickness(0, 4, 8, 4),
            Padding = new Thickness(16, 0, 16, 0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        // Converts system icons (like in original message box) to BitmapSource to be able to set it to Source property of Image control 
        private static BitmapSource FromSystemIcon(Icon icon) =>
            Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

        // Handler on CustomMessageBox caption-header to allow move window while left button pressed on it
        private void OnCaptionPress(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}

