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

        private string _dialogTitle = "Создание нового проекта";
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
                OnPropertyChanged(nameof(ShowProjectExistsWarning));
            }
        }
        public string ProjectExistsWarning =>
            $"Проект с названием \"{ProjectName}\" уже существует. Если продолжить, он будет перезаписан.";

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

        private string _confirmButtonText = "Создать";
        public string ConfirmButtonText
        {
            get => _confirmButtonText;
            set { _confirmButtonText = value; OnPropertyChanged(); }
        }

        public bool ShowProjectExistsWarning
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ProjectName))
                    return false;

                var mainFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
                if (string.IsNullOrEmpty(mainFolder))
                    return false;

                return SaveHelper.ProjectExists(mainFolder, ProjectName.Trim(), out _);
            }
        }

        public bool IsValid
        {
            get
            {
                string name = ProjectName.Trim();
                return !string.IsNullOrWhiteSpace(name) &&
                       name is not "." and not ".." &&
                       !Path.IsPathRooted(name) &&
                       name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
            }
        }

        public bool Confirmed { get; private set; }

        public ProjectSetupDialog()
        {
            InitializeComponent();
            LocalizationHelper.ApplyTo(this);
            DataContext = this;
        }

        public static ProjectSetupDialog ShowForNewProject(Window owner)
        {
            var dialog = new ProjectSetupDialog
            {
                Owner = owner,
                DialogTitle = "Создание нового проекта",
                ConfirmButtonText = "Создать",
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
                DialogTitle = "Открытие существующего аддона",
                ConfirmButtonText = "Открыть",
                ProjectName = suggestedName,
                IsSelfContained = false,
                ShowDrawableCount = true,
                DrawableCountMessage = metaFileCount > 1 
                    ? $"Обнаружено элементов одежды: {drawableCount}. Файлов .meta: {metaFileCount}"
                    : $"Обнаружено элементов одежды: {drawableCount}"
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
                    $"Проект с названием \"{ProjectName}\" уже существует. Выберите другое название — существующие данные не будут удалены.",
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
