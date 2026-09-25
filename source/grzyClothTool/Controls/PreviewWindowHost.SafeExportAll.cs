using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace grzyClothTool.Controls
{
    public partial class PreviewWindowHost
    {
        private void ExportAllPngSafely(string outputRoot)
        {
            if (_customPedsForm == null || _customPedsForm.IsDisposed || !_customPedsForm.formopen)
            {
                CustomMessageBox.Show("3D-предпросмотр ещё не готов.", "Экспорт всех PNG", CustomMessageBox.CustomMessageBoxButtons.OKOnly, CustomMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            var addon = MainWindow.AddonManager?.SelectedAddon;
            if (addon?.Drawables == null)
            {
                CustomMessageBox.Show("Аддон не загружен.", "Экспорт всех PNG", CustomMessageBox.CustomMessageBoxButtons.OKOnly, CustomMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            var items = addon.Drawables
                .OfType<GDrawable>()
                .Where(d => d != null && !d.IsEncrypted && d.Textures != null && d.Textures.Count > 0)
                .SelectMany(d => d.Textures
                    .Where(t => t != null && !t.IsPreviewDisabled)
                    .Select(t => new ExportItem { Drawable = d, Texture = t }))
                .ToList();

            if (items.Count == 0)
            {
                CustomMessageBox.Show("Текстуры одежды для экспорта не найдены.", "Экспорт всех PNG", CustomMessageBox.CustomMessageBoxButtons.OKOnly, CustomMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            ExportSettings settings;
            using (var configForm = new ExportConfigForm(items.Count))
            {
                if (configForm.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                settings = configForm.Settings;
            }

            if ((settings.Mode == ExportMode.Hard || settings.Mode == ExportMode.Extreme) &&
                CustomMessageBox.Show("Режимы «Высокий» и «Экстремальный» нагружают GPU и могут привести к сбою приложения или тайм-ауту драйвера AMD/NVIDIA.\n\nПродолжить в режиме " + GetModeDisplayName(settings.Mode) + "?",
                    "Риск сбоя GPU", CustomMessageBox.CustomMessageBoxButtons.YesNo, CustomMessageBox.CustomMessageBoxIcon.Warning) != CustomMessageBox.CustomMessageBoxResult.Yes)
            {
                return;
            }

            Directory.CreateDirectory(outputRoot);

            var progress = new ExportProgressForm(settings);
            var exported = 0;
            var failed = 0;
            var records = new List<ExportRecord>();
            var previousDrawables = addon.SelectedDrawables?.OfType<GDrawable>().ToList() ?? new List<GDrawable>();
            GDrawable previousDrawable = addon.SelectedDrawable;
            GTexture previousTexture = addon.SelectedTexture;
            object currentSex = null;
            var oldRenderLoopPaused = _customPedsForm.Renderer?.DXMan?.RenderLoopPaused ?? false;

            try
            {
                if (settings.PauseRenderLoop && _customPedsForm.Renderer?.DXMan != null)
                {
                    _customPedsForm.Renderer.DXMan.RenderLoopPaused = true;
                    Thread.Sleep(250);
                    Application.DoEvents();
                }

                progress.Show();
                progress.SetStatus("Начало экспорта в режиме " + GetModeDisplayName(settings.Mode) + "...", 0, items.Count, 0, 0, GetMemoryMb());
                Application.DoEvents();

                for (var i = 0; i < items.Count; i++)
                {
                    if (progress.CancelRequested)
                    {
                        break;
                    }

                    var item = items[i];
                    var component = SafePathSegment((item.Drawable.TypeName ?? "unknown").ToLowerInvariant());
                    var drawableId = SafePathSegment(item.Drawable.DisplayNumber);
                    var textureId = item.Texture.TxtNumber.ToString("000");
                    var relativePath = component + "/" + drawableId + "/" + textureId + ".png";
                    var outputPath = Path.Combine(outputRoot, component, drawableId, textureId + ".png");

                    progress.SetStatus("Экспортируется " + relativePath, i + 1, items.Count, exported, failed, GetMemoryMb());
                    Application.DoEvents();

                    if (!StayUnderRamLimit(progress, settings))
                    {
                        failed += items.Count - i;
                        LogHelper.Log("Экспорт всех PNG остановлен: объём ОЗУ остался выше установленного безопасного предела.", Views.LogType.Warning);
                        break;
                    }

                    try
                    {
                        var outputDirectory = Path.GetDirectoryName(outputPath);
                        if (!string.IsNullOrWhiteSpace(outputDirectory))
                        {
                            Directory.CreateDirectory(outputDirectory);
                        }

                        var updates = new Dictionary<string, string>();
                        if (currentSex == null || !currentSex.Equals(item.Drawable.Sex))
                        {
                            var sexName = item.Drawable.Sex.ToString().ToLowerInvariant();
                            SetPedModel(sexName == "male" ? "mp_m_freemode_01" : "mp_f_freemode_01");
                            updates["GenderChanged"] = string.Empty;
                            currentSex = item.Drawable.Sex;
                            WaitWithEvents(settings.PreviewSettleMs);
                        }

                        addon.SelectedDrawables.Clear();
                        addon.SelectedDrawables.Add(item.Drawable);
                        addon.SelectedDrawable = item.Drawable;
                        addon.SelectedTexture = item.Texture;

                        UpdateDrawables(addon.SelectedDrawables, item.Texture, updates);

                        WaitWithEvents(settings.PreviewSettleMs);

                        ExportItemPngWithRetries(outputPath, relativePath, settings);
                        exported++;

                        records.Add(new ExportRecord
                        {
                            Component = component,
                            DrawableId = drawableId,
                            TextureId = textureId,
                            DrawableName = item.Drawable.Name,
                            TextureName = item.Texture.DisplayName,
                            Path = relativePath
                        });
                    }
                    catch (SharpDX.SharpDXException ex)
                    {
                        failed++;
                        LogHelper.Log("Ошибка DirectX при экспорте всех PNG (" + relativePath + "): " + ex.Message, Views.LogType.Error);
                        CustomMessageBox.Show("При экспорте произошла ошибка DirectX или драйвера GPU. Экспорт остановлен для защиты приложения.\n\nПоследний элемент: " + relativePath + "\n\nИспользуйте режим «Низкий». Если ошибка повторится, перезапустите приложение перед новым экспортом: устройство GPU может быть нестабильным.", "Экспорт всех PNG", CustomMessageBox.CustomMessageBoxButtons.OKOnly, CustomMessageBox.CustomMessageBoxIcon.Warning);
                        break;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        LogHelper.Log("Элемент пропущен при экспорте всех PNG (" + relativePath + "): " + ex.Message, Views.LogType.Warning);
                    }

                    if (settings.ItemDelayMs > 0)
                    {
                        WaitWithEvents(settings.ItemDelayMs);
                    }

                    if (settings.PauseEveryItems > 0 && (i + 1) % settings.PauseEveryItems == 0)
                    {
                        ForceMemoryCleanup();
                        WaitWithEvents(settings.BatchPauseMs);
                    }
                }
            }
            finally
            {
                if (_customPedsForm.Renderer?.DXMan != null)
                {
                    _customPedsForm.Renderer.DXMan.RenderLoopPaused = oldRenderLoopPaused;
                }

                WriteExportManifest(outputRoot, records);
                RestoreExportSelection(addon, previousDrawables, previousDrawable, previousTexture);
                ForceMemoryCleanup();

                if (!progress.IsDisposed)
                {
                    progress.Close();
                }
            }

            CustomMessageBox.Show("Экспортировано: " + exported + "\nПропущено или с ошибкой: " + failed, "Экспорт всех PNG", CustomMessageBox.CustomMessageBoxButtons.OKOnly, CustomMessageBox.CustomMessageBoxIcon.Information);
        }

        private static string GetModeDisplayName(ExportMode mode) => mode switch
        {
            ExportMode.Low => "Низкий",
            ExportMode.Stable => "Стабильный",
            ExportMode.Medium => "Средний",
            ExportMode.Hard => "Высокий",
            ExportMode.Extreme => "Экстремальный",
            _ => mode.ToString()
        };

        private static ExportMode GetModeFromDisplayName(string displayName) => displayName switch
        {
            "Низкий" => ExportMode.Low,
            "Стабильный" => ExportMode.Stable,
            "Средний" => ExportMode.Medium,
            "Высокий" => ExportMode.Hard,
            "Экстремальный" => ExportMode.Extreme,
            _ => ExportMode.Low
        };

        private void ExportItemPngWithRetries(string outputPath, string relativePath, ExportSettings settings)
        {
            Exception lastError = null;
            var tempPath = outputPath + ".tmp";

            for (var attempt = 1; attempt <= Math.Max(1, settings.RetryCount); attempt++)
            {
                try
                {
                    DeleteFileIfExists(tempPath);
                    _customPedsForm.ExportCurrentPreviewPngStable(tempPath);

                    if (!File.Exists(tempPath))
                    {
                        throw new IOException("Экспорт завершён, но PNG-файл не был создан.");
                    }

                    DeleteFileIfExists(outputPath);
                    File.Move(tempPath, outputPath);
                    return;
                }
                catch (SharpDX.SharpDXException)
                {
                    DeleteFileIfExists(tempPath);
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    DeleteFileIfExists(tempPath);
                    LogHelper.Log("Попытка экспорта " + attempt + "/" + settings.RetryCount + " не удалась (" + relativePath + "): " + ex.Message, Views.LogType.Warning);
                    ForceMemoryCleanup();
                    WaitWithEvents(settings.RetryDelayMs);
                }
            }

            throw new InvalidOperationException("Не удалось выполнить после " + settings.RetryCount + " попыток: " + (lastError?.Message ?? "неизвестная ошибка"), lastError);
        }

        private static void DeleteFileIfExists(string path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private static bool StayUnderRamLimit(ExportProgressForm progress, ExportSettings settings)
        {
            var limitBytes = settings.RamLimitMb * 1024L * 1024L;
            var memory = Process.GetCurrentProcess().PrivateMemorySize64;
            if (memory < limitBytes)
            {
                return true;
            }

            progress.SetStatus("Достигнут лимит ОЗУ. Очистка памяти...", 0, 1, 0, 0, GetMemoryMb());
            Application.DoEvents();

            for (var i = 0; i < 3; i++)
            {
                ForceMemoryCleanup();
                WaitWithEvents(750);

                if (Process.GetCurrentProcess().PrivateMemorySize64 < limitBytes)
                {
                    return true;
                }
            }

            return false;
        }

        private static long GetMemoryMb()
        {
            return Process.GetCurrentProcess().PrivateMemorySize64 / 1024L / 1024L;
        }

        private static void ForceMemoryCleanup()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private static void WaitWithEvents(int milliseconds)
        {
            var remaining = Math.Max(0, milliseconds);
            while (remaining > 0)
            {
                var step = Math.Min(100, remaining);
                Thread.Sleep(step);
                Application.DoEvents();
                remaining -= step;
            }
        }

        private void RestoreExportSelection(dynamic addon, List<GDrawable> previousDrawables, GDrawable previousDrawable, GTexture previousTexture)
        {
            try
            {
                addon.SelectedDrawables.Clear();
                foreach (var drawable in previousDrawables)
                {
                    addon.SelectedDrawables.Add(drawable);
                }

                addon.SelectedDrawable = previousDrawable;
                addon.SelectedTexture = previousTexture;

                if (previousDrawables.Count > 0)
                {
                    UpdateDrawables(addon.SelectedDrawables, previousTexture, new Dictionary<string, string>());
                }
            }
            catch
            {
            }
        }

        private static string SafePathSegment(string value)
        {
            value = string.IsNullOrWhiteSpace(value) ? "unknown" : value;
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }
            return value;
        }

        private static string EscapeJson(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static void WriteExportManifest(string outputRoot, List<ExportRecord> records)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"exportedAt\": \"" + DateTime.UtcNow.ToString("O") + "\",");
            sb.AppendLine("  \"files\": [");

            for (var i = 0; i < records.Count; i++)
            {
                var r = records[i];
                sb.AppendLine("    {");
                sb.AppendLine("      \"component\": \"" + EscapeJson(r.Component) + "\",");
                sb.AppendLine("      \"drawableId\": \"" + EscapeJson(r.DrawableId) + "\",");
                sb.AppendLine("      \"textureId\": \"" + EscapeJson(r.TextureId) + "\",");
                sb.AppendLine("      \"drawableName\": \"" + EscapeJson(r.DrawableName) + "\",");
                sb.AppendLine("      \"textureName\": \"" + EscapeJson(r.TextureName) + "\",");
                sb.AppendLine("      \"path\": \"" + EscapeJson(r.Path) + "\"");
                sb.Append("    }");
                if (i < records.Count - 1)
                {
                    sb.Append(",");
                }
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            File.WriteAllText(Path.Combine(outputRoot, "export_manifest.json"), sb.ToString(), Encoding.UTF8);
        }

        private class ExportItem
        {
            public GDrawable Drawable;
            public GTexture Texture;
        }

        private class ExportRecord
        {
            public string Component;
            public string DrawableId;
            public string TextureId;
            public string DrawableName;
            public string TextureName;
            public string Path;
        }

        private enum ExportMode
        {
            Low,
            Stable,
            Medium,
            Hard,
            Extreme
        }

        private class ExportSettings
        {
            public ExportMode Mode;
            public int PreviewSettleMs;
            public int ItemDelayMs;
            public int PauseEveryItems;
            public int BatchPauseMs;
            public int RamLimitMb;
            public bool PauseRenderLoop;
            public int RetryCount;
            public int RetryDelayMs;

            public static ExportSettings FromMode(ExportMode mode)
            {
                switch (mode)
                {
                    case ExportMode.Low:
                        return new ExportSettings { Mode = mode, PreviewSettleMs = 1500, ItemDelayMs = 1500, PauseEveryItems = 1, BatchPauseMs = 3500, RamLimitMb = 1200, PauseRenderLoop = true, RetryCount = 3, RetryDelayMs = 1500 };
                    case ExportMode.Stable:
                        return new ExportSettings { Mode = mode, PreviewSettleMs = 900, ItemDelayMs = 1000, PauseEveryItems = 3, BatchPauseMs = 2500, RamLimitMb = 2000, PauseRenderLoop = true, RetryCount = 2, RetryDelayMs = 1000 };
                    case ExportMode.Medium:
                        return new ExportSettings { Mode = mode, PreviewSettleMs = 500, ItemDelayMs = 600, PauseEveryItems = 6, BatchPauseMs = 1500, RamLimitMb = 3000, PauseRenderLoop = true, RetryCount = 2, RetryDelayMs = 800 };
                    case ExportMode.Hard:
                        return new ExportSettings { Mode = mode, PreviewSettleMs = 300, ItemDelayMs = 250, PauseEveryItems = 12, BatchPauseMs = 800, RamLimitMb = 4500, PauseRenderLoop = true, RetryCount = 1, RetryDelayMs = 500 };
                    case ExportMode.Extreme:
                        return new ExportSettings { Mode = mode, PreviewSettleMs = 125, ItemDelayMs = 75, PauseEveryItems = 25, BatchPauseMs = 250, RamLimitMb = 7000, PauseRenderLoop = false, RetryCount = 1, RetryDelayMs = 300 };
                    default:
                        return FromMode(ExportMode.Low);
                }
            }
        }

        private class ExportConfigForm : Form
        {
            private readonly ComboBox modeBox;
            private readonly NumericUpDown settleBox;
            private readonly NumericUpDown delayBox;
            private readonly NumericUpDown pauseEveryBox;
            private readonly NumericUpDown pauseMsBox;
            private readonly NumericUpDown ramBox;
            private readonly NumericUpDown retryBox;
            private readonly NumericUpDown retryDelayBox;
            private readonly CheckBox pauseRenderLoopBox;
            private readonly Label warningLabel;
            private readonly int totalItems;
            private bool applyingMode;

            public ExportSettings Settings { get; private set; }

            public ExportConfigForm(int totalItems)
            {
                this.totalItems = totalItems;
                Text = "Экспорт всех PNG — настройка";
                Width = 540;
                Height = 425;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                StartPosition = FormStartPosition.CenterScreen;

                var intro = new Label
                {
                    Left = 12,
                    Top = 12,
                    Width = 500,
                    Height = 42,
                    Text = "Режим «Низкий» теперь используется по умолчанию как самый безопасный. Более быстрые режимы нагружают GPU и драйвер, поэтому их не рекомендуется использовать для больших пакетов."
                };

                var modeLabel = new Label { Left = 12, Top = 62, Width = 130, Height = 22, Text = "Режим" };
                modeBox = new ComboBox { Left = 170, Top = 60, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
                modeBox.Items.AddRange(new object[]
                {
                    GetModeDisplayName(ExportMode.Low),
                    GetModeDisplayName(ExportMode.Stable),
                    GetModeDisplayName(ExportMode.Medium),
                    GetModeDisplayName(ExportMode.Hard),
                    GetModeDisplayName(ExportMode.Extreme)
                });
                modeBox.SelectedItem = GetModeDisplayName(ExportMode.Low);
                modeBox.SelectedIndexChanged += (s, e) => ApplyMode(GetModeFromDisplayName(modeBox.SelectedItem?.ToString()));

                settleBox = AddNumber("Задержка предпросмотра, мс", 92, 0, 10000);
                delayBox = AddNumber("Задержка на PNG, мс", 122, 0, 15000);
                pauseEveryBox = AddNumber("Пауза каждые N PNG", 152, 1, 1000);
                pauseMsBox = AddNumber("Пауза пакета, мс", 182, 0, 30000);
                ramBox = AddNumber("Предел ОЗУ, МБ", 212, 512, 64000);
                retryBox = AddNumber("Повторов на PNG", 242, 1, 10);
                retryDelayBox = AddNumber("Задержка повтора, мс", 272, 0, 10000);

                pauseRenderLoopBox = new CheckBox
                {
                    Left = 170,
                    Top = 302,
                    Width = 330,
                    Height = 24,
                    Text = "Приостановить 3D-рендеринг во время экспорта (рекомендуется)"
                };

                warningLabel = new Label
                {
                    Left = 12,
                    Top = 330,
                    Width = 500,
                    Height = 42,
                    Text = "Рекомендуется режим «Низкий». Всего PNG: " + totalItems
                };

                var okButton = new Button { Left = 332, Top = 377, Width = 85, Height = 26, Text = "Начать" };
                var cancelButton = new Button { Left = 427, Top = 377, Width = 85, Height = 26, Text = "Отмена" };
                okButton.Click += (s, e) => { SaveSettings(); DialogResult = DialogResult.OK; Close(); };
                cancelButton.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

                Controls.Add(intro);
                Controls.Add(modeLabel);
                Controls.Add(modeBox);
                Controls.Add(pauseRenderLoopBox);
                Controls.Add(warningLabel);
                Controls.Add(okButton);
                Controls.Add(cancelButton);

                ApplyMode(ExportMode.Low);
            }

            private NumericUpDown AddNumber(string label, int top, int minimum, int maximum)
            {
                Controls.Add(new Label { Left = 12, Top = top + 2, Width = 150, Height = 22, Text = label });
                var box = new NumericUpDown { Left = 170, Top = top, Width = 120, Minimum = minimum, Maximum = maximum, Increment = 50 };
                Controls.Add(box);
                return box;
            }

            private void ApplyMode(ExportMode mode)
            {
                if (applyingMode) return;
                applyingMode = true;
                var s = ExportSettings.FromMode(mode);
                settleBox.Value = s.PreviewSettleMs;
                delayBox.Value = s.ItemDelayMs;
                pauseEveryBox.Value = s.PauseEveryItems;
                pauseMsBox.Value = s.BatchPauseMs;
                ramBox.Value = s.RamLimitMb;
                retryBox.Value = s.RetryCount;
                retryDelayBox.Value = s.RetryDelayMs;
                pauseRenderLoopBox.Checked = s.PauseRenderLoop;

                var approxSeconds = totalItems * (s.PreviewSettleMs + s.ItemDelayMs) / 1000.0;
                warningLabel.Text = mode == ExportMode.Hard || mode == ExportMode.Extreme
                    ? "ПРЕДУПРЕЖДЕНИЕ: режим «" + GetModeDisplayName(mode) + "» может привести к сбою приложения или драйвера GPU. Примерное время: " + Math.Round(approxSeconds / 60.0, 1) + " мин."
                    : "Выбран режим «" + GetModeDisplayName(mode) + "». Примерное время: " + Math.Round(approxSeconds / 60.0, 1) + " мин.";
                applyingMode = false;
            }

            private void SaveSettings()
            {
                Settings = new ExportSettings
                {
                    Mode = GetModeFromDisplayName(modeBox.SelectedItem?.ToString()),
                    PreviewSettleMs = (int)settleBox.Value,
                    ItemDelayMs = (int)delayBox.Value,
                    PauseEveryItems = (int)pauseEveryBox.Value,
                    BatchPauseMs = (int)pauseMsBox.Value,
                    RamLimitMb = (int)ramBox.Value,
                    PauseRenderLoop = pauseRenderLoopBox.Checked,
                    RetryCount = (int)retryBox.Value,
                    RetryDelayMs = (int)retryDelayBox.Value
                };
            }
        }

        private class ExportProgressForm : Form
        {
            private readonly Label statusLabel;
            private readonly Label countLabel;
            private readonly ProgressBar progressBar;
            private readonly Button cancelButton;
            private readonly ExportSettings settings;

            public bool CancelRequested { get; private set; }

            public ExportProgressForm(ExportSettings settings)
            {
                this.settings = settings;
                Text = "Экспорт всех PNG — " + GetModeDisplayName(settings.Mode);
                Width = 660;
                Height = 180;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                StartPosition = FormStartPosition.CenterScreen;
                TopMost = true;

                statusLabel = new Label { Left = 12, Top = 12, Width = 620, Height = 24, Text = "Подготовка..." };
                countLabel = new Label { Left = 12, Top = 40, Width = 620, Height = 48, Text = "0%" };
                progressBar = new ProgressBar { Left = 12, Top = 92, Width = 620, Height = 20, Minimum = 0, Maximum = 100 };
                cancelButton = new Button { Left = 532, Top = 120, Width = 100, Height = 26, Text = "Отмена" };
                cancelButton.Click += (s, e) => { CancelRequested = true; cancelButton.Enabled = false; cancelButton.Text = "Отмена..."; };

                Controls.Add(statusLabel);
                Controls.Add(countLabel);
                Controls.Add(progressBar);
                Controls.Add(cancelButton);
            }

            public void SetStatus(string status, int processed, int total, int exported, int failed, long ramMb)
            {
                if (IsDisposed) return;
                total = Math.Max(1, total);
                var percent = Math.Max(0, Math.Min(100, (int)Math.Round(processed * 100.0 / total)));
                statusLabel.Text = status;
                countLabel.Text = percent + "% | обработано: " + processed + "/" + total + " | экспортировано: " + exported + " | ошибок: " + failed + " | ОЗУ: " + ramMb + " МБ\nРежим: " + GetModeDisplayName(settings.Mode) + " | Задержка: " + settings.ItemDelayMs + " мс | Повторы: " + settings.RetryCount + " | Пауза каждые " + settings.PauseEveryItems + " | Рендеринг приостановлен: " + settings.PauseRenderLoop;
                progressBar.Value = percent;
                Refresh();
            }
        }
    }
}
