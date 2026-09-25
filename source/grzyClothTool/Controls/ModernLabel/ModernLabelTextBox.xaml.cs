using grzyClothTool.Helpers;
using System;
using System.Windows;
using System.Windows.Input;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for ModernLabelTextBox.xaml
    /// </summary>
    public partial class ModernLabelTextBox : ModernLabelBaseControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty
        .Register("Label",
                typeof(string),
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata("Введите значение"));

        public static readonly DependencyProperty TextProperty = DependencyProperty
            .Register("Text",
                    typeof(string),
                    typeof(ModernLabelTextBox),
                    new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnUpdate));

        public static readonly DependencyProperty IsFolderSelectionProperty = DependencyProperty
            .Register("IsFolderSelection",
                typeof(bool),
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata(false, OnSelectionModeChanged));

        public static readonly DependencyProperty IsFileSelectionProperty = DependencyProperty
            .Register("IsFileSelection",
                typeof(bool),
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata(false, OnSelectionModeChanged));

        public static readonly DependencyProperty FileExtensionProperty = DependencyProperty
            .Register("FileExtension",
                typeof(string),
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata(""));

        public static readonly DependencyProperty OriginalSelectedPathProperty = DependencyProperty
            .Register("OriginalSelectedPath",
                typeof(string),
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata(""));

        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public bool IsFolderSelection
        {
            get { return (bool)GetValue(IsFolderSelectionProperty); }
            set { SetValue(IsFolderSelectionProperty, value); }
        }

        public bool IsFileSelection
        {
            get { return (bool)GetValue(IsFileSelectionProperty); }
            set { SetValue(IsFileSelectionProperty, value); }
        }

        public string FileExtension
        {
            get { return (string)GetValue(FileExtensionProperty); }
            set { SetValue(FileExtensionProperty, value); }
        }

        public string OriginalSelectedPath
        {
            get { return (string)GetValue(OriginalSelectedPathProperty); }
            set { SetValue(OriginalSelectedPathProperty, value); }
        }

        public bool IsFolderOrFileSelection {
            get {
                return IsFolderSelection || IsFileSelection;
            } 
        }

        private static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ModernLabelTextBox control && control.MyText != null)
            {
                control.MyText.IsReadOnly = control.IsFolderOrFileSelection;
            }
        }

        static ModernLabelTextBox()
        {
            FontSizeProperty.OverrideMetadata(
                typeof(ModernLabelTextBox),
                new FrameworkPropertyMetadata(14.0));
        }

        public ModernLabelTextBox()
        {
            InitializeComponent();
            MyText.GotFocus += (s, e) => IsUserInitiated = true;
            MyText.LostFocus += (s, e) => IsUserInitiated = false;
        }

        private void MyText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsFolderOrFileSelection)
            {
                return;
            }

            IsUserInitiated = true;
            try
            {
                if (IsFolderSelection)
                {
                    using var dialog = new System.Windows.Forms.FolderBrowserDialog
                    {
                        Description = LocalizationHelper.Translate("Выберите папку")
                    };

                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK &&
                        !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                    {
                        OriginalSelectedPath = dialog.SelectedPath;
                        Text = dialog.SelectedPath;
                    }
                }
                else if (IsFileSelection)
                {
                    var dialog = new Microsoft.Win32.OpenFileDialog
                    {
                        DefaultExt = FileExtension,
                        Filter = $"{FileExtension} files (*{FileExtension})|*{FileExtension}",
                        Title = LocalizationHelper.Translate("Выберите файл")
                    };

                    if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FileName))
                    {
                        OriginalSelectedPath = dialog.FileName;
                        Text = dialog.FileName;
                    }
                }
            }
            finally
            {
                IsUserInitiated = false;
            }
        }
    }
}
