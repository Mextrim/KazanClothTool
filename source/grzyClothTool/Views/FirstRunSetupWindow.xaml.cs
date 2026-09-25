using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for FirstRunSetupWindow.xaml
    /// </summary>
    public partial class FirstRunSetupWindow : Window
    {
        public bool SetupCompleted { get; private set; }

        public FirstRunSetupWindow()
        {
            InitializeComponent();
            LocalizationHelper.ApplyTo(this);

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string defaultFolder = Path.Combine(documentsPath, "KazanClothTool Projects");
            FolderPathTextBox.Text = defaultFolder;
            ContinueButton.IsEnabled = true;

            Closing += FirstRunSetupWindow_Closing;
        }

        private void FirstRunSetupWindow_Closing(object sender, CancelEventArgs e)
        {
            if (!SetupCompleted)
            {
                var result = CustomMessageBox.Show(
                    LocalizationHelper.Translate("Чтобы продолжить работу с приложением, необходимо выбрать главную папку.\n\nЗакрыть приложение?"),
                    LocalizationHelper.Translate("Требуется настройка"),
                    CustomMessageBox.CustomMessageBoxButtons.YesNo,
                    CustomMessageBox.CustomMessageBoxIcon.Warning);

                if (result == CustomMessageBox.CustomMessageBoxResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    // User wants to exit the application entirely
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = LocalizationHelper.Translate("Выберите ГЛАВНУЮ папку, в которой будут храниться ВСЕ проекты (не папку конкретного проекта)");
            dialog.ShowNewFolderButton = true;

            if (!string.IsNullOrWhiteSpace(FolderPathTextBox.Text) && Directory.Exists(FolderPathTextBox.Text))
            {
                dialog.SelectedPath = FolderPathTextBox.Text;
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FolderPathTextBox.Text = dialog.SelectedPath;
                ValidationMessage.Visibility = Visibility.Collapsed;
                ContinueButton.IsEnabled = true;
            }
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedPath = FolderPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                ValidationMessage.Text = LocalizationHelper.Translate("Перед продолжением выберите главную папку.");
                ValidationMessage.Visibility = Visibility.Visible;
                return;
            }

            if (PersistentSettingsHelper.IsRootDrive(selectedPath))
            {
                ValidationMessage.Text = LocalizationHelper.Translate("Корневой диск (например, C:\\) нельзя использовать как главную папку. Выберите или создайте вложенную папку.");
                ValidationMessage.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                if (!Directory.Exists(selectedPath))
                {
                    Directory.CreateDirectory(selectedPath);
                }

                string testFile = Path.Combine(selectedPath, ".KazanClothTool_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                PersistentSettingsHelper.Instance.MainProjectsFolder = selectedPath;
                PersistentSettingsHelper.Instance.IsFirstRun = false;

                SetupCompleted = true;
                DialogResult = true;
                Close();
            }
            catch (UnauthorizedAccessException)
            {
                ValidationMessage.Text = LocalizationHelper.Translate("Доступ запрещён. Выберите папку, в которую у вас есть права записи.");
                ValidationMessage.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Не удалось настроить папку проектов", ex);
                ValidationMessage.Text = LocalizationHelper.Format("Ошибка: {0}", ErrorMessageHelper.Friendly(ex));
                ValidationMessage.Visibility = Visibility.Visible;
            }
        }
    }
}
