using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using grzyClothTool.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using static grzyClothTool.Controls.CustomMessageBox;
using static grzyClothTool.Enums;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for BuildWindow.xaml
    /// </summary>
    public partial class BuildWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string ProjectName { get; set; } = MainWindow.AddonManager.ProjectName;

        private bool _isBuilding;
        public bool IsBuilding
        {
            get => _isBuilding;
            set
            {
                if (_isBuilding != value)
                {
                    _isBuilding = value;
                    OnPropertyChanged(nameof(IsBuilding));
                }
            }
        }
        private int _progressValue;
        public int ProgressValue
        {
            get => _progressValue;
            set
            {
                if (_progressValue != value)
                {
                    _progressValue = value;
                    OnPropertyChanged(nameof(ProgressValue));
                }
            }
        }

        private bool _splitAddons;
        public bool SplitAddons
        {
            get => _splitAddons;
            set
            {
                if (_splitAddons != value)
                {
                    _splitAddons = value;
                    OnPropertyChanged(nameof(SplitAddons));
                }
            }
        }

        private bool _isWarningVisible;
        public bool IsWarningVisible
        {
            get => _isWarningVisible;
            set
            {
                if (_isWarningVisible != value)
                {
                    _isWarningVisible = value;
                    OnPropertyChanged(nameof(IsWarningVisible));
                }
            }
        }

        private string _warningMessage;
        public string WarningMessage
        {
            get => _warningMessage;
            set
            {
                if (_warningMessage != value)
                {
                    _warningMessage = value;
                    OnPropertyChanged(nameof(WarningMessage));
                }
            }
        }

        private bool _canBuild = true;
        public bool CanBuild
        {
            get => _canBuild;
            set
            {
                if (_canBuild != value)
                {
                    _canBuild = value;
                    OnPropertyChanged(nameof(CanBuild));
                }
            }
        }

        public string BuildPath { get; set; } = GetDefaultBuildPath();

        private static string GetDefaultBuildPath()
        {
            var projectName = MainWindow.AddonManager.ProjectName;
            var mainFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;

            if (string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(mainFolder))
            {
                return string.Empty;
            }

            return Path.Combine(mainFolder, projectName, "build_output");
        }

        private BuildResourceType _resourceType;

        public BuildWindow()
        {
            InitializeComponent();

            var workArea = SystemParameters.WorkArea;
            MaxWidth = Math.Max(MinWidth, Math.Min(700, workArea.Width - 32));
            MaxHeight = Math.Max(MinHeight, Math.Min(800, workArea.Height - 32));
            Width = Math.Min(Width, MaxWidth);
            Height = Math.Min(Height, MaxHeight);

            LocalizationHelper.ApplyTo(this);
            DataContext = this;

            this.Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            split_addons.IsEnabled = MainWindow.AddonManager.Addons.Count > 1;

            CheckAddons();
        }

        private void CheckAddons()
        {
            IsWarningVisible = false;
            WarningMessage = string.Empty;
            CanBuild = true;

            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                IsWarningVisible = true;
                WarningMessage = LocalizationHelper.Translate("Проект не загружен. Сначала создайте или откройте проект.");
                CanBuild = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(BuildPath))
            {
                IsWarningVisible = true;
                WarningMessage = LocalizationHelper.Translate("Не удалось определить путь сборки. Проверьте настройки проекта.");
                CanBuild = false;
                return;
            }

            var allDrawablesCount = MainWindow.AddonManager.Addons.Sum(a => a.Drawables.Count);
            if (allDrawablesCount == 0)
            {
                IsWarningVisible = true;
                WarningMessage = LocalizationHelper.Translate("Элементы одежды не найдены. Добавьте одежду, чтобы собрать ресурс.");
                CanBuild = false;
            }
        }

        private void ProjectInput_Updated(object sender, UpdatedEventArgs e)
        {
            CheckAddons();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = IsBuilding;
        }

        private async Task BuildResource(BuildResourceHelper buildHelper)
        {
            switch (_resourceType)
            {
                case BuildResourceType.FiveM:
                    await buildHelper.BuildFiveMResource();
                    break;
                case BuildResourceType.AltV:
                    await buildHelper.BuildAltVResource();
                    break;
                case BuildResourceType.Singleplayer:
                    await buildHelper.BuildSingleplayerResource();
                    break;
                default:
                    throw new NotImplementedException($"Неподдерживаемый тип ресурса: {_resourceType}");
            }
        }

        private async void build_MyBtnClickEvent(object sender, RoutedEventArgs e)
        {
            var error = ValidateProjectName();
            if (error != null)
            {
                CustomMessageBox.Show(
                    LocalizationHelper.Translate(error),
                    LocalizationHelper.Translate("Ошибка"),
                    CustomMessageBoxButtons.OKOnly,
                    CustomMessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(ProjectName) || string.IsNullOrEmpty(BuildPath))
            {
                CustomMessageBox.Show("Заполните все поля и убедитесь, что проект загружен.", "Ошибка", CustomMessageBoxButtons.OKOnly);
                return;
            }

            var buildButton = sender as CustomButton;
            if (buildButton != null)
            {
                buildButton.IsEnabled = false; // blocking interactions - spamming button led to building multiple times/exception
            }

            int totalSteps = MainWindow.AddonManager.GetTotalDrawableAndTextureCount();

            ProgressValue = 0;
            pbBuild.Maximum = totalSteps;
            IsBuilding = true;

            try
            {
                await SaveHelper.SaveAsync();
                var timer = new Stopwatch();

                timer.Start();

                var progress = new Progress<int>(value => ProgressValue += value);
                var buildHelper = new BuildResourceHelper(ProjectName, BuildPath, progress, _resourceType, SplitAddons);

                await Task.Run(() => BuildResource(buildHelper)); // moved out of ui thread, so users don't think tool stopped responding

                timer.Stop();
                CustomMessageBox.Show($"Сборка завершена, затраченное время: {timer.Elapsed}", "Сборка завершена", CustomMessageBoxButtons.OpenFolder, BuildPath);
                LogHelper.Log($"Сборка завершена, затраченное время: {timer.Elapsed}");
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Не удалось собрать ресурс: {ex}", LogType.Error);
                ErrorLogHelper.LogError("Не удалось собрать ресурс", ex);
                CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось собрать ресурс: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка"),
                    CustomMessageBoxButtons.OKOnly,
                    CustomMessageBoxIcon.Error);
            }
            finally
            {
                ProgressValue = totalSteps; // make sure that progress bar is full

                if (buildButton != null)
                {
                    buildButton.IsEnabled = true;
                }

                IsBuilding = false;
                Close();
            }
        }

        private void RadioButton_ChangedEvent(object sender, RoutedEventArgs e)
        {
            if (sender is ModernLabelRadioButton radioButton && radioButton.IsChecked == true)
            {
                _resourceType = radioButton.Tag?.ToString() switch
                {
                    "FiveM" => BuildResourceType.FiveM,
                    "AltV" => BuildResourceType.AltV,
                    "Singleplayer" => BuildResourceType.Singleplayer,
                    _ => throw new NotImplementedException()
                };


                // Singleplayer doesn't support splitting addons
                if (_resourceType == BuildResourceType.Singleplayer)
                {
                    SplitAddons = false;
                    split_addons.IsEnabled = false;
                }
                else if (DataContext != null) // check if DataContext exist, to prevent error (happens on initialization)
                {
                    split_addons.IsEnabled = MainWindow.AddonManager.Addons.Count > 1;
                }
            }
        }

        private string ValidateProjectName()
        {
            string? error = ProjectNameValidator.Validate(ProjectName);
            return error == null ? null : LocalizationHelper.Translate(error);
        }
    }
}
