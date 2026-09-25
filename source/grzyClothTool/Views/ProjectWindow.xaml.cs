using CodeWalker;
using CodeWalker.GameFiles;
using grzyClothTool.Controls;
using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using grzyClothTool.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
using UserControl = System.Windows.Controls.UserControl;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Windows.Input;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using System.Threading.Tasks;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDragDropEffects = System.Windows.DragDropEffects;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for Project.xaml
    /// </summary>
    public partial class ProjectWindow : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private Addon _addon;
        public Addon Addon
        {
            get { return _addon; }
            set
            {
                if (_addon != value)
                {
                    _addon = value;
                    OnPropertyChanged();
                }
            }
        }

        public ProjectWindow()
        {
            InitializeComponent();
            LocalizationHelper.ApplyTo(this);

            if(DesignerProperties.GetIsInDesignMode(this))
            {
                Addon = new Addon("design");
                DataContext = this;
                return;
            }

            DataContext = MainWindow.AddonManager;
            
            Loaded += ProjectWindow_Loaded;
            Loaded += (_, _) => LocalizationHelper.ApplyTo(this);
            Unloaded += ProjectWindow_Unloaded;
        }

        private void EmptyProject_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.NavigationHelper.Navigate("Home");
        }

        private void ProjectWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PreviewWindowHost.Preview3DAvailabilityChanged -= OnPreview3DAvailabilityChanged;
            PreviewWindowHost.Preview3DAvailabilityChanged += OnPreview3DAvailabilityChanged;
            UpdatePreviewButtonState();
        }

        private void ProjectWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            PreviewWindowHost.Preview3DAvailabilityChanged -= OnPreview3DAvailabilityChanged;
        }

        private void OnPreview3DAvailabilityChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => UpdatePreviewButtonState());
        }

        private void UpdatePreviewButtonState()
        {
            if (PreviewButton != null)
            {
                PreviewButton.IsEnabled = SettingsHelper.Preview3DAvailable;
            }
        }

        private async void Add_DrawableFile(object sender, RoutedEventArgs e)
        {
            if (sender is not CustomButton button)
            {
                return;
            }

            if (Addon == null)
            {
                CustomMessageBox.Show(
                    "Сначала откройте или создайте проект, а затем добавьте одежду.",
                    "Проект не открыт",
                    CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    CustomMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            e.Handled = true;
            var sex = string.Equals(button.Tag?.ToString(), "MALE", StringComparison.OrdinalIgnoreCase)
                ? Enums.SexType.male
                : Enums.SexType.female;

            var files = new OpenFileDialog
            {
                Title = LocalizationHelper.Translate(sex == Enums.SexType.male ? "Выберите мужские YDD" : "Выберите женские YDD"),
                Filter = LocalizationHelper.Translate("Drawable YDD (*.ydd)|*.ydd"),
                Multiselect = true
            };

            if (files.ShowDialog() != true)
            {
                return;
            }

            bool success = false;
            ProgressHelper.Start(LocalizationHelper.Translate("Добавление одежды..."));
            try
            {
                await MainWindow.AddonManager.AddDrawables(files.FileNames, sex, targetAddon: Addon);
                SaveHelper.SetUnsavedChanges(true);
                success = true;
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Не удалось добавить одежду", ex);
                CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось добавить одежду: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка"),
                    CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    CustomMessageBox.CustomMessageBoxIcon.Error);
            }
            finally
            {
                ProgressHelper.Stop(success ? LocalizationHelper.Translate("Одежда добавлена") : LocalizationHelper.Translate("Не удалось добавить одежду"), success);
            }
        }

        private async void Add_DrawableFolder(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element)
            {
                return;
            }

            if (Addon == null)
            {
                CustomMessageBox.Show(
                    "Сначала откройте или создайте проект, а затем выберите папку с одеждой.",
                    "Проект не открыт",
                    CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    CustomMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            e.Handled = true;
            var sex = string.Equals(element.Tag?.ToString(), "MALE", StringComparison.OrdinalIgnoreCase)
                ? Enums.SexType.male
                : Enums.SexType.female;

            var folder = new OpenFolderDialog
            {
                Title = LocalizationHelper.Translate(sex == Enums.SexType.male ? "Выберите папку с мужскими YDD" : "Выберите папку с женскими YDD"),
                Multiselect = true
            };

            if (folder.ShowDialog() != true)
            {
                return;
            }

            bool success = false;
            int allFilesCount = 0;
            ProgressHelper.Start(LocalizationHelper.Translate("Поиск одежды..."));
            try
            {
                string[] allFiles = await Task.Run(() => folder.FolderNames
                    .SelectMany(path => Directory.GetFiles(path, "*.ydd", SearchOption.AllDirectories))
                    .OrderBy(file => FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(file)) ?? int.MaxValue)
                    .ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                    .ToArray());

                if (allFiles.Length == 0)
                {
                    throw new InvalidOperationException("В выбранных папках нет файлов YDD.");
                }

                allFilesCount = allFiles.Length;
                await MainWindow.AddonManager.AddDrawables(allFiles, sex, targetAddon: Addon);
                SaveHelper.SetUnsavedChanges(true);
                success = true;
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Не удалось добавить одежду", ex);
                CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось добавить одежду: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка"),
                    CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    CustomMessageBox.CustomMessageBoxIcon.Error);
            }
            finally
            {
                ProgressHelper.Stop(success
                    ? LocalizationHelper.Format("Добавлено файлов: {0}", allFilesCount)
                    : LocalizationHelper.Translate("Не удалось добавить одежду"), success);
            }
        }

        public void SelectedDrawable_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete || Addon?.SelectedDrawables == null || Addon.SelectedDrawables.Count == 0)
            {
                return;
            }

            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.Shift:
                    // Shift+Delete was pressed, delete the drawable instantly
                    MainWindow.AddonManager.DeleteDrawables([.. Addon.SelectedDrawables]);
                    break;
                case ModifierKeys.Control:
                    // Ctrl+Delete was pressed, replace the drawable instantly
                    ReplaceDrawables([.. Addon.SelectedDrawables]);
                    break;
                default:
                    // Only Delete was pressed, show the message box
                    Delete_SelectedDrawable(sender, new RoutedEventArgs());
                    break;
            }
        }

        private void Delete_SelectedDrawable(object sender, RoutedEventArgs e)
        {
            if (Addon?.SelectedDrawables == null || Addon.SelectedDrawables.Count == 0)
            {
                CustomMessageBox.Show("Не выбрано ни одного элемента одежды.", "Удаление одежды", CustomMessageBox.CustomMessageBoxButtons.OKOnly);
                return;
            }

            int count = Addon.SelectedDrawables.Count;
            string message = count == 1
                ? LocalizationHelper.Format("Удалить выбранный элемент одежды «{0}»?", Addon.SelectedDrawable?.Name)
                : LocalizationHelper.Format("Удалить выбранные элементы одежды ({0})?", count);
            message += "\n\n" + LocalizationHelper.Translate("Нумерация последующих элементов изменится.")
                + "\n\n" + LocalizationHelper.Translate("Заменить их зарезервированными слотами вместо удаления?");

            var result = CustomMessageBox.Show(message, "Удаление одежды", CustomMessageBox.CustomMessageBoxButtons.DeleteReplaceCancel);
            if (result == CustomMessageBox.CustomMessageBoxResult.Delete)
            {
                MainWindow.AddonManager.DeleteDrawables([.. Addon.SelectedDrawables]);
            }
            else if (result == CustomMessageBox.CustomMessageBoxResult.Replace)
            {
                ReplaceDrawables([.. Addon.SelectedDrawables]);
            }
        }

        private void ReplaceDrawables(List<GDrawable> drawables)
        {
            if (Addon == null || drawables == null || drawables.Count == 0)
            {
                return;
            }

            foreach (GDrawable drawable in drawables)
            {
                int index = Addon.Drawables.IndexOf(drawable);
                if (index < 0)
                {
                    continue;
                }

                DuplicateDetector.UnregisterDrawable(drawable);
                Addon.SelectedDrawables.Remove(drawable);
                Addon.Drawables[index] = new GDrawableReserved(drawable.Sex, drawable.IsProp, drawable.TypeNumeric, drawable.Number);
            }

            Addon.SelectedDrawable = Addon.Drawables.FirstOrDefault();
            Addon.SelectedTexture = Addon.SelectedDrawable?.Textures.FirstOrDefault();
            SaveHelper.SetUnsavedChanges(true);
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || e.AddedItems[0] is not Addon addon)
            {
                return;
            }

            var selectedAddon = MainWindow.AddonManager.Addons
                .FirstOrDefault(item => ReferenceEquals(item, addon));

            if (selectedAddon == null)
            {
                return;
            }

            Addon = selectedAddon;
            MainWindow.AddonManager.SelectedAddon = selectedAddon;

            foreach (var menuItem in MainWindow.AddonManager.MoveMenuItems)
            {
                menuItem.IsEnabled = !ReferenceEquals(menuItem, selectedAddon)
                    && !string.Equals(menuItem.Header?.ToString(), selectedAddon.Name?.ToString(), StringComparison.Ordinal);
            }
        }

        private void BuildResource_Btn(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MainWindow.AddonManager.ProjectName))
            {
                CustomMessageBox.Show("Проект не открыт. Сначала создайте или откройте проект.",
                    "Нет проекта",
                    CustomMessageBox.CustomMessageBoxButtons.OKOnly, 
                    CustomMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            BuildWindow buildWindow = new()
            {
                Owner = Window.GetWindow(this)
            };
            buildWindow.ShowDialog();
        }

        private void Preview_Btn(object sender, RoutedEventArgs e)
        {
            var mainWindow = MainWindow.Instance;
            if (mainWindow == null || Addon == null) return;

            if (mainWindow.PreviewAnchorable != null)
            {
                mainWindow.PreviewAnchorable.Show();
                mainWindow.PreviewHost?.InitializePreview();

                if (Addon.SelectedDrawable != null && !Addon.SelectedDrawable.IsEncrypted)
                {
                    CWHelper.SendDrawableUpdateToPreview(e);
                }

                MainWindow.AddonManager.IsPreviewEnabled = true;
                
                UpdatePreviewButtonState();
            }
        }

        private void SelectedDrawable_Changed(object sender, EventArgs e)
        {
            if (Addon == null || e is not SelectionChangedEventArgs args) return;
            args.Handled = true;

            foreach (GDrawable drawable in args.RemovedItems)
            {
                Addon.SelectedDrawables.Remove(drawable);
            }

            foreach (GDrawable drawable in args.AddedItems)
            {
                Addon.SelectedDrawables.Add(drawable);
                drawable.IsNew = false;
            }

            if (Addon.SelectedDrawables.Count == 1)
            {
                Addon.SelectedDrawable = Addon.SelectedDrawables.First();
                if (Addon.SelectedDrawable.Textures.Count > 0)
                {
                    Addon.SelectedTexture = Addon.SelectedDrawable.Textures.First();
                    SelDrawable.SelectedTextures = [Addon.SelectedTexture];
                }
            }
            else
            {
                Addon.SelectedDrawable = null;
                Addon.SelectedTexture = null;
            }

            if (!MainWindow.AddonManager.IsPreviewEnabled || (Addon.SelectedDrawable == null && Addon.SelectedDrawables.Count == 0)) return;
            
            var mainWindow = MainWindow.Instance;
            if (mainWindow?.PreviewAnchorable?.IsVisible != true) return;
            
            CWHelper.SendDrawableUpdateToPreview(e);
        }

        private void SelectedDrawable_Updated(object sender, DrawableUpdatedArgs e)
        {
            if (Addon == null ||
                !Addon.TriggerSelectedDrawableUpdatedEvent ||
                !MainWindow.AddonManager.IsPreviewEnabled ||
                (Addon.SelectedDrawable is null && Addon.SelectedDrawables.Count == 0) ||
                Addon.SelectedDrawables.All(d => d.Textures.Count == 0))
            {
                return;
            }

            var mainWindow = MainWindow.Instance;
            if (mainWindow?.PreviewAnchorable?.IsVisible != true) return;

            CWHelper.SendDrawableUpdateToPreview(e);
        }

        private void SelectedDrawable_TextureChanged(object sender, EventArgs e)
        {
            if (e is not SelectionChangedEventArgs args || args.AddedItems.Count == 0)
            {
                Addon.SelectedTexture = null;
                return;
            }

            args.Handled = true;
            Addon.SelectedTexture = (GTexture)args.AddedItems[0];

            if (!MainWindow.AddonManager.IsPreviewEnabled) return;

            var mainWindow = MainWindow.Instance;
            if (mainWindow?.PreviewAnchorable?.IsVisible != true) return;

            CWHelper.SendDrawableUpdateToPreview(e);
        }

        #region Drag and Drop for Drawables

        private void DrawablesGroupBox_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(WpfDataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(WpfDataFormats.FileDrop);
                    var yddFiles = files.Where(f => Path.GetExtension(f).Equals(".ydd", StringComparison.OrdinalIgnoreCase)).ToArray();
                    
                    e.Effects = yddFiles.Length > 0 ? WpfDragDropEffects.Copy : WpfDragDropEffects.None;
                }
                else if (e.Data.GetDataPresent("FileGroupDescriptor") || e.Data.GetDataPresent("FileGroupDescriptorW"))
                {
                    var filter = DragDropHelper.CreateExtensionFilter(".ydd");
                    var hasYddFiles = DragDropHelper.CheckForFilesInDescriptor(e.Data, filter);
                    e.Effects = hasYddFiles ? WpfDragDropEffects.Copy : WpfDragDropEffects.None;
                }
                else
                {
                    e.Effects = WpfDragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Ошибка проверки перетаскивания: {ex.Message}", LogType.Error);
                e.Effects = WpfDragDropEffects.None;
            }
            
            e.Handled = true;
        }

        private void DrawablesGroupBox_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(WpfDataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(WpfDataFormats.FileDrop);
                    e.Effects = files.Any(f => Path.GetExtension(f).Equals(".ydd", StringComparison.OrdinalIgnoreCase)) 
                        ? WpfDragDropEffects.Copy 
                        : WpfDragDropEffects.None;
                }
                else if (e.Data.GetDataPresent("FileGroupDescriptor") || e.Data.GetDataPresent("FileGroupDescriptorW"))
                {
                    e.Effects = WpfDragDropEffects.Copy;
                }
                else
                {
                    e.Effects = WpfDragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Ошибка перетаскивания: {ex.Message}", LogType.Error);
                e.Effects = WpfDragDropEffects.None;
            }
            
            e.Handled = true;
        }

        private void DrawablesGroupBox_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
        }

        private async void DrawablesGroupBox_Drop(object sender, System.Windows.DragEventArgs e)
        {
            bool progressStarted = false;
            bool success = false;
            try
            {
                List<string> filesToProcess = [];
                
                if (e.Data.GetDataPresent(WpfDataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(WpfDataFormats.FileDrop);
                    filesToProcess.AddRange(files);
                }
                else if (e.Data.GetDataPresent("FileGroupDescriptor") || e.Data.GetDataPresent("FileGroupDescriptorW"))
                {
                    var filter = DragDropHelper.CreateExtensionFilter(".ydd");
                    var extractedFiles = await DragDropHelper.ExtractVirtualFilesAsync(e.Data, filter);
                    if (extractedFiles.Count > 0)
                    {
                        filesToProcess.AddRange(extractedFiles);
                    }
                    else
                    {
                        LogHelper.Log("Не удалось извлечь виртуальные файлы", LogType.Error);
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    e.Handled = true;
                    return;
                }

                var yddFiles = filesToProcess.Where(f => Path.GetExtension(f).Equals(".ydd", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (yddFiles.Length == 0)
                {
                    e.Handled = true;
                    return;
                }

                var (accessibleFiles, inaccessibleFiles) = DragDropHelper.ValidateFileAccess(yddFiles);

                if (inaccessibleFiles.Count > 0)
                {
                    var message = $"Не удалось получить доступ к следующим файлам:\n\n" +
                                  string.Join("\n", inaccessibleFiles.Select(Path.GetFileName)) +
                                  "\n\nЭто могут быть виртуальные пути. Сначала извлеките файлы в папку и перетащите их оттуда.";
                     
                    CustomMessageBox.Show(message, "Файлы недоступны",
                        CustomMessageBox.CustomMessageBoxButtons.OKOnly, 
                        CustomMessageBox.CustomMessageBoxIcon.Warning);
                }

                if (accessibleFiles.Count == 0)
                {
                    e.Handled = true;
                    return;
                }

                var maleFiles = new List<string>();
                var femaleFiles = new List<string>();
                var undeterminedFiles = new List<string>();

                foreach (var file in accessibleFiles)
                {
                    var detectedGender = DetermineGenderFromFilename(file);
                    
                    if (detectedGender == Enums.SexType.male)
                    {
                        maleFiles.Add(file);
                    }
                    else if (detectedGender == Enums.SexType.female)
                    {
                        femaleFiles.Add(file);
                    }
                    else
                    {
                        undeterminedFiles.Add(file);
                    }
                }

                ProgressHelper.Start("Добавление перетащенной одежды...");
                progressStarted = true;

                if (maleFiles.Count > 0)
                {
                    await MainWindow.AddonManager.AddDrawables(maleFiles.ToArray(), Enums.SexType.male, targetAddon: Addon);
                }

                if (femaleFiles.Count > 0)
                {
                    await MainWindow.AddonManager.AddDrawables(femaleFiles.ToArray(), Enums.SexType.female, targetAddon: Addon);
                }

                if (undeterminedFiles.Count > 0)
                {
                    var result = CustomMessageBox.Show(
                        $"Не удалось определить пол для {undeterminedFiles.Count} файлов. Выберите пол одежды:",
                        "Выбор пола",
                        CustomMessageBox.CustomMessageBoxButtons.MaleFemaleCancel);

                    if (result == CustomMessageBox.CustomMessageBoxResult.Male)
                    {
                        await MainWindow.AddonManager.AddDrawables(undeterminedFiles.ToArray(), Enums.SexType.male, targetAddon: Addon);
                    }
                    else if (result == CustomMessageBox.CustomMessageBoxResult.Female)
                    {
                        await MainWindow.AddonManager.AddDrawables(undeterminedFiles.ToArray(), Enums.SexType.female, targetAddon: Addon);
                    }
                }

                SaveHelper.SetUnsavedChanges(true);
                success = true;
            }
            catch (Exception ex)
            {
                ErrorLogHelper.LogError("Ошибка при добавлении перетащенных файлов", ex);
                CustomMessageBox.Show(
                    LocalizationHelper.Format("Не удалось обработать перетащенные файлы: {0}", ErrorMessageHelper.Friendly(ex)),
                    LocalizationHelper.Translate("Ошибка перетаскивания"),
                    CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    CustomMessageBox.CustomMessageBoxIcon.Error);
            }
            finally
            {
                if (progressStarted)
                {
                    ProgressHelper.Stop(success ? LocalizationHelper.Translate("Одежда добавлена") : LocalizationHelper.Translate("Не удалось добавить одежду"), success);
                }
            }

            e.Handled = true;
        }

        private static Enums.SexType? DetermineGenderFromFilename(string filePath)
        {
            var filename = Path.GetFileName(filePath).ToLowerInvariant();
            if (filename.Contains("mp_m_freemode") || filename.Contains("_m_") || filename.Contains("male"))
            {
                return Enums.SexType.male;
            }

            if (filename.Contains("mp_f_freemode") || filename.Contains("_f_") || filename.Contains("female"))
            {
                return Enums.SexType.female;
            }

            return null;
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
