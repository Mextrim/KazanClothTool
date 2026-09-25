using CodeWalker;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using FormsControl = System.Windows.Forms.Control;
using FormsTreeNode = System.Windows.Forms.TreeNode;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for PreviewWindowHost.xaml
    /// </summary>
    public partial class PreviewWindowHost : System.Windows.Controls.UserControl
    {
        private CustomPedsForm _customPedsForm;
        private readonly Dictionary<object, string> _previewOriginalTexts = new();
        private bool _isInitialized = false;

        public static event EventHandler Preview3DAvailabilityChanged;

        public PreviewWindowHost()
        {
            InitializeComponent();
            LocalizationHelper.LanguageChanged += OnLanguageChanged;
            this.Loaded += PreviewWindowHost_Loaded;
            this.Unloaded += PreviewWindowHost_Unloaded;
        }

        private void PreviewWindowHost_Loaded(object sender, RoutedEventArgs e)
        {
            LocalizationHelper.LanguageChanged -= OnLanguageChanged;
            LocalizationHelper.LanguageChanged += OnLanguageChanged;
            if (_isInitialized && _customPedsForm != null && !_customPedsForm.IsDisposed && PreviewHost.Child == null)
            {
                PreviewHost.Child = _customPedsForm;
                if (MainWindow.AddonManager?.SelectedAddon?.SelectedDrawable is { IsEncrypted: false })
                {
                    PlaceholderPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ShowWaitingForSelection();
                }
            }
            else if (!_isInitialized && SettingsHelper.Preview3DAvailable)
            {
                InitializePreview();
            }
        }

        private void PreviewWindowHost_Unloaded(object sender, RoutedEventArgs e)
        {
            LocalizationHelper.LanguageChanged -= OnLanguageChanged;
            if (_customPedsForm != null && !_customPedsForm.IsDisposed && PreviewHost.Child != null)
            {
                PreviewHost.Child = null;
                PlaceholderPanel.Visibility = Visibility.Visible;
            }
        }

        public void InitializePreview()
        {
            if (!CWHelper.IsPreviewAvailable)
            {
                SettingsHelper.Preview3DAvailable = false;
                PlaceholderText.Text = LocalizationHelper.Translate("Путь GTA V не настроен. Выберите его в настройках, чтобы включить 3D-просмотр.");
                PlaceholderPanel.Visibility = Visibility.Visible;
                return;
            }

            if (_isInitialized && _customPedsForm != null && !_customPedsForm.IsDisposed)
            {
                if (MainWindow.AddonManager?.SelectedAddon?.SelectedDrawable is not { IsEncrypted: false })
                {
                    ShowWaitingForSelection();
                }
                return;
            }

            try
            {
                _customPedsForm = new CustomPedsForm
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };
                _customPedsForm.BatchExportRequested += RefreshBatchExportCache;
                _customPedsForm.ExportAllPngRequested += ExportAllPngSafely;

                PreviewHost.Child = _customPedsForm;
                _customPedsForm.Show();
                ApplyPreviewLocalization();

                ShowWaitingForSelection();
                _isInitialized = true;
                SettingsHelper.Preview3DAvailable = true;
                Preview3DAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                var errorMsg = LocalizationHelper.Format("Не удалось инициализировать 3D-просмотр: {0}", ex.Message);
                LogHelper.Log(errorMsg, Views.LogType.Error);
                ErrorLogHelper.LogError("Не удалось инициализировать 3D-просмотр", ex);
                
                bool isGtaError = ex.Message.Contains("GTA", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("DLC", StringComparison.OrdinalIgnoreCase) || ex.Message.Contains("corrupted", StringComparison.OrdinalIgnoreCase);
                PlaceholderText.Text = LocalizationHelper.Translate(isGtaError
                    ? "3D-просмотр недоступен: проверьте установку GTA V и журнал"
                    : "3D-просмотр недоступен: проверьте DirectX и видеодрайвер");
                
                SettingsHelper.Preview3DAvailable = false;
                Preview3DAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void InitializePreviewInBackground()
        {
            if (_isInitialized)
            {
                return;
            }

            if (!CWHelper.IsPreviewAvailable)
            {
                SettingsHelper.Preview3DAvailable = false;
                PlaceholderText.Text = LocalizationHelper.Translate("Путь GTA V не настроен. Выберите его в настройках, чтобы включить 3D-просмотр.");
                return;
            }

            try
            {
                _customPedsForm = new CustomPedsForm
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };
                _customPedsForm.BatchExportRequested += RefreshBatchExportCache;
                _customPedsForm.ExportAllPngRequested += ExportAllPngSafely;

                PreviewHost.Child = _customPedsForm;
                _customPedsForm.Show();
                ApplyPreviewLocalization();
                ShowWaitingForSelection();

                _isInitialized = true;
                SettingsHelper.Preview3DAvailable = true;
                Preview3DAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                var errorMsg = LocalizationHelper.Format("Не удалось инициализировать 3D-просмотр в фоне: {0}", ex.Message);
                LogHelper.Log(errorMsg, Views.LogType.Error);
                ErrorLogHelper.LogError("Не удалось инициализировать 3D-просмотр в фоне", ex);
                SettingsHelper.Preview3DAvailable = false;
                Preview3DAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ShowWaitingForSelection()
        {
            PlaceholderText.Text = LocalizationHelper.Translate("Выберите одежду слева или перетащите YDD в список");
            PlaceholderPanel.Visibility = Visibility.Visible;
        }

        public void ShowPreviewMessage(string message)
        {
            PlaceholderText.Text = LocalizationHelper.Translate(message);
            PlaceholderPanel.Visibility = Visibility.Visible;
        }

        public void MarkModelRendered()
        {
            if (_isInitialized && _customPedsForm != null && !_customPedsForm.IsDisposed)
            {
                PlaceholderPanel.Visibility = Visibility.Collapsed;
            }
        }

        public void ClearProjectSelection()
        {
            var form = _customPedsForm;
            if (form == null || form.IsDisposed)
            {
                return;
            }

            lock (form.RenderSyncRoot)
            {
                form.LoadedDrawables.Clear();
                form.LoadedTextures.Clear();
                form.SavedDrawables.Clear();
                form.SavedTextures.Clear();
                form.BatchLoadedDrawables.Clear();
                form.BatchLoadedTextureVariants.Clear();
                form.Refresh();
            }
        }

        private void ClosePreview_Click(object sender, RoutedEventArgs e)
        {
            ClosePreview();
            MainWindow.AddonManager.IsPreviewEnabled = false;
            MainWindow.Instance?.PreviewAnchorable.Hide();
        }

        public void ClosePreview()
        {
            var previewForm = _customPedsForm;
            _customPedsForm = null;
            _isInitialized = false;

            if (previewForm == null)
            {
                return;
            }

            try
            {
                previewForm.BatchExportRequested -= RefreshBatchExportCache;
                previewForm.ExportAllPngRequested -= ExportAllPngSafely;

                if (PreviewHost.Child == previewForm)
                {
                    PreviewHost.Child = null;
                }

                if (!previewForm.IsDisposed)
                {
                    previewForm.Close();
                    previewForm.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Ошибка закрытия 3D-просмотра: {ex.Message}", Views.LogType.Warning);
            }
            finally
            {
                PlaceholderText.Text = "3D-просмотр закрыт. Откройте его снова, когда он понадобится.";
                _previewOriginalTexts.Clear();
                PlaceholderPanel.Visibility = Visibility.Visible;
                SettingsHelper.Preview3DAvailable = true;
            }
        }

        public void SetPedModel(string pedModel)
        {
            try
            {
                if (_customPedsForm != null && !_customPedsForm.IsDisposed && _customPedsForm.formopen)
                {
                    _customPedsForm.PedModel = pedModel;
                }
            }
            catch (Exception ex)
            {
                HandlePreviewError("Не удалось установить модель пешехода", ex);
            }
        }

        public void UpdateDrawables(ObservableCollection<GDrawable> selectedDrawables, GTexture selectedTexture, Dictionary<string, string> updateDict)
        {
            try
            {
                if (_customPedsForm == null || _customPedsForm.IsDisposed || !_customPedsForm.formopen || _customPedsForm.isLoading)
                {
                    return;
                }

                lock (_customPedsForm.RenderSyncRoot)
                {
                var selectedNames = selectedDrawables.Select(d => d.Name).ToHashSet();
                var removedDrawables = _customPedsForm.LoadedDrawables.Keys.Where(name => !selectedNames.Contains(name)).ToList();
                foreach (var removed in removedDrawables)
                {
                    if (_customPedsForm.LoadedDrawables.TryGetValue(removed, out var removedDrawable))
                    {
                        _customPedsForm.LoadedTextures.Remove(removedDrawable);
                    }
                    _customPedsForm.LoadedDrawables.Remove(removed);
                }

                foreach (var drawable in selectedDrawables)
                {
                    if (drawable.IsEncrypted)
                    {
                        continue;
                    }

                    var ydd = CWHelper.CreateYddFile(drawable);
                    if (ydd == null || ydd.Drawables.Length == 0) continue;

                    var firstDrawable = ydd.Drawables.First();
                    _customPedsForm.LoadedDrawables[drawable.Name] = firstDrawable;

                    CodeWalker.GameFiles.YtdFile ytd = null;
                    if (selectedTexture != null)
                    {
                        ytd = CWHelper.CreateYtdFile(selectedTexture, selectedTexture.DisplayName);
                        _customPedsForm.LoadedTextures[firstDrawable] = ytd.TextureDict;
                    }

                    if (selectedTexture == null && selectedDrawables.Count > 1)
                    {
                        var firstTexture = drawable.Textures.FirstOrDefault();
                        if (firstTexture != null)
                        {
                            ytd = CWHelper.CreateYtdFile(firstTexture, firstTexture.DisplayName);
                            _customPedsForm.LoadedTextures[firstDrawable] = ytd.TextureDict;
                        }
                    }

                    _customPedsForm.UpdateSelectedDrawable(
                        firstDrawable,
                        ytd?.TextureDict,
                        updateDict
                    );
                }

                _customPedsForm.Refresh();
                }
            }
            catch (Exception ex)
            {
                HandlePreviewError("Не удалось обновить одежду в 3D-просмотре", ex);
            }
        }

        private void RefreshBatchExportCache()
        {
            if (_customPedsForm == null || MainWindow.AddonManager?.SelectedAddon?.Drawables == null)
            {
                return;
            }

            _customPedsForm.BatchLoadedDrawables.Clear();
            _customPedsForm.BatchLoadedTextureVariants.Clear();

            foreach (var drawable in MainWindow.AddonManager.SelectedAddon.Drawables.OfType<GDrawable>())
            {
                if (drawable == null || drawable.IsEncrypted || string.IsNullOrWhiteSpace(drawable.Name))
                {
                    continue;
                }

                try
                {
                    var ydd = CWHelper.CreateYddFile(drawable);
                    if (ydd == null || ydd.Drawables == null || ydd.Drawables.Length == 0)
                    {
                        continue;
                    }

                    var firstDrawable = ydd.Drawables.First();
                    _customPedsForm.BatchLoadedDrawables[drawable.Name] = firstDrawable;
                    var textureVariants = new List<CodeWalker.GameFiles.TextureDictionary>();

                    foreach (var texture in drawable.Textures ?? new ObservableCollection<GTexture>())
                    {
                        if (texture == null || texture.IsPreviewDisabled)
                        {
                            continue;
                        }

                        try
                        {
                            var ytd = CWHelper.CreateYtdFile(texture, texture.DisplayName);
                            if (ytd?.TextureDict != null)
                            {
                                textureVariants.Add(ytd.TextureDict);
                            }
                        }
                        catch (Exception textureEx)
                        {
                            LogHelper.Log($"Текстура «{texture.DisplayName}» пропущена при подготовке экспорта PNG: {textureEx.Message}", Views.LogType.Warning);
                        }
                    }

                    _customPedsForm.BatchLoadedTextureVariants[firstDrawable] = textureVariants;
                }
                catch (Exception drawableEx)
                {
                    LogHelper.Log($"Элемент одежды «{drawable.Name}» пропущен при подготовке экспорта PNG: {drawableEx.Message}", Views.LogType.Warning);
                }
            }
        }

        private void OnLanguageChanged(object? sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(ApplyPreviewLocalization));
        }

        private void ApplyPreviewLocalization()
        {
            if (_customPedsForm == null || _customPedsForm.IsDisposed)
            {
                return;
            }

            ApplyPreviewControlLocalization(_customPedsForm, new HashSet<object>());
        }

        private void ApplyPreviewControlLocalization(FormsControl control, HashSet<object> visited)
        {
            if (control == null || !visited.Add(control))
            {
                return;
            }

            if (control is not TextBoxBase)
            {
                control.Text = TranslatePreviewText(control, control.Text);
            }
            if (control.ContextMenuStrip != null)
            {
                ApplyPreviewItemsLocalization(control.ContextMenuStrip.Items, visited);
            }

            foreach (Control child in control.Controls)
            {
                ApplyPreviewControlLocalization(child, visited);
            }

            if (control is TreeView treeView)
            {
                foreach (TreeNode node in treeView.Nodes)
                {
                    ApplyPreviewTreeNodeLocalization(node, visited);
                }
            }

            if (control is ListView listView)
            {
                foreach (ColumnHeader column in listView.Columns)
                {
                    column.Text = TranslatePreviewText(column, column.Text);
                }
            }

            if (control is DataGridView dataGridView)
            {
                foreach (DataGridViewColumn column in dataGridView.Columns)
                {
                    column.HeaderText = TranslatePreviewText(column, column.HeaderText);
                }
            }

            if (control is ComboBox comboBox)
            {
                for (int index = 0; index < comboBox.Items.Count; index++)
                {
                    if (comboBox.Items[index] is string item)
                    {
                        comboBox.Items[index] = TranslatePreviewText(comboBox.Items[index], item);
                    }
                }
            }
        }

        private void ApplyPreviewItemsLocalization(ToolStripItemCollection items, HashSet<object> visited)
        {
            foreach (ToolStripItem item in items)
            {
                if (!visited.Add(item))
                {
                    continue;
                }

                item.Text = TranslatePreviewText(item, item.Text);
                item.ToolTipText = TranslatePreviewText(item, item.ToolTipText);
                if (item is ToolStripDropDownItem dropDownItem)
                {
                    ApplyPreviewItemsLocalization(dropDownItem.DropDownItems, visited);
                }
            }
        }

        private void ApplyPreviewTreeNodeLocalization(FormsTreeNode node, HashSet<object> visited)
        {
            if (!visited.Add(node))
            {
                return;
            }

            node.Text = TranslatePreviewText(node, node.Text);
            node.ToolTipText = TranslatePreviewText(node, node.ToolTipText);
            foreach (FormsTreeNode child in node.Nodes)
            {
                ApplyPreviewTreeNodeLocalization(child, visited);
            }
        }

        private string TranslatePreviewText(object owner, string current)
        {
            if (string.IsNullOrEmpty(current))
            {
                return current;
            }

            if (!_previewOriginalTexts.TryGetValue(owner, out string? source))
            {
                source = current;
                _previewOriginalTexts[owner] = source;
            }

            return LocalizationHelper.Translate(source);
        }

        private void HandlePreviewError(string context, Exception ex)
        {
            ErrorLogHelper.LogError($"Ошибка 3D-просмотра — {context}: {ex.Message}", ex);
            LogHelper.Log($"Ошибка 3D-просмотра: {context}", Views.LogType.Warning);
            
            // Disable the preview if it's having issues
            try
            {
                if (_customPedsForm != null && !_customPedsForm.IsDisposed)
                {
                    _customPedsForm.BatchExportRequested -= RefreshBatchExportCache;
                    _customPedsForm.ExportAllPngRequested -= ExportAllPngSafely;
                    _customPedsForm.Dispose();
                }
                _customPedsForm = null;
                _isInitialized = false;
                SettingsHelper.Preview3DAvailable = false;
                
                if (PlaceholderText != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        PlaceholderText.Text = LocalizationHelper.Translate("3D-просмотр отключён из-за ошибок. Подробности в журнале");
                        PlaceholderPanel.Visibility = Visibility.Visible;
                    });
                }
                
                Preview3DAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                // Silently fail if we can't clean up
            }
        }
    }
}
