using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using grzyClothTool.Helpers;

namespace grzyClothTool.Views
{
    public partial class ProjectSetupDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _dialogTitle = LocalizationHelper.Translate("Создание нового проекта");
        public string DialogTitle
        {
            get => _dialogTitle;
            set { _dialogTitle = value; OnPropertyChanged(); }
        }


        private string _projectName = string.Empty;
        public string ProjectName
        {
            get => _projectName;
            set 
            { 
                _projectName = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsValid));
                OnPropertyChanged(nameof(ProjectExistsWarning));
                OnPropertyChanged(nameof(ProjectNameValidationMessage));
                OnPropertyChanged(nameof(ShowProjectExistsWarning));
            }
        }
        public string ProjectExistsWarning =>
            LocalizationHelper.Format("Проект с названием \"{0}\" уже существует. Выберите другое название — существующие данные не будут удалены.", ProjectName);

        public string ProjectNameValidationMessage
        {
            get
            {
                string? error = ProjectNameValidator.Validate(ProjectName);
                return error == null ? string.Empty : LocalizationHelper.Translate(error);
            }
        }

        private bool _isSelfContained = true;
        public bool IsSelfContained
        {
            get => _isSelfContained;
            set 
            { 
                _isSelfContained = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsExternal));
            }
        }

        public bool IsExternal
        {
            get => !_isSelfContained;
            set 
            { 
                _isSelfContained = !value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSelfContained));
            }
        }

        private bool _showDrawableCount;
        public bool ShowDrawableCount
        {
            get => _showDrawableCount;
            set { _showDrawableCount = value; OnPropertyChanged(); }
        }

        private string _drawableCountMessage = string.Empty;
        public string DrawableCountMessage
        {
            get => _drawableCountMessage;
            set { _drawableCountMessage = value; OnPropertyChanged(); }
        }

        private string _confirmButtonText = LocalizationHelper.Translate("Создать");
        public string ConfirmButtonText
        {
            get => _confirmButtonText;
            set { _confirmButtonText = value; OnPropertyChanged(); }
        }

        public bool ShowProjectExistsWarning
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ProjectName) || !ProjectNameValidator.IsValid(ProjectName))
                    return false;

                var mainFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
                if (string.IsNullOrEmpty(mainFolder))
                    return false;

                return SaveHelper.ProjectExists(mainFolder, ProjectName.Trim(), out _);
            }
        }

        public bool IsValid => ProjectNameValidator.IsValid(ProjectName) && !ShowProjectExistsWarning;

        public bool Confirmed { get; private set; }

        public ProjectSetupDialog()
        {
            InitializeComponent();
            LocalizationHelper.ApplyTo(this);
            LocalizationHelper.LanguageChanged += OnLanguageChanged;
            Closed += (_, _) => LocalizationHelper.LanguageChanged -= OnLanguageChanged;
            DataContext = this;
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            RefreshLocalizedText();
        }

        private void RefreshLocalizedText()
        {
            OnPropertyChanged(nameof(DialogTitle));
            OnPropertyChanged(nameof(ProjectExistsWarning));
            OnPropertyChanged(nameof(ProjectNameValidationMessage));
            OnPropertyChanged(nameof(ConfirmButtonText));
            OnPropertyChanged(nameof(DrawableCountMessage));
            LocalizationHelper.ApplyTo(this);
        }

        public static ProjectSetupDialog ShowForNewProject(Window owner)
        {
            var dialog = new ProjectSetupDialog
            {
                Owner = owner,
                DialogTitle = LocalizationHelper.Translate("Создание нового проекта"),
                ConfirmButtonText = LocalizationHelper.Translate("Создать"),
                IsSelfContained = true,
                ShowDrawableCount = false
            };
            
            dialog.ProjectNameTextBox.Focus();
            dialog.ShowDialog();
            return dialog;
        }

        public static ProjectSetupDialog ShowForOpenAddon(Window owner, string suggestedName, int drawableCount, int metaFileCount)
        {
            var dialog = new ProjectSetupDialog
            {
                Owner = owner,
                DialogTitle = LocalizationHelper.Translate("Открытие существующего аддона"),
                ConfirmButtonText = LocalizationHelper.Translate("Открыть"),
                ProjectName = suggestedName,
                IsSelfContained = false,
                ShowDrawableCount = true,
                DrawableCountMessage = metaFileCount > 1 
                    ? LocalizationHelper.Format("Обнаружено элементов одежды: {0}. Файлов .meta: {1}", drawableCount, metaFileCount)
                    : LocalizationHelper.Format("Обнаружено элементов одежды: {0}", drawableCount)
            };
            
            dialog.ProjectNameTextBox.SelectAll();
            dialog.ProjectNameTextBox.Focus();
            dialog.ShowDialog();
            return dialog;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValid)
            {
                return;
            }

            if (ShowProjectExistsWarning)
            {
                Controls.CustomMessageBox.Show(
                    LocalizationHelper.Format("Проект с названием \"{0}\" уже существует. Выберите другое название — существующие данные не будут удалены.", ProjectName),
                    "Проект уже существует",
                    Controls.CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    Controls.CustomMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            Confirmed = true;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Confirmed = false;
            DialogResult = false;
            Close();
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
