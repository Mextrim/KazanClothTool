using CodeWalker.GameFiles;
using grzyClothTool.Constants;
using grzyClothTool.Controls;
using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Other;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace grzyClothTool.Models
{

    public class AddonManagerDesign : AddonManager
    {
        public AddonManagerDesign()
        {
            ProjectName = "Design";
            Addons = [];

            Addons.Add(new Addon("design"));
            SelectedAddon = Addons.First();
        }
    }

    public class AddonManager : INotifyPropertyChanged
    {
        private abstract record WorkItem;
        private record DrawableWorkItem(
            string FilePath,
            Enums.SexType Sex,
            string BasePath,
            PedFile Ymt,
            PedAlternativeVariations PedAltVariations,
            Dictionary<(int, int), MCComponentInfo> CompInfoDict,
            Dictionary<(int, int), MCPedPropMetaData> PedPropMetaDataDict,
            Dictionary<(int, bool), int> TypeNumericCounts,
            Addon TargetAddon
        ) : WorkItem;
        private record CompletionMarker(TaskCompletionSource Tcs, Addon TargetAddon) : WorkItem;

        private readonly BlockingCollection<WorkItem> _drawableQueue = new();
        private Task? _drawableProcessingTask;
        private int _queueStarted;

        public static readonly object AddonsLock = new();

        private static readonly Regex AlternateRegex = new(@"_\w_\d+\.ydd$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PhysicsRegex = new(@"\.yld$", RegexOptions.Compiled);

        public event PropertyChangedEventHandler PropertyChanged;

        private string _projectName = string.Empty;
        public string ProjectName
        {
            get => _projectName;
            set
            {
                string normalized = value ?? string.Empty;
                if (!string.Equals(_projectName, normalized, StringComparison.Ordinal))
                {
                    _projectName = normalized;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasProject));
                }
            }
        }

        [JsonIgnore]
        public bool HasProject => !string.IsNullOrWhiteSpace(_projectName) && Addons.Count > 0;

        /// <summary>
        /// When true, files remain in their original external locations
        /// When false (default), files are copied to the project folder
        /// </summary>
        public bool IsExternalProject { get; set; }

        [JsonInclude]
        private string SavedAt => DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

        public ObservableCollection<string> Groups { get; set; } = [];
        public ObservableCollection<string> Tags { get; set; } = [];
        
        [JsonIgnore]
        public ObservableCollection<MoveMenuItem> MoveMenuItems { get; set; } = [];

        private ObservableCollection<Addon> _addons = [];
        public ObservableCollection<Addon> Addons
        {
            get { return _addons; }
            set
            {
                if (_addons == value)
                {
                    return;
                }

                _addons.CollectionChanged -= Addons_CollectionChanged;
                _addons = value ?? [];
                _addons.CollectionChanged += Addons_CollectionChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasProject));
            }
        }

        private Addon _selectedAddon;
        [JsonIgnore]
        public Addon SelectedAddon
        {
            get { return _selectedAddon; }
            set
            {
                if (_selectedAddon != value)
                {
                    _selectedAddon = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isPreviewEnabled;
        [JsonIgnore]
        public bool IsPreviewEnabled
        {
            get { return _isPreviewEnabled; }
            set
            {
                _isPreviewEnabled = value;
                OnPropertyChanged();
            }
        }

        public AddonManager()
        {
            _addons.CollectionChanged += Addons_CollectionChanged;
        }

        private void Addons_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasProject));
        }

        private void EnsureDrawableQueueStarted()
        {
            if (Interlocked.Exchange(ref _queueStarted, 1) == 0)
            {
                _drawableProcessingTask = Task.Run(ProcessDrawableQueue);
            }
        }

        public void CreateAddon()
        {
            var name = "Аддон " + (Addons.Count + 1);

            Addons.Add(new Addon(name));
            RebuildMoveMenuItems();
            OnPropertyChanged("Addons");
        }

        private async Task<PedAlternativeVariations> LoadPedAlternativeVariationsFileAsync(string dirPath, string addonName)
        {
            try
            {
                var pedAltVariationsFiles = await Task.Run(() => 
                    Directory.GetFiles(dirPath, "pedalternativevariations*.meta", SearchOption.AllDirectories)
                        .Where(x => x.Contains(addonName))
                        .ToArray());

                if (pedAltVariationsFiles.Length == 0)
                {
                    return null;
                }

                var pedAltVariationsFile = pedAltVariationsFiles.FirstOrDefault();
                if (pedAltVariationsFile == null)
                {
                    return null;
                }

                var xmlDoc = await Task.Run(() => XDocument.Load(pedAltVariationsFile));
                return PedAlternativeVariations.FromXml(xmlDoc);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Ошибка загрузки pedalternativevariations.meta: {ex.Message}", Views.LogType.Warning);
                return null;
            }
        }

        public async Task<bool> LoadAddon(string path, bool shouldSetProjectName = false)
        {
            var dirPath = Path.GetDirectoryName(path);
            var addonName = Path.GetFileNameWithoutExtension(path);

            // Determine if the addonName indicates male or female
            Enums.SexType sex = addonName.Contains("mp_m_freemode_01") ? Enums.SexType.male : Enums.SexType.female;

            // Build the appropriate regex pattern based on whether it's male or female
            string genderSpecificPart = sex == Enums.SexType.male ? "mp_m_freemode_01" : "mp_f_freemode_01";
            string addonNameWithoutGender = addonName.Replace(genderSpecificPart, "").TrimStart('_');

            if (shouldSetProjectName)
            {
                MainWindow.AddonManager.ProjectName = addonNameWithoutGender;
            }

            var (yddFiles, ymtFile, yldFiles) = await Task.Run(() =>
            {
                string pattern = $@"^{genderSpecificPart}(_p)?.*?{Regex.Escape(addonNameWithoutGender)}\^";
                var compiledPattern = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

                var allFiles = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories);
                
                var ydds = allFiles
                    .Where(f => f.EndsWith(".ydd", StringComparison.OrdinalIgnoreCase))
                    .Where(f => compiledPattern.IsMatch(Path.GetFileName(f)))
                    .OrderBy(x =>
                    {
                        var number = FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(x));
                        return number ?? int.MaxValue;
                    })
                    .ThenBy(Path.GetFileName, StringComparer.Ordinal)
                    .ToArray();

                var ymt = allFiles
                    .Where(f => f.EndsWith(".ymt", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault(x => x.Contains(addonName));

                var ylds = allFiles
                    .Where(f => f.EndsWith(".yld", StringComparison.OrdinalIgnoreCase))
                    .Where(f => compiledPattern.IsMatch(Path.GetFileName(f)))
                    .OrderBy(x =>
                    {
                        var number = FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(x));
                        return number ?? int.MaxValue;
                    })
                    .ThenBy(Path.GetFileName, StringComparer.Ordinal)
                    .ToArray();

                return (ydds, ymt, ylds);
            });

            if (yddFiles.Length == 0)
            {
                CustomMessageBox.Show($"Для выбранного .meta не найдено .ydd ({Path.GetFileName(path)})", "Ошибка");
                return false;
            }

            if (ymtFile == null)
            {
                CustomMessageBox.Show($"Для выбранного .meta не найдено .ymt ({Path.GetFileName(path)})", "Ошибка");
                return false;
            }

            var ymt = new PedFile();
            var ymtBytes = await FileHelper.ReadAllBytesAsync(ymtFile);
            RpfFile.LoadResourceFile(ymt, ymtBytes, 2);
            
            var pedAltVariations = await LoadPedAlternativeVariationsFileAsync(dirPath, addonNameWithoutGender);
            
            //merge ydd with yld files
            var mergedFiles = yddFiles.Concat(yldFiles).ToArray();

            await AddDrawables(mergedFiles, sex, ymt, dirPath, pedAltVariations);
            return true;
        }

        public Task AddDrawables(
            string[] filePaths,
            Enums.SexType sex,
            PedFile ymt = null,
            string basePath = null,
            PedAlternativeVariations pedAltVariations = null,
            Addon targetAddon = null)
        {
            if (filePaths == null || filePaths.Length == 0)
            {
                return Task.CompletedTask;
            }

            EnsureDrawableQueueStarted();
            targetAddon = Addons.Contains(targetAddon) ? targetAddon : SelectedAddon;
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            // We need to count how many drawables of each type we have added so far
            // this is because if we are loading from ymt file, numbers are relative to this ymt file, once adding it to existing project
            // we need to adjust numbers to get proper properties
            Dictionary<(int, bool), int> typeNumericCounts = [];

            //read properties from provided ymt file if there is any
            Dictionary<(int, int), MCComponentInfo> compInfoDict = [];
            Dictionary<(int, int), MCPedPropMetaData> pedPropMetaDataDict = [];
            if (ymt is not null)
            {
                var hasCompInfos = ymt.VariationInfo.CompInfos != null;
                if (hasCompInfos)
                {
                    foreach (var compInfo in ymt.VariationInfo.CompInfos)
                    {
                        var key = (compInfo.ComponentType, compInfo.ComponentIndex);
                        compInfoDict[key] = compInfo;
                    }
                }

                var hasProps = ymt.VariationInfo.PropInfo.PropMetaData != null && ymt.VariationInfo.PropInfo.Data.numAvailProps > 0;
                if (hasProps)
                {
                    foreach (var pedPropMetaData in ymt.VariationInfo.PropInfo.PropMetaData)
                    {
                        var key = (pedPropMetaData.Data.anchorId, pedPropMetaData.Data.propId);
                        pedPropMetaDataDict[key] = pedPropMetaData;
                    }
                }
            }

            foreach (var filePath in filePaths)
            {
                var workItem = new DrawableWorkItem(
                    filePath,
                    sex,
                    basePath,
                    ymt,
                    pedAltVariations,
                    compInfoDict,
                    pedPropMetaDataDict,
                    typeNumericCounts,
                    targetAddon
                );
                _drawableQueue.Add(workItem);
            }

            _drawableQueue.Add(new CompletionMarker(tcs, targetAddon));
            return tcs.Task;
        }

        private async Task ProcessDrawableQueue()
        {
            var pendingDrawables = new List<GDrawable>();
            var pendingDrawableSourceNumbers = new Dictionary<GDrawable, int>();
            Exception? pendingBatchException = null;

            foreach (var workItem in _drawableQueue.GetConsumingEnumerable())
            {
                Addon? batchTarget = workItem switch
                {
                    DrawableWorkItem item => item.TargetAddon,
                    CompletionMarker marker => marker.TargetAddon,
                    _ => null
                };

                try
                {
                if (workItem is CompletionMarker marker)
                {
                    if (pendingBatchException != null)
                    {
                        marker.Tcs.TrySetException(pendingBatchException);
                        pendingBatchException = null;
                        pendingDrawables.Clear();
                        pendingDrawableSourceNumbers.Clear();
                        continue;
                    }

                    if (pendingDrawables.Count > 0)
                    {
                        await ProcessBatchDuplicatesAndAdd(pendingDrawables, marker.TargetAddon);
                        pendingDrawables.Clear();
                        pendingDrawableSourceNumbers.Clear();
                    }
                    
                    marker.Tcs.TrySetResult();
                    continue;
                }

                var (filePath, sex, basePath, ymt, pedAltVariations, compInfoDict, pedPropMetaDataDict, typeNumericCounts, _) = (DrawableWorkItem)workItem;

                var (isProp, drawableType) = await FileHelper.ResolveDrawableType(filePath);
                if (drawableType == -1)
                {
                    continue;
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (Addons.Count == 0)
                    {
                        CreateAddon();
                    }
                });

                var drawablesOfType = new List<GDrawable>();
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var addon in Addons)
                    {
                        drawablesOfType.AddRange(addon.Drawables.Where(x => x.TypeNumeric == drawableType && x.IsProp == isProp && x.Sex == sex));
                    }
                });

                if (AlternateRegex.IsMatch(filePath))
                {
                    if (filePath.EndsWith("_1.ydd", StringComparison.OrdinalIgnoreCase))
                    {
                        var number = FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(filePath));
                        if (number == null)
                        {
                            LogHelper.Log($"Не удалось найти связанный YDD-файл для файла от первого лица: {filePath}. Связь нужно задать вручную.", Views.LogType.Warning);
                            continue;
                        }

                        var foundDrawable = drawablesOfType.FirstOrDefault(x => x.Number == number);
                        if (foundDrawable == null)
                        {
                            foundDrawable = pendingDrawables.FirstOrDefault(x => 
                                x.TypeNumeric == drawableType && 
                                x.IsProp == isProp && 
                                x.Sex == sex && 
                                pendingDrawableSourceNumbers.TryGetValue(x, out var srcNum) && 
                                srcNum == number);
                        }
                        
                        if (foundDrawable != null)
                        {
                            if (IsExternalProject)
                            {
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => foundDrawable.FirstPersonPath = filePath);
                            }
                            else
                            {
                                try
                                {
                                    var firstPersonFileNameWithoutExtension = $"{foundDrawable.Id}_firstperson";
                                    var firstPersonRelativePath = await FileHelper.CopyToProjectAssetsWithReplaceAsync(filePath, firstPersonFileNameWithoutExtension);
                                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => foundDrawable.FirstPersonPath = firstPersonRelativePath);
                                }
                                catch (Exception ex)
                                {
                                    if (!IsExternalProject)
                                    {
                                        throw new IOException($"Не удалось скопировать файл от первого лица в ресурсы проекта: {ex.Message}", ex);
                                    }

                                    LogHelper.Log($"Не удалось скопировать файл от первого лица в ресурсы проекта: {ex.Message}. Используется исходный путь.", Views.LogType.Warning);
                                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => foundDrawable.FirstPersonPath = filePath);
                                }
                            }
                        }
                        else
                        {
                            LogHelper.Log($"Не удалось найти связанный YDD-файл для файла от первого лица: {filePath}. Связь нужно задать вручную.", Views.LogType.Warning);
                        }
                    }
                    continue;
                }

                if (PhysicsRegex.IsMatch(filePath))
                {
                    var number = FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(filePath));
                    if (number == null)
                    {
                        LogHelper.Log($"Не удалось найти связанный YDD-файл для этого YLD: {filePath}. Связь нужно задать вручную.", Views.LogType.Warning);
                        continue;
                    }

                    var foundDrawable = drawablesOfType.FirstOrDefault(x => x.Number == number);
                    if (foundDrawable == null)
                    {
                        foundDrawable = pendingDrawables.FirstOrDefault(x => 
                            x.TypeNumeric == drawableType && 
                            x.IsProp == isProp && 
                            x.Sex == sex && 
                            pendingDrawableSourceNumbers.TryGetValue(x, out var srcNum) && 
                            srcNum == number);
                    }
                    if (foundDrawable != null)
                    {
                        if (IsExternalProject)
                        {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => foundDrawable.ClothPhysicsPath = filePath);
                        }
                        else
                        {
                            try
                            {
                                var physicsFileNameWithoutExtension = $"{foundDrawable.Id}_cloth";
                                var physicsRelativePath = await FileHelper.CopyToProjectAssetsWithReplaceAsync(filePath, physicsFileNameWithoutExtension);
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => foundDrawable.ClothPhysicsPath = physicsRelativePath);
                            }
                            catch (Exception ex)
                            {
                                if (!IsExternalProject)
                                {
                                    throw new IOException($"Не удалось скопировать файл физики одежды в ресурсы проекта: {ex.Message}", ex);
                                }

                                LogHelper.Log($"Не удалось скопировать файл физики одежды в ресурсы проекта: {ex.Message}. Используется исходный путь.", Views.LogType.Warning);
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => foundDrawable.ClothPhysicsPath = filePath);
                            }
                        }
                    }
                    else
                    {
                        LogHelper.Log($"Не удалось найти связанный YDD-файл для этого YLD: {filePath}. Связь нужно задать вручную.", Views.LogType.Warning);
                    }
                    continue;
                }

                var drawable = await FileHelper.CreateDrawableAsync(filePath, sex, isProp, drawableType, 0); // Number is set by AddDrawable
                
                var sourceNumber = FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(filePath));
                if (sourceNumber.HasValue)
                {
                    pendingDrawableSourceNumbers[drawable] = sourceNumber.Value;
                }
                
                if (!string.IsNullOrEmpty(basePath) && filePath.StartsWith(basePath))
                {
                    var extractedGroup = ExtractGroupFromPath(filePath, basePath, sex, isProp);
                    if (SimplePathBuilder.TryNormalizeGroupPath(extractedGroup, out string normalizedGroup))
                    {
                        drawable.Group = normalizedGroup;
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (!Groups.Contains(normalizedGroup))
                            {
                                Groups.Add(normalizedGroup);
                                GroupManager.Instance.AddGroup(normalizedGroup);
                            }
                        });
                    }
                }

                if (ymt is not null)
                {
                    var key = (drawableType, isProp);
                    if (typeNumericCounts.TryGetValue(key, out int value))
                    {
                        typeNumericCounts[key] = ++value;
                    }
                    else
                    {
                        typeNumericCounts[key] = 1;
                    }

                    var ymtKey = (drawable.TypeNumeric, typeNumericCounts[(drawable.TypeNumeric, drawable.IsProp)] - 1);
                    if (compInfoDict.TryGetValue(ymtKey, out MCComponentInfo compInfo))
                    {
                        var list = EnumHelper.GetFlags((int)compInfo.Data.flags);
                        drawable.Audio = compInfo.Data.pedXml_audioID.ToString();
                        drawable.SelectedFlags = list.ToObservableCollection();
                        if (compInfo.Data.pedXml_expressionMods.f4 != 0)
                        {
                            drawable.EnableHighHeels = true;
                            drawable.HighHeelsValue = compInfo.Data.pedXml_expressionMods.f4;
                        }
                    }

                    if (drawable.IsProp && pedPropMetaDataDict.TryGetValue(ymtKey, out MCPedPropMetaData pedPropMetaData))
                    {
                        drawable.Audio = pedPropMetaData.Data.audioId.ToString();
                        drawable.RenderFlag = pedPropMetaData.Data.renderFlags.ToString();
                        var list = EnumHelper.GetFlags((int)pedPropMetaData.Data.propFlags);
                        drawable.SelectedFlags = list.ToObservableCollection();
                        if (pedPropMetaData.Data.expressionMods.f0 != 0)
                        {
                            drawable.EnableHairScale = true;
                            drawable.HairScaleValue = Math.Abs(pedPropMetaData.Data.expressionMods.f0);
                        }
                    }
                }

                if (pedAltVariations != null && !drawable.IsProp)
                {
                    string pedName = sex == Enums.SexType.male ? "mp_m_freemode_01" : "mp_f_freemode_01";
                    var pedVariation = pedAltVariations.Peds.FirstOrDefault(p => p.Name == pedName);
                    if (pedVariation != null)
                    {
                        foreach (var alternateSwitch in pedVariation.Switches)
                        {
                            var matchingAsset = alternateSwitch.SourceAssets.FirstOrDefault(asset =>
                                asset.Component == drawable.TypeNumeric && asset.Index == drawable.Number);
                            if (matchingAsset != null)
                            {
                                drawable.HidesHair = true;
                                break;
                            }
                        }
                    }
                }

                pendingDrawables.Add(drawable);
                }
                catch (Exception ex)
                {
                    pendingBatchException ??= ex;
                    LogHelper.Log($"Ошибка обработки одежды: {ex.Message}", Views.LogType.Error);
                    ErrorLogHelper.LogError("Ошибка обработки элемента очереди одежды", ex);

                    if (pendingDrawables.Count > 0)
                    {
                        try
                        {
                            await ProcessBatchDuplicatesAndAdd(pendingDrawables, batchTarget);
                        }
                        catch (Exception batchException)
                        {
                            LogHelper.Log($"Ошибка пакетной обработки одежды: {batchException.Message}", Views.LogType.Error);
                        }
                    }

                    pendingDrawables.Clear();
                    pendingDrawableSourceNumbers.Clear();

                    if (workItem is CompletionMarker failedMarker)
                    {
                        failedMarker.Tcs.TrySetException(ex);
                    }
                }
            }
        }

        private async Task ProcessBatchDuplicatesAndAdd(List<GDrawable> drawables, Addon targetAddon)
        {
            var duplicatesDict = DuplicateDetector.CheckDrawableDuplicatesBatch(drawables);
            
            var drawablesWithDuplicates = drawables.Where(d => duplicatesDict.ContainsKey(d)).ToList();
            var drawablesWithoutDuplicates = drawables.Where(d => !duplicatesDict.ContainsKey(d)).ToList();
            
            foreach (var drawable in drawablesWithoutDuplicates)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AddDrawableInternal(drawable, targetAddon);
                });
            }
            
            if (drawablesWithDuplicates.Count > 0)
            {
                var batchItems = drawablesWithDuplicates
                    .Select(d => new DuplicateBatchItem
                    {
                        Drawable = d,
                        ExistingDuplicates = duplicatesDict[d]
                    })
                    .ToList();

                DuplicateBatchResult result = null;
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    result = DuplicateBatchDialog.Show(batchItems);
                });

                if (result != null && !result.Cancelled)
                {
                    foreach (var drawable in result.DrawablesToAdd)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            AddDrawableInternal(drawable, targetAddon);
                        });
                    }

                    foreach (var drawable in result.DrawablesToSkip)
                    {
                        LogHelper.Log($"Элемент одежды «{Path.GetFileName(drawable.FilePath)}» не добавлен (пользователь пропустил дубликат)", Views.LogType.Info);
                    }
                }
                else if (result?.Cancelled == true)
                {
                    foreach (var drawable in drawablesWithDuplicates)
                    {
                        LogHelper.Log($"Элемент одежды «{Path.GetFileName(drawable.FilePath)}» не добавлен (пакетная обработка дубликатов отменена)", Views.LogType.Info);
                    }
                }
            }

            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Addons.Sort(true);
            });
        }


        /// <summary>
        /// Extracts group path from file path relative to base path
        /// Expected structure for FiveM: basePath/stream/[gender]/[group]/[type]/file.ydd
        /// </summary>
        private static string ExtractGroupFromPath(string filePath, string basePath, Enums.SexType sex, bool isProp)
        {
            try
            {
                filePath = Path.GetFullPath(filePath);
                basePath = Path.GetFullPath(basePath);

                var relativePath = Path.GetRelativePath(basePath, Path.GetDirectoryName(filePath));
                
                var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                
                // Expected structure: stream/[gender]/[group(s)]/[type]
                if (parts.Length > 3)
                {
                    int genderIndex = -1;
                    string expectedGenderFolder = sex == Enums.SexType.male ? "[male]" : "[female]";
                    
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i].Equals(expectedGenderFolder, StringComparison.OrdinalIgnoreCase))
                        {
                            genderIndex = i;
                            break;
                        }
                    }
                    
                    if (genderIndex >= 0 && genderIndex < parts.Length - 2)
                    {
                        var groupParts = parts.Skip(genderIndex + 1).Take(parts.Length - genderIndex - 2).ToArray();
                        if (groupParts.Length > 0)
                        {
                            return string.Join("/", groupParts);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Ошибка извлечения группы из пути: {ex.Message}", Views.LogType.Warning);
            }
            
            return null;
        }

        public void AddDrawable(GDrawable drawable)
        {
            lock (AddonsLock)
            {
                var existingDuplicates = DuplicateDetector.CheckDrawableDuplicate(drawable);
                if (existingDuplicates != null && existingDuplicates.Count > 0)
                {
                    var duplicateNames = string.Join("\n", existingDuplicates.Select(d => $"  • {d.Name} (Аддон: {Addons.FirstOrDefault(a => a.Drawables.Contains(d))?.Name ?? "Неизвестно"})"));
                    var message = $"Обнаружен дубликат элемента одежды!\n\nДобавляемый элемент одежды совпадает со следующими элементами:\n{duplicateNames}\n\nНовый элемент будет добавлен, но помечен как дубликат.\n\nПродолжить?";
                    
                    var result = System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        Controls.CustomMessageBox.Show(message, "Обнаружен дубликат одежды", 
                            Controls.CustomMessageBox.CustomMessageBoxButtons.OKCancel, 
                            Controls.CustomMessageBox.CustomMessageBoxIcon.Warning));
                    
                    if (result == Controls.CustomMessageBox.CustomMessageBoxResult.Cancel)
                    {
                        LogHelper.Log($"Элемент одежды «{Path.GetFileName(drawable.FilePath)}» не добавлен (обработка дубликата отменена)", Views.LogType.Info);
                        return;
                    }
                }

                AddDrawableInternal(drawable, SelectedAddon);
            }
        }

        /// <summary>
        /// Internal method to add a drawable without duplicate checking.
        /// Used by batch processing after duplicates have already been handled.
        /// </summary>
        private void AddDrawableInternal(GDrawable drawable, Addon targetAddon)
        {
            lock (AddonsLock)
            {
                int nextNumber = 0;
                int currentAddonIndex = targetAddon != null ? Addons.IndexOf(targetAddon) : 0;
                if (currentAddonIndex < 0)
                {
                    currentAddonIndex = 0;
                }
                Addon currentAddon;

                // find to which addon we should add the drawable
                while (currentAddonIndex < Addons.Count)
                {
                    currentAddon = Addons[currentAddonIndex];
                    int countOfType = currentAddon.Drawables.Count(x => x.TypeNumeric == drawable.TypeNumeric && x.IsProp == drawable.IsProp && x.Sex == drawable.Sex);

                    // If the number of drawables of this type has reached 128, move to the next addon
                    if (countOfType >= GlobalConstants.MAX_DRAWABLES_IN_ADDON)
                    {
                        currentAddonIndex++;
                        continue;
                    }

                    nextNumber = countOfType;
                    break;
                }

                // make sure we are adding to correct addon
                if (currentAddonIndex < Addons.Count)
                {
                    currentAddon = Addons[currentAddonIndex];
                }
                else
                {
                    // Create a new Addon
                    currentAddon = new Addon("Аддон " + (currentAddonIndex + 1));
                    Addons.Add(currentAddon);
                    RebuildMoveMenuItems();
                }

                // Update name and number
                // mark as new, to make it easier to find
                drawable.IsNew = true;
                drawable.Number = nextNumber;
                drawable.SetDrawableName();

                currentAddon.Drawables.Add(drawable);

                DuplicateDetector.RegisterDrawable(drawable);
                SaveHelper.SetUnsavedChanges(true);
            }
        }

        public void DeleteDrawables(List<GDrawable> drawables)
        {
            var addon = SelectedAddon;
            if (addon == null || drawables == null || drawables.Count == 0)
            {
                return;
            }

            SaveHelper.SetUnsavedChanges(true);
            foreach (GDrawable drawable in drawables)
            {
                DuplicateDetector.UnregisterDrawable(drawable);
                addon.SelectedDrawables.Remove(drawable);
                addon.Drawables.Remove(drawable);

                if (SettingsHelper.Instance.AutoDeleteFiles)
                {
                    try
                    {
                        foreach (var texture in drawable.Textures)
                        {
                            DeleteManagedFileIfUnused(texture.FullFilePath);
                        }

                        DeleteManagedFileIfUnused(drawable.FullFilePath);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Log($"Не удалось удалить файлы одежды '{drawable.Name}': {ex.Message}", Views.LogType.Warning);
                    }
                }
            }

            addon.SelectedDrawable = addon.Drawables.FirstOrDefault();
            addon.SelectedTexture = addon.SelectedDrawable?.Textures.FirstOrDefault();

            // if addon is empty, remove it
            if (addon.Drawables.Count == 0)
            {
                DeleteAddon(addon);
                return;
            }

            addon.Drawables.Sort(true);
        }

        public void MoveDrawable(GDrawable drawable, Addon targetAddon)
        {
            if (drawable == null || targetAddon == null)
            {
                var nullParam = drawable == null ? nameof(drawable) : nameof(targetAddon);
                throw new ArgumentNullException(nullParam, $"{nullParam} cannot be null.");
            }

            var currentAddon = Addons.FirstOrDefault(a => a.Drawables.Contains(drawable));
            if (currentAddon != null && !ReferenceEquals(currentAddon, targetAddon))
            {
                bool wasSelected = currentAddon.SelectedDrawables?.Contains(drawable) == true;
                currentAddon.Drawables.Remove(drawable);
                currentAddon.SelectedDrawables?.Remove(drawable);

                drawable.Number = targetAddon.GetNextDrawableNumber(drawable.TypeNumeric, drawable.IsProp, drawable.Sex);
                drawable.SetDrawableName();

                targetAddon.Drawables.Add(drawable);
                if (wasSelected)
                {
                    targetAddon.SelectedDrawables ??= [];
                    targetAddon.SelectedDrawables.Add(drawable);
                    targetAddon.SelectedDrawable = drawable;
                    targetAddon.SelectedTexture = drawable.Textures?.FirstOrDefault();
                    SelectedAddon = targetAddon;
                }

                RenumberAddon(currentAddon);
                RenumberAddon(targetAddon);
                if (currentAddon.Drawables.Count == 0)
                {
                    currentAddon.SelectedDrawable = null;
                    currentAddon.SelectedTexture = null;
                }
                SaveHelper.SetUnsavedChanges(true);
            }
        }

        private static void RenumberAddon(Addon addon)
        {
            var counters = new Dictionary<(int, bool, Enums.SexType), int>();
            foreach (var drawable in addon.Drawables.OrderBy(item => item.Number))
            {
                var key = (drawable.TypeNumeric, drawable.IsProp, drawable.Sex);
                counters.TryGetValue(key, out int number);
                counters[key] = ++number;
                drawable.Number = number - 1;
                drawable.SetDrawableName();
            }
        }

        public void DeleteManagedFileIfUnused(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return;
            }

            string assetsRoot = Path.GetFullPath(FileHelper.GetProjectAssetsPath())
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(filePath);
            bool isManagedAsset = fullPath.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase);
            bool isReferenced = Addons
                .SelectMany(item => item.Drawables)
                .Any(item => string.Equals(item.FullFilePath, fullPath, StringComparison.OrdinalIgnoreCase) ||
                             item.Textures.Any(texture => string.Equals(texture.FullFilePath, fullPath, StringComparison.OrdinalIgnoreCase)));

            if (isManagedAsset && !isReferenced)
            {
                File.Delete(filePath);
            }
        }

        public int GetTotalDrawableAndTextureCount()
        {
            return Addons.Sum(addon => addon.GetTotalDrawableAndTextureCount());
        }

        private void DeleteAddon(Addon addon)
        {
            if (Addons.Count <= 1)
            {
                return;
            }

            int index = Addons.IndexOf(addon);
            if (index < 0) { return; } // if not found, don't remove

            Addons.RemoveAt(index);
            if (ReferenceEquals(SelectedAddon, addon))
            {
                SelectedAddon = Addons.ElementAtOrDefault(Math.Min(index, Addons.Count - 1));
            }
            AdjustAddonNames();
        }

        private void AdjustAddonNames()
        {
            for (int i = 0; i < Addons.Count; i++)
            {
                Addons[i].Name = $"Аддон {i + 1}";
            }

            RebuildMoveMenuItems();
            OnPropertyChanged("Addons");
        }

        public void RebuildMoveMenuItems()
        {
            MoveMenuItems.Clear();
            foreach (Addon addon in Addons)
            {
                MoveMenuItems.Add(new MoveMenuItem { Header = addon.Name, IsEnabled = true });
            }
        }


        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
