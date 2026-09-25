using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using CodeWalker.GameFiles;
using CodeWalker.Properties;
using CodeWalker.Rendering;
using CodeWalker.Utils;
using CodeWalker.World;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.XInput;
using Color = SharpDX.Color;

namespace CodeWalker
{
    public partial class CustomPedsForm : Form, DXForm
    {
        public Form Form { get { return this; } } //for DXForm/DXManager use

        public Renderer Renderer = null;
        public object RenderSyncRoot { get { return Renderer.RenderSyncRoot; } }

        public volatile bool formopen = false;

        public bool isLoading = true;
        volatile bool running = false;
        volatile bool pauserendering = false;
        //volatile bool initialised = false;

        Stopwatch frametimer = new Stopwatch();
        Camera camera;
        Timecycle timecycle;
        Weather weather;
        Clouds clouds;

        Entity camEntity = new Entity();


        bool MouseLButtonDown = false;
        bool MouseRButtonDown = false;
        int MouseX;
        int MouseY;
        System.Drawing.Point MouseDownPoint;
        System.Drawing.Point MouseLastPoint;

        public GameFileCache GameFileCache { get; } = new GameFileCache(Settings.Default.CacheSize, Settings.Default.CacheTime, GTAFolder.CurrentGTAFolder, Settings.Default.DLC, false, "levels;anim;audio;data;");


        InputManager Input = new InputManager();


        bool initedOk = false;



        bool toolsPanelResizing = false;
        int toolsPanelResizeStartX = 0;
        int toolsPanelResizeStartLeft = 0;
        int toolsPanelResizeStartRight = 0;


        public string PedModel = "mp_f_freemode_01";
        Ped SelectedPed = new Ped();
        public Dictionary<string, Drawable> SavedDrawables = new Dictionary<string, Drawable>();
        public Dictionary<string, Drawable> LoadedDrawables = new Dictionary<string, Drawable>();
        public Dictionary<Drawable, TextureDictionary> LoadedTextures = new Dictionary<Drawable, TextureDictionary>();
        public Dictionary<Drawable, TextureDictionary> SavedTextures = new Dictionary<Drawable, TextureDictionary>();

        string liveTexturePath = null;
        DateTime liveTextureLastWriteTime;
        Texture LiveTexture = new Texture();

        List<List<ComponentComboItem>> ComponentComboBoxes;
        private Dictionary<string, List<VertexTypePC>> floorVerticesDict = new Dictionary<string, List<VertexTypePC>>();
        private readonly Vector3[] floorVertices = new Vector3[]
        {
            new Vector3(-1.0f, 1.0f, -1.0f),
            new Vector3(1.0f, -1.0f, -1.0f),
            new Vector3(1.0f, 1.0f,  -1.0f),

            new Vector3(-1.0f, 1.0f, -1.0f),
            new Vector3(-1.0f, -1.0f, -1.0f),
            new Vector3(1.0f, -1.0f, -1.0f),

            new Vector3(-1.0f, 1.0f, -1.0f),
            new Vector3(1.0f, 1.0f,  -1.0f),
            new Vector3(1.0f, -1.0f, -1.0f),

            new Vector3(-1.0f, 1.0f, -1.0f),
            new Vector3(1.0f, -1.0f, -1.0f),
            new Vector3(-1.0f, -1.0f, -1.0f)
        };

        private bool highheelvaluechanged = false;
        private bool renderOnlySelected = false;

        private List<string> CustomAnimationPaths = new List<string>();
        private Dictionary<string, YcdFile> CustomAnimations = new Dictionary<string, YcdFile>();
        private bool _suppressClipDictEvent = false;


        private System.Windows.Forms.Timer autoRotateTimer;
        private float autoRotateAngle = 0f;
        private const float AutoRotateSpeed = 1.0f;
        private const int ExportImageSize = 768;
        private const int UpprComponentIndex = 3;
        private const int LowrComponentIndex = 4;
        private const int JbibComponentIndex = 11;
        private const float DefaultVisibleFill = 0.86f;
        private const float TorsoVisibleFill = 0.78f;
        private const float LowrVisibleFill = 0.82f;
        private const float LowrVerticalOffset = 0.03f;
        private const float DefaultExportCameraPadding = 1.35f;
        private const float TorsoExportCameraPadding = 1.75f;
        private const float LowrExportCameraPadding = 1.65f;

        private volatile bool _inputsUpdatePending;
        private Throttler throttler;
        private string _activePresetName = null;

        public class ComponentComboItem
        {
            public MCPVDrawblData DrawableData { get; set; }
            public int AlternativeIndex { get; set; }
            public int TextureIndex { get; set; }
            public ComponentComboItem(MCPVDrawblData drawableData, int altIndex = 0, int textureIndex = -1)
            {
                DrawableData = drawableData;
                AlternativeIndex = altIndex;
                TextureIndex = textureIndex;
            }
            public override string ToString()
            {
                if (DrawableData == null) return TextureIndex.ToString();
                var itemname = DrawableData.GetDrawableName(AlternativeIndex);
                if (DrawableData.TexData?.Length > 0) return itemname + " + " + DrawableData.GetTextureSuffix(TextureIndex);
                return itemname;
            }
            public string DrawableName
            {
                get
                {
                    return DrawableData?.GetDrawableName(AlternativeIndex) ?? "error";
                }
            }
            public string TextureName
            {
                get
                {
                    return DrawableData?.GetTextureName(TextureIndex);
                }
            }
        }

        public CustomPedsForm()
        {

            InitializeComponent();
            AddExportPreviewButton();

            ComponentComboBoxes = new List<List<ComponentComboItem>>
            {
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
            };

            throttler = new Throttler(1000);

            Renderer = new Renderer(this, GameFileCache);
            camera = Renderer.camera;
            timecycle = Renderer.timecycle;
            weather = Renderer.weather;
            clouds = Renderer.clouds;

            initedOk = Renderer.Init();

            Renderer.controllightdir = !Settings.Default.Skydome;
            Renderer.rendercollisionmeshes = false;
            Renderer.renderclouds = false;
            Renderer.rendermoon = true;
            Renderer.renderskeletons = false;
            Renderer.SelectionFlagsTestAll = true;
            Renderer.swaphemisphere = true;

            Renderer.renderskydome = false;
            Renderer.renderhdtextures = true;

            autoRotateTimer = new System.Windows.Forms.Timer();
            autoRotateTimer.Interval = 16; // About 60 FPS
            autoRotateTimer.Tick += AutoRotateTimer_Tick;
        }

        private void AddExportPreviewButton()
        {
            if (ToolsPanel == null)
            {
                return;
            }

            if (ToolsPanel.Controls.Find("ExportPreviewButton", false).Length > 0)
            {
                return;
            }

            var exportPreviewButton = new System.Windows.Forms.Button();
            exportPreviewButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            exportPreviewButton.Location = new System.Drawing.Point(ToolsPanel.Width - 99, 3);
            exportPreviewButton.Name = "ExportPreviewButton";
            exportPreviewButton.Size = new System.Drawing.Size(93, 23);
            exportPreviewButton.TabIndex = 18;
            exportPreviewButton.Text = "Экспорт PNG";
            exportPreviewButton.UseVisualStyleBackColor = true;
            exportPreviewButton.Click += ExportPreviewButton_Click;

            ToolsPanel.Controls.Add(exportPreviewButton);
            exportPreviewButton.BringToFront();
        }

        private void ExportPreviewButton_Click(object sender, EventArgs e)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.AddExtension = true;
                saveFileDialog.DefaultExt = "png";
                saveFileDialog.FileName = "preview.png";
                saveFileDialog.Filter = "Изображение PNG (*.png)|*.png";
                saveFileDialog.OverwritePrompt = true;
                saveFileDialog.Title = "Экспорт предпросмотра в PNG";

                if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    ExportCurrentPreviewPng(saveFileDialog.FileName);
                    UpdateStatus("Предпросмотр экспортирован в PNG");
                }
                catch (Exception ex)
                {
                    LogError("Не удалось экспортировать предпросмотр в PNG: " + ex);
                    MessageBox.Show(this, "Не удалось экспортировать предпросмотр в PNG:\n" + ex.Message, "Экспорт PNG", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportCurrentPreviewPng(string filePath)
        {
            if (Renderer == null || Renderer.DXMan == null || Renderer.DXMan.device == null || Renderer.DXMan.context == null || Renderer.DXMan.backbuffer == null)
            {
                throw new InvalidOperationException("Предпросмотр 3D ещё не готов.");
            }

            var selectedDrawable = GetSelectedExportDrawable();
            var selectedTexture = GetSelectedExportTexture(selectedDrawable);
            if (selectedDrawable == null)
            {
                throw new InvalidOperationException("Нет выбранного элемента для экспорта.");
            }

            var selectedPedComponentIndex = GetComponentIndexFromDrawableName(selectedDrawable.Name);
            var forceSelectedDrawableIntoComponent = selectedPedComponentIndex >= 0;
            if (selectedPedComponentIndex < 0)
            {
                selectedPedComponentIndex = GetSelectedPedComponentIndex(selectedDrawable);
            }
            if (selectedPedComponentIndex < 0 && selectedTexture == null && liveTexturePath == null)
            {
                throw new InvalidOperationException("Для выбранного элемента нет текстуры.");
            }

            var dxman = Renderer.DXMan;
            var device = dxman.device;
            var context = dxman.context;
            var backbufferDesc = dxman.backbuffer.Description;
            var exportSize = ExportImageSize;
            Texture2D exportTexture = null;
            Texture2D depthTexture = null;
            Texture2D stagingTexture = null;
            RenderTargetView exportTargetView = null;
            DepthStencilView exportDepthView = null;

            lock (Renderer.RenderSyncRoot)
            {
                if (backbufferDesc.Width <= 0 || backbufferDesc.Height <= 0)
                {
                    throw new InvalidOperationException("Буфер предпросмотра 3D не содержит данных изображения.");
                }

                var cameraState = CaptureCameraState();
                PedComponentExportState componentExportState = null;
                try
                {
                    if (forceSelectedDrawableIntoComponent)
                    {
                        componentExportState = ApplyTemporaryExportPedComponent(selectedPedComponentIndex, selectedDrawable, selectedTexture);
                    }

                    var exportDesc = new Texture2DDescription
                    {
                        Width = exportSize,
                        Height = exportSize,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                        SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.RenderTarget,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = ResourceOptionFlags.None
                    };
                    exportTexture = new Texture2D(device, exportDesc);
                    exportTargetView = new RenderTargetView(device, exportTexture);

                    depthTexture = new Texture2D(device, new Texture2DDescription
                    {
                        Width = exportSize,
                        Height = exportSize,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = SharpDX.DXGI.Format.D32_Float,
                        SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                        Usage = ResourceUsage.Default,
                        BindFlags = BindFlags.DepthStencil,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = ResourceOptionFlags.None
                    });
                    exportDepthView = new DepthStencilView(device, depthTexture);

                    var viewport = new ViewportF(0.0f, 0.0f, exportSize, exportSize, 0.0f, 1.0f);
                    dxman.SetRenderTargetOverride(exportTargetView, exportDepthView, viewport);

                    var exportDrawable = forceSelectedDrawableIntoComponent
                        ? selectedDrawable
                        : (selectedPedComponentIndex >= 0 ? SelectedPed.Drawables[selectedPedComponentIndex] : selectedDrawable);
                    FrameExportCamera(exportDrawable ?? selectedDrawable, exportSize, selectedPedComponentIndex);
                    Renderer.BeginRender(context);
                    context.ClearRenderTargetView(exportTargetView, new Color(0, 0, 0, 0));
                    context.ClearDepthStencilView(exportDepthView, DepthStencilClearFlags.Depth, 0.0f, 0);

                    if (!RenderSelectedExportItemWithRetry(selectedDrawable, selectedTexture, selectedPedComponentIndex))
                    {
                        throw new InvalidOperationException("Выбранный элемент ещё не готов к отрисовке. Дождитесь его появления в предпросмотре и повторите экспорт.");
                    }

                    Renderer.RenderQueued();
                    Renderer.RenderFinalPass();
                    Renderer.EndRender();

                    var stagingDesc = exportDesc;
                    stagingDesc.BindFlags = BindFlags.None;
                    stagingDesc.CpuAccessFlags = CpuAccessFlags.Read;
                    stagingDesc.Usage = ResourceUsage.Staging;

                    stagingTexture = new Texture2D(device, stagingDesc);
                    context.CopyResource(exportTexture, stagingTexture);
                    SaveTextureToPng(context, stagingTexture, filePath, depthTexture, selectedPedComponentIndex);
                }
                finally
                {
                    dxman.ClearRenderTargetOverride();
                    RestoreCameraState(cameraState);
                    if (componentExportState != null)
                    {
                        componentExportState.Restore(SelectedPed);
                    }
                    if (stagingTexture != null)
                    {
                        stagingTexture.Dispose();
                    }
                    if (exportDepthView != null)
                    {
                        exportDepthView.Dispose();
                    }
                    if (depthTexture != null)
                    {
                        depthTexture.Dispose();
                    }
                    if (exportTargetView != null)
                    {
                        exportTargetView.Dispose();
                    }
                    if (exportTexture != null)
                    {
                        exportTexture.Dispose();
                    }
                }
            }
        }

        private bool RenderSelectedExportItemWithRetry(Drawable selectedDrawable, TextureDictionary selectedTexture, int selectedPedComponentIndex)
        {
            for (int i = 0; i < 8; i++)
            {
                if (RenderSelectedExportItem(selectedDrawable, selectedTexture, selectedPedComponentIndex))
                {
                    return true;
                }

                Renderer.RenderableCache.ContentThreadProc();
                Renderer.RenderableCache.RenderThreadSync();
            }

            return false;
        }

        private int GetSelectedPedComponentIndex(Drawable selectedDrawable)
        {
            if (selectedDrawable == null || SelectedPed?.Drawables == null)
            {
                return -1;
            }

            for (int i = 0; i < SelectedPed.Drawables.Length; i++)
            {
                var pedDrawable = SelectedPed.Drawables[i];
                if (pedDrawable == null)
                {
                    continue;
                }
                if (ReferenceEquals(pedDrawable, selectedDrawable) || pedDrawable.Name == selectedDrawable.Name)
                {
                    return i;
                }
            }

            return -1;
        }

        private static int GetComponentIndexFromDrawableName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return -1;
            }

            var lowerName = name.ToLowerInvariant();
            if (lowerName.StartsWith("p_head") || lowerName.Contains("_p_head") ||
                lowerName.StartsWith("p_eyes") || lowerName.Contains("_p_eyes"))
            {
                return -1;
            }

            string[] prefixes =
            {
                "head",
                "berd",
                "hair",
                "uppr",
                "lowr",
                "hand",
                "feet",
                "teef",
                "accs",
                "task",
                "decl",
                "jbib"
            };

            for (int i = 0; i < prefixes.Length; i++)
            {
                var prefix = prefixes[i];
                if (lowerName == prefix ||
                    lowerName.StartsWith(prefix + "_") ||
                    lowerName.Contains("_" + prefix + "_") ||
                    lowerName.EndsWith("_" + prefix))
                {
                    return i;
                }
            }

            return -1;
        }

        private PedComponentExportState ApplyTemporaryExportPedComponent(int componentIndex, Drawable selectedDrawable, TextureDictionary fallbackTextureDictionary)
        {
            var state = new PedComponentExportState(SelectedPed, componentIndex);
            var fallbackTexture = fallbackTextureDictionary?.Textures?.data_items?.FirstOrDefault();
            var textureName = fallbackTexture?.Name;

            if (!string.IsNullOrWhiteSpace(selectedDrawable?.Name) && !string.IsNullOrWhiteSpace(textureName))
            {
                SelectedPed.SetComponentDrawable(componentIndex, selectedDrawable.Name, textureName, GameFileCache);
            }

            var loadedComponentData = SelectedPed.Drawables[componentIndex]?.Name == selectedDrawable?.Name;
            if (selectedDrawable != null)
            {
                SelectedPed.Drawables[componentIndex] = selectedDrawable;
            }
            if (fallbackTexture != null)
            {
                SelectedPed.Textures[componentIndex] = fallbackTexture;
            }
            if (!string.IsNullOrWhiteSpace(selectedDrawable?.Name))
            {
                SelectedPed.DrawableNames[componentIndex] = selectedDrawable.Name;
            }
            if (!loadedComponentData)
            {
                SelectedPed.Clothes[componentIndex] = null;
                SelectedPed.Expressions[componentIndex] = null;
            }

            return state;
        }

        private class PedComponentExportState
        {
            private readonly int componentIndex;
            private readonly string drawableName;
            private readonly Drawable drawable;
            private readonly Texture texture;
            private readonly Expression expression;
            private readonly ClothInstance cloth;

            public PedComponentExportState(Ped ped, int componentIndex)
            {
                this.componentIndex = componentIndex;
                drawableName = ped.DrawableNames[componentIndex];
                drawable = ped.Drawables[componentIndex];
                texture = ped.Textures[componentIndex];
                expression = ped.Expressions[componentIndex];
                cloth = ped.Clothes[componentIndex];
            }

            public void Restore(Ped ped)
            {
                ped.DrawableNames[componentIndex] = drawableName;
                ped.Drawables[componentIndex] = drawable;
                ped.Textures[componentIndex] = texture;
                ped.Expressions[componentIndex] = expression;
                ped.Clothes[componentIndex] = cloth;
            }
        }

        private bool RenderSelectedExportItem(Drawable selectedDrawable, TextureDictionary selectedTexture, int selectedPedComponentIndex)
        {
            if (selectedPedComponentIndex >= 0)
            {
                return RenderSelectedPedComponent(selectedPedComponentIndex, selectedTexture);
            }

            return RenderSelectedItem(selectedDrawable, selectedTexture, false);
        }

        private bool RenderSelectedPedComponent(int componentIndex, TextureDictionary fallbackTextureDictionary, Drawable drawableOverride = null)
        {
            var selectedPedDrawable = SelectedPed.Drawables[componentIndex];
            var drawable = drawableOverride ?? selectedPedDrawable;
            if (drawable == null)
            {
                return false;
            }

            var useOverrideTexture = drawableOverride != null && !ReferenceEquals(drawableOverride, selectedPedDrawable);
            var texture = useOverrideTexture ? null : SelectedPed.Textures[componentIndex];
            if (texture == null && fallbackTextureDictionary?.Textures?.data_items?.Length > 0)
            {
                texture = fallbackTextureDictionary.Textures.data_items[0];
            }

            var ac = SelectedPed.AnimClip;
            if (ac != null)
            {
                ac.EnableRootMotion = SelectedPed.EnableRootMotion;
            }

            if (SelectedPed.Skeleton != null)
            {
                if (drawable.Skeleton == null)
                {
                    drawable.Skeleton = SelectedPed.Skeleton;
                }
                else if (drawable.Skeleton != SelectedPed.Skeleton)
                {
                    var dskel = drawable.Skeleton;
                    if (SelectedPed.Skeleton.Bones?.Items != null)
                    {
                        for (int b = 0; b < SelectedPed.Skeleton.Bones.Items.Length; b++)
                        {
                            var srcbone = SelectedPed.Skeleton.Bones.Items[b];
                            var dstbone = srcbone;
                            if (dskel.BonesMap.TryGetValue(srcbone.Tag, out dstbone))
                            {
                                if (srcbone == dstbone)
                                {
                                    break;
                                }
                                dskel.Bones.Items[dstbone.Index] = srcbone;
                                dskel.BonesMap[srcbone.Tag] = srcbone;
                            }
                        }
                        dskel.BonesSorted = SelectedPed.Skeleton.BonesSorted;
                    }
                }
            }

            var isProp = drawable.Name.StartsWith("p_");
            return Renderer.RenderDrawable(
                drawable,
                null,
                SelectedPed.RenderEntity,
                0,
                SelectedPed.Ytd?.TextureDict,
                texture,
                ac,
                SelectedPed.Clothes[componentIndex],
                SelectedPed.Expressions[componentIndex],
                isProp,
                true);
        }

        private Drawable GetSelectedExportDrawable()
        {
            var selectedDrawable = Renderer.SelDrawable;
            if (selectedDrawable != null)
            {
                return selectedDrawable;
            }

            if (LoadedDrawables.Count == 1)
            {
                return LoadedDrawables.Values.FirstOrDefault();
            }

            return null;
        }

        private TextureDictionary GetSelectedExportTexture(Drawable selectedDrawable)
        {
            if (selectedDrawable == null)
            {
                return null;
            }

            TextureDictionary textureDictionary;
            if (LoadedTextures.TryGetValue(selectedDrawable, out textureDictionary))
            {
                return textureDictionary;
            }
            if (SavedTextures.TryGetValue(selectedDrawable, out textureDictionary))
            {
                return textureDictionary;
            }
            if (LoadedDrawables.TryGetValue(selectedDrawable.Name, out var loadedDrawable) && LoadedTextures.TryGetValue(loadedDrawable, out textureDictionary))
            {
                return textureDictionary;
            }
            if (SavedDrawables.TryGetValue(selectedDrawable.Name, out var savedDrawable) && SavedTextures.TryGetValue(savedDrawable, out textureDictionary))
            {
                return textureDictionary;
            }

            return null;
        }

        private CameraExportState CaptureCameraState()
        {
            return new CameraExportState
            {
                FollowPosition = camera.FollowEntity.Position,
                TargetDistance = camera.TargetDistance,
                CurrentDistance = camera.CurrentDistance,
                TargetRotation = camera.TargetRotation,
                CurrentRotation = camera.CurrentRotation,
                Width = camera.Width,
                Height = camera.Height,
                AspectRatio = camera.AspectRatio,
                UpdateProj = camera.UpdateProj
            };
        }

        private void RestoreCameraState(CameraExportState state)
        {
            camera.FollowEntity.Position = state.FollowPosition;
            camera.TargetDistance = state.TargetDistance;
            camera.CurrentDistance = state.CurrentDistance;
            camera.TargetRotation = state.TargetRotation;
            camera.CurrentRotation = state.CurrentRotation;
            camera.Width = state.Width;
            camera.Height = state.Height;
            camera.AspectRatio = state.AspectRatio;
            camera.UpdateProj = true;
            camera.Update(0.0f);
            camera.UpdateProj = state.UpdateProj;
        }

        private void FrameExportCamera(Drawable drawable, int exportSize, int selectedPedComponentIndex)
        {
            var bounds = GetDrawableExportBounds(drawable);
            var radius = Math.Max(0.05f, bounds.Radius);
            var cameraPadding = GetExportCameraPadding(selectedPedComponentIndex);
            var distance = (float)(radius / Math.Tan(camera.FieldOfView * 0.5f)) * cameraPadding;

            camera.OnWindowResize(exportSize, exportSize);
            camera.FollowEntity.Position = bounds.Center;
            camera.TargetDistance = distance;
            camera.CurrentDistance = distance;
            var exportRotation = new Vector3((float)Math.PI, 0.2f, 0.0f);
            camera.TargetRotation = exportRotation;
            camera.CurrentRotation = exportRotation;
            camera.UpdateProj = true;
            camera.Update(0.0f);
        }

        private static DrawableExportBounds GetDrawableExportBounds(Drawable drawable)
        {
            var min = drawable.BoundingBoxMin;
            var max = drawable.BoundingBoxMax;
            var size = max - min;

            if (IsValidExportBounds(min, max, size))
            {
                return new DrawableExportBounds
                {
                    Center = (min + max) * 0.5f,
                    Radius = size.Length() * 0.5f
                };
            }

            return new DrawableExportBounds
            {
                Center = drawable.BoundingCenter,
                Radius = drawable.BoundingSphereRadius
            };
        }

        private static bool IsValidExportBounds(Vector3 min, Vector3 max, Vector3 size)
        {
            if (!IsFinite(min) || !IsFinite(max) || !IsFinite(size))
            {
                return false;
            }

            return size.X > 0.0001f && size.Y > 0.0001f && size.Z > 0.0001f;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.X) && IsFinite(value.Y) && IsFinite(value.Z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private struct DrawableExportBounds
        {
            public Vector3 Center;
            public float Radius;
        }

        private static float GetExportCameraPadding(int selectedPedComponentIndex)
        {
            if (selectedPedComponentIndex == LowrComponentIndex)
            {
                return LowrExportCameraPadding;
            }
            if (selectedPedComponentIndex == UpprComponentIndex || selectedPedComponentIndex == JbibComponentIndex)
            {
                return TorsoExportCameraPadding;
            }

            return DefaultExportCameraPadding;
        }

        private class CameraExportState
        {
            public Vector3 FollowPosition;
            public float TargetDistance;
            public float CurrentDistance;
            public Vector3 TargetRotation;
            public Vector3 CurrentRotation;
            public float Width;
            public float Height;
            public float AspectRatio;
            public bool UpdateProj;
        }

        private static void SaveTextureToPng(DeviceContext context, Texture2D texture, string filePath, Texture2D depthTexture = null, int selectedPedComponentIndex = -1)
        {
            DataStream dataStream = null;
            var textureDesc = texture.Description;
            var dataBox = context.MapSubresource(texture, 0, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out dataStream);

            try
            {
                var stride = textureDesc.Width * 4;
                var pixels = new byte[stride * textureDesc.Height];
                for (int y = 0; y < textureDesc.Height; y++)
                {
                    dataStream.Position = y * dataBox.RowPitch;
                    dataStream.Read(pixels, y * stride, stride);
                }

                if (!ApplyAlphaFromDepth(context, depthTexture, pixels, textureDesc.Width, textureDesc.Height))
                {
                    ApplyAlphaFromRenderedPixels(pixels);
                }
                FitVisiblePixelsToSquare(pixels, textureDesc.Width, textureDesc.Height, selectedPedComponentIndex);

                using (var imagingFactory = new SharpDX.WIC.ImagingFactory())
                using (var pixelStream = DataStream.Create(pixels, true, false))
                using (var bitmap = new SharpDX.WIC.Bitmap(
                    imagingFactory,
                    textureDesc.Width,
                    textureDesc.Height,
                    SharpDX.WIC.PixelFormat.Format32bppRGBA,
                    new DataRectangle(pixelStream.DataPointer, stride)))
                using (var outputStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                using (var bitmapEncoder = new SharpDX.WIC.PngBitmapEncoder(imagingFactory, outputStream))
                using (var bitmapFrameEncode = new SharpDX.WIC.BitmapFrameEncode(bitmapEncoder))
                {
                    bitmapFrameEncode.Initialize();
                    bitmapFrameEncode.SetSize(textureDesc.Width, textureDesc.Height);
                    var pixelFormat = SharpDX.WIC.PixelFormat.Format32bppRGBA;
                    bitmapFrameEncode.SetPixelFormat(ref pixelFormat);
                    bitmapFrameEncode.WriteSource(bitmap);
                    bitmapFrameEncode.Commit();
                    bitmapEncoder.Commit();
                }
            }
            finally
            {
                context.UnmapSubresource(texture, 0);
                if (dataStream != null)
                {
                    dataStream.Dispose();
                }
            }
        }

        private static bool ApplyAlphaFromDepth(DeviceContext context, Texture2D depthTexture, byte[] pixels, int width, int height)
        {
            if (depthTexture == null)
            {
                return false;
            }

            Texture2D depthStagingTexture = null;
            DataStream depthStream = null;
            try
            {
                var depthDesc = depthTexture.Description;
                depthDesc.BindFlags = BindFlags.None;
                depthDesc.CpuAccessFlags = CpuAccessFlags.Read;
                depthDesc.Usage = ResourceUsage.Staging;
                depthDesc.OptionFlags = ResourceOptionFlags.None;

                depthStagingTexture = new Texture2D(depthTexture.Device, depthDesc);
                context.CopyResource(depthTexture, depthStagingTexture);
                var depthBox = context.MapSubresource(depthStagingTexture, 0, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out depthStream);

                var hasVisiblePixels = false;
                for (int y = 0; y < height; y++)
                {
                    depthStream.Position = y * depthBox.RowPitch;
                    for (int x = 0; x < width; x++)
                    {
                        var depth = depthStream.Read<float>();
                        var alpha = depth > 0.000001f ? (byte)255 : (byte)0;
                        pixels[((y * width + x) * 4) + 3] = alpha;
                        hasVisiblePixels = hasVisiblePixels || alpha > 0;
                    }
                }

                context.UnmapSubresource(depthStagingTexture, 0);
                return hasVisiblePixels;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (depthStream != null)
                {
                    depthStream.Dispose();
                }
                if (depthStagingTexture != null)
                {
                    depthStagingTexture.Dispose();
                }
            }
        }

        private static void ApplyAlphaFromRenderedPixels(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                var maxRgb = Math.Max(pixels[i], Math.Max(pixels[i + 1], pixels[i + 2]));
                pixels[i + 3] = (byte)(maxRgb > 2 ? 255 : 0);
            }
        }

        private static void FitVisiblePixelsToSquare(byte[] pixels, int width, int height, int selectedPedComponentIndex)
        {
            var visibleFill = GetVisibleFill(selectedPedComponentIndex);
            var minX = width;
            var minY = height;
            var maxX = -1;
            var maxY = -1;

            FindVisiblePixelBounds(pixels, width, height, true, ref minX, ref minY, ref maxX, ref maxY);
            if (maxX < 0 || maxY < 0)
            {
                FindVisiblePixelBounds(pixels, width, height, false, ref minX, ref minY, ref maxX, ref maxY);
            }

            if (maxX < 0 || maxY < 0)
            {
                return;
            }

            var sourceWidth = maxX - minX + 1;
            var sourceHeight = maxY - minY + 1;
            var targetMaxSize = Math.Max(1, (int)Math.Round(Math.Min(width, height) * visibleFill));
            var scale = targetMaxSize / (float)Math.Max(sourceWidth, sourceHeight);
            var targetWidth = Math.Max(1, (int)Math.Round(sourceWidth * scale));
            var targetHeight = Math.Max(1, (int)Math.Round(sourceHeight * scale));
            var targetMinX = (width - targetWidth) / 2;
            var targetMinY = (height - targetHeight) / 2;
            if (selectedPedComponentIndex == LowrComponentIndex)
            {
                targetMinY += (int)(height * LowrVerticalOffset);
                targetMinY = Math.Max(0, Math.Min(height - targetHeight, targetMinY));
            }

            var fittedPixels = new byte[pixels.Length];
            for (int y = 0; y < targetHeight; y++)
            {
                var targetY = targetMinY + y;
                if (targetY < 0 || targetY >= height)
                {
                    continue;
                }

                var sourceY = minY + ((y + 0.5f) / scale) - 0.5f;
                for (int x = 0; x < targetWidth; x++)
                {
                    var targetX = targetMinX + x;
                    if (targetX < 0 || targetX >= width)
                    {
                        continue;
                    }

                    var sourceX = minX + ((x + 0.5f) / scale) - 0.5f;
                    var targetIndex = (targetY * width + targetX) * 4;
                    SampleBilinearPixel(pixels, fittedPixels, width, height, sourceX, sourceY, targetIndex);
                }
            }

            System.Buffer.BlockCopy(fittedPixels, 0, pixels, 0, pixels.Length);
        }

        private static float GetVisibleFill(int selectedPedComponentIndex)
        {
            if (selectedPedComponentIndex == LowrComponentIndex)
            {
                return LowrVisibleFill;
            }
            if (selectedPedComponentIndex == UpprComponentIndex || selectedPedComponentIndex == JbibComponentIndex)
            {
                return TorsoVisibleFill;
            }

            return DefaultVisibleFill;
        }

        private static void SampleBilinearPixel(byte[] sourcePixels, byte[] targetPixels, int width, int height, float sourceX, float sourceY, int targetIndex)
        {
            sourceX = Math.Max(0.0f, Math.Min(width - 1, sourceX));
            sourceY = Math.Max(0.0f, Math.Min(height - 1, sourceY));

            var x0 = (int)Math.Floor(sourceX);
            var y0 = (int)Math.Floor(sourceY);
            var x1 = Math.Min(width - 1, x0 + 1);
            var y1 = Math.Min(height - 1, y0 + 1);
            var tx = sourceX - x0;
            var ty = sourceY - y0;

            var i00 = (y0 * width + x0) * 4;
            var i10 = (y0 * width + x1) * 4;
            var i01 = (y1 * width + x0) * 4;
            var i11 = (y1 * width + x1) * 4;

            for (int i = 0; i < 4; i++)
            {
                var top = (sourcePixels[i00 + i] * (1.0f - tx)) + (sourcePixels[i10 + i] * tx);
                var bottom = (sourcePixels[i01 + i] * (1.0f - tx)) + (sourcePixels[i11 + i] * tx);
                var value = (top * (1.0f - ty)) + (bottom * ty);
                targetPixels[targetIndex + i] = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(value)));
            }
        }

        private static void FindVisiblePixelBounds(byte[] pixels, int width, int height, bool requireColor, ref int minX, ref int minY, ref int maxX, ref int maxY)
        {
            minX = width;
            minY = height;
            maxX = -1;
            maxY = -1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var index = (y * width + x) * 4;
                    var alpha = pixels[index + 3];
                    if (alpha == 0)
                    {
                        continue;
                    }

                    if (requireColor)
                    {
                        var maxRgb = Math.Max(pixels[index], Math.Max(pixels[index + 1], pixels[index + 2]));
                        if (maxRgb <= 4)
                        {
                            continue;
                        }
                    }

                    minX = Math.Min(minX, x);
                    minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        private static Guid GetWicPixelFormat(SharpDX.DXGI.Format format)
        {
            switch (format)
            {
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm:
                case SharpDX.DXGI.Format.R8G8B8A8_UNorm_SRgb:
                    return SharpDX.WIC.PixelFormat.Format32bppRGBA;
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                case SharpDX.DXGI.Format.B8G8R8A8_UNorm_SRgb:
                    return SharpDX.WIC.PixelFormat.Format32bppBGRA;
                default:
                    throw new NotSupportedException("Неподдерживаемый формат буфера предпросмотра: " + format);
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            UpdateModelsUI();
        }

        public void InitScene(Device device)
        {
            int width = ClientSize.Width;
            int height = ClientSize.Height;

            try
            {
                Renderer.DeviceCreated(device, width, height);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки шейдеров!\n" + ex.ToString());
                return;
            }


            SetDefaultCameraPosition();

            Renderer.shaders.deferred = false; //no point using this here yet
            Renderer.shaders.AnisotropicFiltering = true;
            Renderer.shaders.hdr = false;

            LoadSettings();


            formopen = true;
            var contentThread = new Thread(new ThreadStart(ContentThread));
            contentThread.IsBackground = true;
            contentThread.Start();

            frametimer.Start();

            floorVerticesDict = new Dictionary<string, List<VertexTypePC>>
            {
                { "DrawableFloor", CreateFloorVertices(new Color(50, 50, 50, 255)) },
                { "PreviewFloor", CreateFloorVertices(new Color(90, 90, 90, 255)) }
            };
        }

        private List<VertexTypePC> CreateFloorVertices(Color color)
        {
            var verticesList = new List<VertexTypePC>(floorVertices.Length);
            uint colorValue = (uint)color.ToRgba();

            foreach (var vertex in floorVertices)
            {
                verticesList.Add(new VertexTypePC
                {
                    Position = vertex,
                    Colour = colorValue
                });
            }

            return verticesList;
        }

        public void CleanupScene()
        {
            pauserendering = true;
            formopen = false;

            int count = 0;
            while (running && (count < 5000))
            {
                Thread.Sleep(1);
                count++;
            }

            Renderer.DeviceDestroyed();
        }

        public void RenderScene(DeviceContext context)
        {
            float elapsed = (float)frametimer.Elapsed.TotalSeconds;
            frametimer.Restart();

            if (pauserendering) return;

            try
            {
                GameFileCache.BeginFrame();

                if (!Monitor.TryEnter(Renderer.RenderSyncRoot, 50))
                { return; }

                try
                {
                    throttler.Throttle(() => UpdateCameraInputs());

                    UpdateControlInputs(elapsed);

                    Renderer.Update(elapsed, MouseLastPoint.X, MouseLastPoint.Y);

                    Renderer.BeginRender(context);

                    Renderer.RenderSkyAndClouds();

                    RenderSelectedItems();

                    RenderFloor();

                    Renderer.SelectedDrawable = null;

                    Renderer.RenderPed(SelectedPed);

                    Renderer.RenderQueued();

                    Renderer.RenderFinalPass();

                    Renderer.EndRender();

                    Monitor.Exit(Renderer.RenderSyncRoot);
                }
                catch
                {
                    if (Monitor.IsEntered(Renderer.RenderSyncRoot))
                    {
                        Monitor.Exit(Renderer.RenderSyncRoot);
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                pauserendering = true;
                UpdateStatus($"Ошибка отрисовки — предпросмотр приостановлен");
                LogError($"Сбой отрисовки в 3D-предпросмотре: {ex.Message}");
            }
        }

        private void RenderSelectedItems()
        {
            var loadedDrawables = LoadedDrawables.Values.ToList().Take(20);
            foreach(var drawable in loadedDrawables)
            {
                if (LoadedTextures.TryGetValue(drawable, out var texture))
                {
                    RenderSelectedItem(drawable, texture);
                }
            }

            if (SavedDrawables.Count > 0)
            {
                var savedDrawables = SavedDrawables.Values.ToList();
                foreach (var drawable in savedDrawables)
                {
                    if (LoadedDrawables.TryGetValue(drawable.Name, out var loadedDrawable) && loadedDrawable.Name.Equals(drawable.Name))
                    {
                        continue;
                    }
                    if (!SavedTextures.ContainsKey(drawable))
                    {
                        continue;
                    }

                    RenderSelectedItem(drawable, SavedTextures[drawable]);
                }
            }
        }
        private bool RenderSelectedItem(Drawable d, TextureDictionary t, bool requirePedRendered = true)
        {
            // dirty hack to render drawable only when all other drawables are rendered, it fixes issue that props sometimes are not rendered attached to the head
            if (requirePedRendered && Renderer.RenderedDrawablesDict.Count < 4) return false;

            var isProp = d.Name.StartsWith("p_");
            d.Owner = SelectedPed;

            if(d.Skeleton == null || d.Skeleton.Bones == null || d.Skeleton.Bones.Items.Length == 0)
            {
                d.Skeleton = SelectedPed.Skeleton.Clone();
            }

            if(liveTexturePath != null)
            {
                var files = Directory.GetFiles(liveTexturePath, "*.dds");
                if(files.Length > 0)
                {
                    var file = files[0];
                    var fileinfo = new FileInfo(file);
                    var lastwritetime = fileinfo.LastWriteTime;
                    
                    if (lastwritetime > liveTextureLastWriteTime)
                    {
                        liveTextureLastWriteTime = lastwritetime;

                        LiveTexture = DDSIO.GetTexture(File.ReadAllBytes(file));

                    }
                    return Renderer.RenderDrawable(d, null, SelectedPed.RenderEntity, 0, null, LiveTexture, SelectedPed.AnimClip, null, null, isProp, true);
                }
                return false;
            }

            if(t == null || t.Textures.data_items.Count() == 0)
            {
                return false;
            }

            return Renderer.RenderDrawable(d, null, SelectedPed.RenderEntity, 0, t, t.Textures.data_items[0], SelectedPed.AnimClip, null, null, isProp, true);
        }

        private void RenderFloor()
        {
            if (Renderer.renderfloor)
            {
                if (floorVerticesDict.TryGetValue("DrawableFloor", out var floorVerticesList))
                {
                    if (Renderer.SelDrawable != null && Renderer.SelDrawable.IsHighHeelsEnabled)
                    {
                        List<VertexTypePC> newFloorVerticesList = new List<VertexTypePC>();

                        if (highheelvaluechanged || newFloorVerticesList.Count == 0)
                        {
                            newFloorVerticesList.Clear();

                            foreach (var ver in floorVerticesList)
                            {
                                var newPosition = new Vector3(ver.Position.X, ver.Position.Y, ver.Position.Z - Renderer.SelDrawable.HighHeelsValue);
                                newFloorVerticesList.Add(new VertexTypePC
                                {
                                    Position = newPosition,
                                    Colour = ver.Colour
                                });
                            }

                            highheelvaluechanged = false;
                        }

                        Renderer.RenderTriangles(newFloorVerticesList);
                    }
                    else
                    {
                        Renderer.RenderTriangles(floorVerticesList);
                    }
                }
            }

            if (floorCheckbox.Checked)
            {
                if (floorVerticesDict.TryGetValue("PreviewFloor", out var previewFloorVerticesList))
                {
                    List<VertexTypePC> newPreviewFloorVerticesList = new List<VertexTypePC>();
                    float floorUpDownValue = (float)floorUpDown.Value / 10;

                    foreach (var ver in previewFloorVerticesList)
                    {
                        var newPosition = new Vector3(ver.Position.X, ver.Position.Y, ver.Position.Z - floorUpDownValue);
                        newPreviewFloorVerticesList.Add(new VertexTypePC
                        {
                            Position = newPosition,
                            Colour = ver.Colour
                        });
                    }

                    Renderer.RenderTriangles(newPreviewFloorVerticesList);
                }
            }
        }

        public void BuffersResized(int w, int h)
        {
            Renderer.BuffersResized(w, h);
        }
        public bool ConfirmQuit()
        {
            return true;
        }

        private void Init()
        {
            //called from PedForm_Load

            if (!initedOk)
            {
                Close();
                return;
            }

            MouseWheel += PedsForm_MouseWheel;

            if (!GTAFolder.UpdateGTAFolder(true))
            {
                Close();
                return;
            }

            ShaderParamNames[] texsamplers = RenderableGeometry.GetTextureSamplerList();
            foreach (var texsampler in texsamplers)
            {
                TextureSamplerComboBox.Items.Add(texsampler);
            }

            Input.Init();

            Renderer.Start();

            LoadCustomAnimationsFromFolder();
        }

        private void ContentThread()
        {
            try
            {
                //main content loading thread.
                running = true;

                UpdateStatus("Сканирование...");

                try
                {
                    GTA5Keys.LoadFromPath(GTAFolder.CurrentGTAFolder, Settings.Default.Key);
                }
                catch
                {
                    MessageBox.Show("Ключи не найдены! Это не должно было произойти.");
                    running = false;
                    return;
                }

                try
                {
                    GameFileCache.EnableDlc = false;
                    GameFileCache.EnableMods = false;
                    GameFileCache.LoadPeds = true;
                    GameFileCache.LoadVehicles = false;
                    GameFileCache.LoadAudio = false;
                    GameFileCache.LoadArchetypes = false;//to speed things up a little
                    GameFileCache.BuildExtendedJenkIndex = false;//to speed things up a little
                    GameFileCache.DoFullStringIndex = true;//to get all global text from DLC...
                    GameFileCache.Init(UpdateStatus, LogError);
                }
                catch (Exception ex)
                {
                    LogError("Не удалось инициализировать кэш игровых файлов: " + ex.Message);
                    LogError("Установка GTA V повреждена или неполна. Предпросмотр 3D отключён.");
                    UpdateStatus("Ошибка: файлы GTA V не найдены или повреждены. Предпросмотр 3D недоступен.");
                    running = false;
                    return;
                }

                UpdateGlobalPedsUI();


                LoadWorld();
                isLoading = false;
                Task.Run(() => {
                    while (formopen && !IsDisposed) //renderer content loop
                    {
                        bool rcItemsPending = Renderer.ContentThreadProc();
                        if (!rcItemsPending)
                        {
                            Thread.Sleep(1); //sleep if there's nothing to do
                        }
                    }
                });

                while (formopen && !IsDisposed) //main asset loop
                {
                    bool fcItemsPending = GameFileCache.ContentThreadProc();
                    if (!fcItemsPending)
                    {
                        Thread.Sleep(1); //sleep if there's nothing to do
                    }
                }

                GameFileCache.Clear();
                running = false;
            }
            catch (Exception ex)
            {
                LogError("Необработанное исключение в ContentThread: " + ex.Message);
                LogError("Стек вызовов: " + ex.StackTrace);
                running = false;
            }
        }

        private void LoadSettings()
        {
            var s = Settings.Default;
            WireframeCheckBox.Checked = false;
            HDRRenderingCheckBox.Checked = false;
            ShadowsCheckBox.Checked = true;

            EnableAnimationCheckBox.Checked = false;
            ClipComboBox.Enabled = false;
            ClipDictComboBox.Enabled = false;
            CustomAnimComboBox.Enabled = false;
            EnableRootMotionCheckBox.Enabled = false;
            PlaybackSpeedTrackBar.Enabled = false;

            RenderModeComboBox.SelectedIndex = Math.Max(RenderModeComboBox.FindString(s.RenderMode), 0);
            TextureSamplerComboBox.SelectedIndex = Math.Max(TextureSamplerComboBox.FindString(s.RenderTextureSampler), 0);
            TextureCoordsComboBox.SelectedIndex = Math.Max(TextureCoordsComboBox.FindString(s.RenderTextureSamplerCoord), 0);

            // Camera presets
            FillDataGridView();
            LoadLastUsedPreset();
        }
        private void LoadWorld()
        {
            try
            {
                UpdateStatus("Загрузка циклов времени...");
                timecycle.Init(GameFileCache, UpdateStatus);
                timecycle.SetTime(Renderer.timeofday);
            }
            catch (Exception ex)
            {
                UpdateStatus("Предупреждение: не удалось загрузить циклы времени");
                LogError($"Ошибка инициализации циклов времени: {ex.Message}");
            }

            try
            {
                UpdateStatus("Загрузка материалов...");
                BoundsMaterialTypes.Init(GameFileCache);
            }
            catch (Exception ex)
            {
                UpdateStatus("Предупреждение: не удалось загрузить материалы");
                LogError($"Ошибка инициализации материалов: {ex.Message}");
            }

            try
            {
                UpdateStatus("Загрузка погоды...");
                weather.Init(GameFileCache, UpdateStatus, timecycle);
            }
            catch (Exception ex)
            {
                UpdateStatus("Предупреждение: не удалось загрузить погоду");
                LogError($"Ошибка инициализации погоды: {ex.Message}");
            }

            try
            {
                UpdateStatus("Загрузка облаков...");
                clouds.Init(GameFileCache, UpdateStatus, weather);
            }
            catch (Exception ex)
            {
                UpdateStatus("Предупреждение: не удалось загрузить облака");
                LogError($"Ошибка инициализации облаков: {ex.Message}");
            }

        }

        private void UpdateStatus(string text)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => { UpdateStatus(text); }));
                }
                else
                {
                    StatusLabel.Text = text;
                }
            }
            catch { }
        }
        private void LogError(string text)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => { LogError(text); }));
                }
                else
                {
                    //TODO: error logging..
                    ConsoleTextBox.AppendText(text + "\r\n");
                    //StatusLabel.Text = text;
                    //MessageBox.Show(text);
                }
            }
            catch { }
        }

        private void UpdateMousePosition(MouseEventArgs e)
        {
            MouseX = e.X;
            MouseY = e.Y;
            MouseLastPoint = e.Location;
        }

        private void RotateCam(int dx, int dy)
        {
            camera.MouseRotate(dx, dy);
            ClearActivePreset();
        }

        private void MoveCameraToView(Vector3 pos, float rad)
        {
            //move the camera to a default place where the given sphere is fully visible.

            rad = Math.Max(0.01f, rad*0.1f);

            camera.FollowEntity.Position = pos;
            camera.TargetDistance = rad * 1.2f;
            camera.CurrentDistance = rad * 1.2f;

            camera.UpdateProj = true;

        }
        private void AddDrawableTreeNode(DrawableBase drawable, string name, bool check)
        {
            var tnode = TexturesTreeView.Nodes.Add(name);
            var dnode = ModelsTreeView.Nodes.Add(name);
            dnode.Tag = drawable;
            dnode.Checked = check;

            if (name.Contains("Selected"))
            {
                string drawableTypeName;
                string[] nameParts = name.Split(' ')[1].Trim('(', ')').Split('_');

                if (nameParts.Length <= 1 || nameParts[0] != "p")
                {
                    drawableTypeName = nameParts[0];
                    var sameName = ModelsTreeView.Nodes.Cast<TreeNode>().Where(n =>
                        n.Text.Contains(drawableTypeName) &&
                        !n.Text.Contains("Selected") &&
                        !n.Text.Contains("Saved")
                    ).ToList();
                    if (sameName.Count > 0)
                    {
                        foreach (var node in sameName)
                        {
                            node.Checked = !node.Checked;
                        }
                    }
                }
            }

            AddDrawableModelsTreeNodes(drawable.DrawableModels?.High, "Высокая детализация", true, dnode, tnode);
            AddDrawableModelsTreeNodes(drawable.DrawableModels?.Med, "Средняя детализация", false, dnode, tnode);
            AddDrawableModelsTreeNodes(drawable.DrawableModels?.Low, "Низкая детализация", false, dnode, tnode);
            AddDrawableModelsTreeNodes(drawable.DrawableModels?.VLow, "Очень низкая детализация", false, dnode, tnode);

        }
        private void AddDrawableModelsTreeNodes(DrawableModel[] models, string prefix, bool check, TreeNode parentDrawableNode = null, TreeNode parentTextureNode = null)
        {
            if (models == null) return;

            for (int mi = 0; mi < models.Length; mi++)
            {
                var tnc = (parentDrawableNode != null) ? parentDrawableNode.Nodes : ModelsTreeView.Nodes;

                var model = models[mi];
                string mprefix = prefix + " " + (mi + 1).ToString();
                var mnode = tnc.Add(mprefix + " " + model.ToString());
                mnode.Tag = model;
                mnode.Checked = check;

                var ttnc = (parentTextureNode != null) ? parentTextureNode.Nodes : TexturesTreeView.Nodes;
                var tmnode = ttnc.Add(mprefix + " " + model.ToString());
                tmnode.Tag = model;

                if (!check)
                {
                    Renderer.SelectionModelDrawFlags[model] = false;
                }

                if (model.Geometries == null) continue;

                foreach (var geom in model.Geometries)
                {
                    var gname = geom.ToString();
                    var gnode = mnode.Nodes.Add(gname);
                    gnode.Tag = geom;
                    gnode.Checked = true;// check;

                    var tgnode = tmnode.Nodes.Add(gname);
                    tgnode.Tag = geom;

                    if ((geom.Shader != null) && (geom.Shader.ParametersList != null) && (geom.Shader.ParametersList.Hashes != null))
                    {
                        var pl = geom.Shader.ParametersList;
                        var h = pl.Hashes;
                        var p = pl.Parameters;
                        for (int ip = 0; ip < h.Length; ip++)
                        {
                            var hash = pl.Hashes[ip];
                            var parm = pl.Parameters[ip];
                            var tex = parm.Data as TextureBase;
                            if (tex != null)
                            {
                                var t = tex as Texture;
                                var tstr = tex.Name.Trim();
                                if (t != null)
                                {
                                    tstr = string.Format("{0} ({1}x{2}, встроенная)", tex.Name, t.Width, t.Height);
                                }
                                var tnode = tgnode.Nodes.Add(hash.ToString().Trim() + ": " + tstr);
                                tnode.Tag = tex;
                            }
                        }
                        tgnode.Expand();
                    }

                }

                mnode.Expand();
                tmnode.Expand();
            }
        }
        private void UpdateSelectionDrawFlags(TreeNode node)
        {
            //update the selection draw flags depending on tag and checked/unchecked
            var drwbl = node.Tag as DrawableBase;
            var model = node.Tag as DrawableModel;
            var geom = node.Tag as DrawableGeometry;
            bool rem = node.Checked;
            lock (Renderer.RenderSyncRoot)
            {
                if (drwbl != null)
                {
                    if (rem)
                    {
                        if (Renderer.SelectionDrawableDrawFlags.ContainsKey(drwbl))
                        {
                            Renderer.SelectionDrawableDrawFlags.Remove(drwbl);
                        }
                    }
                    else
                    {
                        Renderer.SelectionDrawableDrawFlags[drwbl] = false;
                    }
                }
                if (model != null)
                {
                    if (rem)
                    {
                        if (Renderer.SelectionModelDrawFlags.ContainsKey(model))
                        {
                            Renderer.SelectionModelDrawFlags.Remove(model);
                        }
                    }
                    else
                    {
                        Renderer.SelectionModelDrawFlags[model] = false;
                    }
                }
                if (geom != null)
                {
                    if (rem)
                    {
                        if (Renderer.SelectionGeometryDrawFlags.ContainsKey(geom))
                        {
                            Renderer.SelectionGeometryDrawFlags.Remove(geom);
                        }
                    }
                    else
                    {
                        Renderer.SelectionGeometryDrawFlags[geom] = false;
                    }
                }
                //updateArchetypeStatus = true;
            }
        }

        private void UpdateGlobalPedsUI()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => { UpdateGlobalPedsUI(); }));
            }
            else
            {

                var index = PedModel == "mp_f_freemode_01" ? 0 : 1;

                PedNameComboBox.Items.Add("mp_f_freemode_01");
                PedNameComboBox.Items.Add("mp_m_freemode_01");
                PedNameComboBox.SelectedIndex = index;
            }

        }

        private void UpdateModelsUI()
        {
            Renderer.SelectionDrawableDrawFlags.Clear();
            Renderer.SelectionModelDrawFlags.Clear();
            Renderer.SelectionGeometryDrawFlags.Clear();
            ModelsTreeView.Nodes.Clear();
            ModelsTreeView.ShowRootLines = true;
            TexturesTreeView.Nodes.Clear();
            TexturesTreeView.ShowRootLines = true;

            if (SelectedPed == null) return;

            // The following could be combined into a single for loop with a conditional check inside
            if (!renderOnlySelected)
            {
                for (int i = 0; i < 12; i++)
                {
                    var drawable = SelectedPed.Drawables[i];
                    var drawablename = SelectedPed.DrawableNames[i];

                    if (drawable != null)
                    {
                        AddDrawableTreeNode(drawable, drawablename, true);
                    }
                }
            }
            else
            {
                // Disabling rendering for all drawables.
                // Code below this for loop will override this flag for the drawable(s) we want to render.
                for (int i = 0; i < 12; i++)
                {
                    var drawable = SelectedPed.Drawables[i];

                    if (drawable != null)
                    {
                        Renderer.SelectionDrawableDrawFlags[drawable] = false;
                    }
                }
            }

            foreach (var drawable in LoadedDrawables.Values)
            {
                AddDrawableTreeNode(drawable, $"Selected ({drawable.Name})", true);
            }

            if (SavedDrawables.Count > 0)
            {
                foreach (var drawable in SavedDrawables.Values)
                {
                    if (LoadedDrawables.Values.Contains(drawable)) continue;
                    AddDrawableTreeNode(drawable, $"Saved ({drawable.Name})", true);
                }
            }
        }

        public void LoadAnimsForModel(string pedname)
        {
            if(pedname == "mp_m_freemode_01")
            {
                ClipDictComboBox.Text = "move_m@generic";
            } 
            else if(pedname == "mp_f_freemode_01")
            {
                ClipDictComboBox.Text = "move_f@generic";
            }

            ClipComboBox.Text = "idle";
            LoadClipDict(ClipDictComboBox.Text);
            SelectClip("idle");
        }

        public void LoadPed(string pedname = "")
        {
            if (string.IsNullOrEmpty(pedname))
            {
                pedname = PedNameComboBox.Text;
            }
            PedNameComboBox.Text = pedname;

            var pedhash = JenkHash.GenHash(pedname.ToLowerInvariant());
            var pedchange = SelectedPed.NameHash != pedhash;

            SelectedPed.Init(pedname, GameFileCache);

            LoadModel(SelectedPed.Yft, pedchange);
            if(EnableAnimationCheckBox.Checked)
            {
                LoadAnimsForModel(pedname);
            }
            
            var vi = SelectedPed.Ymt?.VariationInfo;
            if (vi != null)
            {
                for (int i = 0; i < 12; i++)
                {
                    PopulateCompCombo(ComponentComboBoxes.ElementAt(i), vi.GetComponentData(i));
                }
            }

            head_updown.Maximum = ComponentComboBoxes[0].Count - 1;
            berd_updown.Maximum = ComponentComboBoxes[1].Count - 1;
            hair_updown.Maximum = ComponentComboBoxes[2].Count - 1;
            uppr_updown.Maximum = ComponentComboBoxes[3].Count - 1;
            lowr_updown.Maximum = ComponentComboBoxes[4].Count - 1;
            feet_updown.Maximum = ComponentComboBoxes[6].Count - 1;

            var index = PedNameComboBox.SelectedIndex;
            head_updown.Value = GetCompSettingsValue("HeadComp", index);
            berd_updown.Value = GetCompSettingsValue("BerdComp", index);
            hair_updown.Value = GetCompSettingsValue("HairComp", index);
            uppr_updown.Value = GetCompSettingsValue("UpprComp", index);
            lowr_updown.Value = GetCompSettingsValue("LowrComp", index);
            feet_updown.Value = GetCompSettingsValue("FeetComp", index);

            SetComponentDrawable(0, (int)head_updown.Value);
            SetComponentDrawable(1, (int)berd_updown.Value);
            SetComponentDrawable(2, (int)hair_updown.Value);
            SetComponentDrawable(3, (int)uppr_updown.Value);
            SetComponentDrawable(4, (int)lowr_updown.Value);
            SetComponentDrawable(6, (int)feet_updown.Value);


            UpdateModelsUI();
        }
       
        public int GetCompSettingsValue(string name, int index)
        {

            string[] settings = (Settings.Default[name] as string).Split(';');
            if (settings != null)
            {
                return Convert.ToInt32(settings[index]);
            }
            return 0;
        }

        public void UpdateSelectedDrawable(Drawable d, TextureDictionary t, Dictionary<string, string> updates)
        {
            Renderer.SelDrawable = d;
            Renderer.SelectedDrawableChanged = true;

            UpdatePolygonVertexCountLabels();

            foreach (var update in updates)
            {
                var value = update.Value.ToString().ToLower();
                switch (update.Key)
                {
                    case "EnableKeepPreview":
                        var v = bool.Parse(value);
                        //if loadeddrawables already contains drawable, remove it
                        if (v == true && !SavedDrawables.ContainsKey(d.Name))
                        {
                            SavedDrawables.Add(d.Name, d);
                            SavedTextures.Add(d, t);
                            UpdateModelsUI();
                        }
                        else if (v == false && SavedDrawables.ContainsKey(d.Name))
                        {
                            SavedDrawables.Remove(d.Name);
                            SavedTextures.Remove(d);
                            UpdateModelsUI();
                        }
                        break;
                    case "EnableHairScale":
                        Renderer.SelDrawable.IsHairScaleEnabled = bool.Parse(value);
                        break;
                    case "HairScaleValue":
                        Renderer.SelDrawable.HairScaleValue = Convert.ToSingle(value);
                        break;
                    case "EnableHighHeels":
                        Renderer.SelDrawable.IsHighHeelsEnabled = bool.Parse(value);
                        break;
                    case "HighHeelsValue":
                        Renderer.SelDrawable.HighHeelsValue = Convert.ToSingle(value) / 10;
                        highheelvaluechanged = true;
                        break;
                    case "GenderChanged":
                        LoadPed(PedModel);
                        break;
                    default:
                        break;
                }
            }
        }

        private void UpdatePolygonVertexCountLabels()
        {
            if (Renderer.SelDrawable != null)
            {
                int polycount = 0;
                int vertcount = 0;
                foreach (var model in Renderer.SelDrawable.DrawableModels.High)
                {
                    if (model.Geometries != null)
                    {
                        foreach (var geom in model.Geometries)
                        {
                            polycount += (int)(geom.IndicesCount / 3);
                            vertcount += geom.VerticesCount;
                        }
                    }
                }
                PolygonCountText.Text = polycount.ToString();
                VertexCountText.Text = vertcount.ToString();
            }
            else
            {
                PolygonCountText.Text = "0";
                VertexCountText.Text = "0";
            }
        }

        public void LoadModel(YftFile yft, bool movecamera = true)
        {
            if (yft == null) return;

            //FileName = yft.Name;
            //Yft = yft;

            var dr = yft.Fragment?.Drawable;
            // Don't move camera if we have an active preset
            if (movecamera && (dr != null) && _activePresetName == null)
            {
                MoveCameraToView(dr.BoundingCenter, dr.BoundingSphereRadius);
            }

            //UpdateModelsUI(yft.Fragment.Drawable);
        }

        private void PopulateCompCombo(List<ComponentComboItem> c, MCPVComponentData compData)
        {
            if (compData?.DrawblData3 == null) return;
            foreach (var item in compData.DrawblData3)
            {
                c.Add(new ComponentComboItem(item));
            }
        }

        private void SetComponentDrawable(int compIndex, int itemIndex)
        {
            var s = ComponentComboBoxes[compIndex][itemIndex];

            SelectedPed.SetComponentDrawable(compIndex, s.DrawableName, s.TextureName, GameFileCache);

            UpdateModelsUI();
        }

        private void LoadClipDict(string name)
        {
            var ycdhash = JenkHash.GenHash(name.ToLowerInvariant());
            var ycd = GameFileCache.GetYcd(ycdhash);
            while ((ycd != null) && (!ycd.Loaded))
            {
                Thread.Sleep(1);//kinda hacky
                ycd = GameFileCache.GetYcd(ycdhash);
            }

            SelectedPed.Ycd = ycd;

            ClipComboBox.Items.Clear();
            ClipComboBox.Items.Add("");

            if (ycd?.ClipMapEntries == null)
            {
                ClipComboBox.SelectedIndex = 0;
                SelectedPed.AnimClip = null;
                return;
            }

            List<string> items = new List<string>();
            foreach (var cme in ycd.ClipMapEntries)
            {
                if (cme.Clip != null)
                {
                    items.Add(cme.Clip.ShortName);
                }
            }

            items.Sort();
            foreach (var item in items)
            {
                ClipComboBox.Items.Add(item);
            }
        }

        private void SelectClip(string name)
        {
            MetaHash cliphash = JenkHash.GenHash(name);
            ClipMapEntry cme = null;
            SelectedPed.Ycd?.ClipMap?.TryGetValue(cliphash, out cme);
            SelectedPed.AnimClip = cme;

            PlaybackSpeedTrackBar.Value = 60;
            UpdatePlaybackSpeedLabel();
        }
        private void UpdateTimeOfDayLabel()
        {
            int v = TimeOfDayTrackBar.Value;
            float fh = v / 60.0f;
            int ih = (int)fh;
            int im = v - (ih * 60);
            if (ih == 24) ih = 0;
            TimeOfDayLabel.Text = string.Format("{0:00}:{1:00}", ih, im);
        }

        private void UpdateControlInputs(float elapsed)
        {
            if (elapsed > 0.1f) elapsed = 0.1f;

            var s = Settings.Default;

            float moveSpeed = 2.0f;


            Input.Update();

            if (Input.ShiftPressed)
            {
                moveSpeed *= 5.0f;
            }
            if (Input.CtrlPressed)
            {
                moveSpeed *= 0.2f;
            }

            Vector3 movevec = Input.KeyboardMoveVec(false);

            if (Input.xbenable)
            {
                movevec.X += Input.xblx;
                movevec.Z -= Input.xbly;
                moveSpeed *= (1.0f + (Math.Min(Math.Max(Input.xblt, 0.0f), 1.0f) * 15.0f)); //boost with left trigger
                if (Input.ControllerButtonPressed(GamepadButtonFlags.A | GamepadButtonFlags.RightShoulder | GamepadButtonFlags.LeftShoulder))
                {
                    moveSpeed *= 5.0f;
                }
            }
            {
                //normal movement
                movevec *= elapsed * moveSpeed * Math.Min(camera.TargetDistance, 50.0f);
            }


            Vector3 movewvec = camera.ViewInvQuaternion.Multiply(movevec);
            camEntity.Position += movewvec;

            if (Input.xbenable)
            {
                camera.ControllerRotate(Input.xbrx, Input.xbry, elapsed);

                float zoom = 0.0f;
                float zoomspd = s.XInputZoomSpeed;
                float zoomamt = zoomspd * elapsed;
                if (Input.ControllerButtonPressed(GamepadButtonFlags.DPadUp)) zoom += zoomamt;
                if (Input.ControllerButtonPressed(GamepadButtonFlags.DPadDown)) zoom -= zoomamt;

                camera.ControllerZoom(zoom);
            }
        }

        private void PedsForm_Load(object sender, EventArgs e)
        {
            Init();
        }

        private void PedsForm_MouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left: MouseLButtonDown = true; break;
                case MouseButtons.Right: MouseRButtonDown = true; break;
            }

            if (!ToolsPanelShowButton.Focused)
            {
                ToolsPanelShowButton.Focus(); //make sure no textboxes etc are focused!
            }

            MouseDownPoint = e.Location;
            MouseLastPoint = MouseDownPoint;

            if (MouseLButtonDown)
            {
            }

            if (MouseRButtonDown)
            {
                //SelectMousedItem();
            }

            MouseX = e.X; //to stop jumps happening on mousedown, sometimes the last MouseMove event was somewhere else... (eg after clicked a menu)
            MouseY = e.Y;
        }

        private void PedsForm_MouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left: MouseLButtonDown = false; break;
                case MouseButtons.Right: MouseRButtonDown = false; break;
            }

        }

        private void PedsForm_MouseMove(object sender, MouseEventArgs e)
        {
            int dx = e.X - MouseX;
            int dy = e.Y - MouseY;

            {
                if (MouseLButtonDown)
                {
                    RotateCam(dx, dy);
                }
                if (MouseRButtonDown)
                {
                    if (Renderer.controllightdir)
                    {
                        Renderer.lightdirx += (dx * camera.Sensitivity);
                        Renderer.lightdiry += (dy * camera.Sensitivity);
                    }
                    else if (Renderer.controltimeofday)
                    {
                        float tod = Renderer.timeofday;
                        tod += (dx - dy) / 30.0f;
                        while (tod >= 24.0f) tod -= 24.0f;
                        while (tod < 0.0f) tod += 24.0f;
                        timecycle.SetTime(tod);
                        Renderer.timeofday = tod;

                        float fv = tod * 60.0f;
                        TimeOfDayTrackBar.Value = (int)fv;
                        UpdateTimeOfDayLabel();
                    }
                }

                UpdateMousePosition(e);
            }
        }

        private void PedsForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta != 0)
            {
                camera.MouseZoom(e.Delta);
                ClearActivePreset();
            }
        }

        private void PedsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (ActiveControl is TextBox)
            {
                var tb = ActiveControl as TextBox;
                if (!tb.ReadOnly) return; //don't move the camera when typing!
            }
            if (ActiveControl is ComboBox)
            {
                var cb = ActiveControl as ComboBox;
                if (cb.DropDownStyle != ComboBoxStyle.DropDownList) return; //nontypable combobox
            }

            bool enablemove = true;// (!iseditmode) || (MouseLButtonDown && (GrabbedMarker == null) && (GrabbedWidget == null));

            Input.KeyDown(e, enablemove);

            var k = e.KeyCode;
            var kb = Input.keyBindings;
            bool ctrl = Input.CtrlPressed;
            bool shift = Input.ShiftPressed;


            if (!ctrl)
            {
                if (k == kb.MoveSlowerZoomIn)
                {
                    camera.MouseZoom(1);
                }
                if (k == kb.MoveFasterZoomOut)
                {
                    camera.MouseZoom(-1);
                }
            }
        }

        private void PedsForm_KeyUp(object sender, KeyEventArgs e)
        {
            Input.KeyUp(e);

            if (ActiveControl is TextBox)
            {
                var tb = ActiveControl as TextBox;
                if (!tb.ReadOnly) return; //don't move the camera when typing!
            }
            if (ActiveControl is ComboBox)
            {
                var cb = ActiveControl as ComboBox;
                if (cb.DropDownStyle != ComboBoxStyle.DropDownList) return; //non-typable combobox
            }
        }

        private void PedsForm_Deactivate(object sender, EventArgs e)
        {
            //try not to lock keyboard movement if the form loses focus.
            Input.KeyboardStop();
        }

        private void StatsUpdateTimer_Tick(object sender, EventArgs e)
        {
            StatsLabel.Text = Renderer.GetStatusText();

            if (Renderer.timerunning)
            {
                float fv = Renderer.timeofday * 60.0f;
                //TimeOfDayTrackBar.Value = (int)fv;
                UpdateTimeOfDayLabel();
            }
        }

        private void ToolsPanelShowButton_Click(object sender, EventArgs e)
        {
            ToolsPanel.Visible = true;
        }

        private void ToolsPanelHideButton_Click(object sender, EventArgs e)
        {
            ToolsPanel.Visible = false;
        }

        private void ToolsDragPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                toolsPanelResizing = true;
                toolsPanelResizeStartX = e.X + ToolsPanel.Left + ToolsDragPanel.Left;
                toolsPanelResizeStartLeft = ToolsPanel.Left;
                toolsPanelResizeStartRight = ToolsPanel.Right;
            }
        }

        private void ToolsDragPanel_MouseUp(object sender, MouseEventArgs e)
        {
            toolsPanelResizing = false;
        }

        private void ToolsDragPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (toolsPanelResizing)
            {
                int rx = e.X + ToolsPanel.Left + ToolsDragPanel.Left;
                int dx = rx - toolsPanelResizeStartX;
                ToolsPanel.Width = toolsPanelResizeStartRight - toolsPanelResizeStartLeft + dx;
            }
        }

        private void ModelsTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                UpdateSelectionDrawFlags(e.Node);
            }
        }

        private void ModelsTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node != null)
            {
                e.Node.Checked = !e.Node.Checked;
                //UpdateSelectionDrawFlags(e.Node);
            }
        }

        private void ModelsTreeView_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true; //stops annoying ding sound...
        }

        private void ShadowsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            lock (Renderer.RenderSyncRoot)
            {
                Renderer.shaders.shadows = ShadowsCheckBox.Checked;
            }
        }

        private void ControlLightDirCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Renderer.controllightdir = ControlLightDirCheckBox.Checked;
        }

        private void TimeOfDayTrackBar_Scroll(object sender, EventArgs e)
        {
            int v = TimeOfDayTrackBar.Value;
            float fh = v / 60.0f;
            UpdateTimeOfDayLabel();
            lock (Renderer.RenderSyncRoot)
            {
                Renderer.timeofday = fh;
                timecycle.SetTime(Renderer.timeofday);
            }
        }

        private void WireframeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Renderer.shaders.wireframe = WireframeCheckBox.Checked;
        }

        private void RenderModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            TextureSamplerComboBox.Enabled = false;
            TextureCoordsComboBox.Enabled = false;
            switch (RenderModeComboBox.Text)
            {
                default:
                case "Default":
                    Renderer.shaders.RenderMode = WorldRenderMode.Default;
                    break;
                case "Single texture":
                    Renderer.shaders.RenderMode = WorldRenderMode.SingleTexture;
                    TextureSamplerComboBox.Enabled = true;
                    TextureCoordsComboBox.Enabled = true;
                    break;
                case "Vertex normals":
                    Renderer.shaders.RenderMode = WorldRenderMode.VertexNormals;
                    break;
                case "Vertex tangents":
                    Renderer.shaders.RenderMode = WorldRenderMode.VertexTangents;
                    break;
                case "Vertex colour 1":
                    Renderer.shaders.RenderMode = WorldRenderMode.VertexColour;
                    Renderer.shaders.RenderVertexColourIndex = 1;
                    break;
                case "Vertex colour 2":
                    Renderer.shaders.RenderMode = WorldRenderMode.VertexColour;
                    Renderer.shaders.RenderVertexColourIndex = 2;
                    break;
                case "Vertex colour 3":
                    Renderer.shaders.RenderMode = WorldRenderMode.VertexColour;
                    Renderer.shaders.RenderVertexColourIndex = 3;
                    break;
                case "Texture coord 1":
                    Renderer.shaders.RenderMode = WorldRenderMode.TextureCoord;
                    Renderer.shaders.RenderTextureCoordIndex = 1;
                    break;
                case "Texture coord 2":
                    Renderer.shaders.RenderMode = WorldRenderMode.TextureCoord;
                    Renderer.shaders.RenderTextureCoordIndex = 2;
                    break;
                case "Texture coord 3":
                    Renderer.shaders.RenderMode = WorldRenderMode.TextureCoord;
                    Renderer.shaders.RenderTextureCoordIndex = 3;
                    break;
            }
        }

        private void TextureSamplerComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TextureSamplerComboBox.SelectedItem is ShaderParamNames)
            {
                Renderer.shaders.RenderTextureSampler = (ShaderParamNames)TextureSamplerComboBox.SelectedItem;
            }
        }

        private void TextureCoordsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (TextureCoordsComboBox.Text)
            {
                default:
                case "Texture coord 1":
                    Renderer.shaders.RenderTextureSamplerCoord = 1;
                    break;
                case "Texture coord 2":
                    Renderer.shaders.RenderTextureSamplerCoord = 2;
                    break;
                case "Texture coord 3":
                    Renderer.shaders.RenderTextureSamplerCoord = 3;
                    break;
            }
        }

        private void SkeletonsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Renderer.renderskeletons = SkeletonsCheckBox.Checked;
        }

        private void StatusBarCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            StatusStrip.Visible = StatusBarCheckBox.Checked;
        }

        private void ErrorConsoleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ConsolePanel.Visible = ErrorConsoleCheckBox.Checked;
        }

        private void PedNameComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!GameFileCache.IsInited) return;

            LoadPed();
        }

        private void ClipDictComboBox_TextChanged(object sender, EventArgs e)
        {
            if (_suppressClipDictEvent) return;

            LoadClipDict(ClipDictComboBox.Text);
        }


        private void ClipComboBox_TextChanged(object sender, EventArgs e)
        {
            SelectClip(ClipComboBox.Text);
        }

        private void EnableRootMotionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SelectedPed.EnableRootMotion = EnableRootMotionCheckBox.Checked;
        }

        private void HDRRenderingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            lock (Renderer.RenderSyncRoot)
            {
                Renderer.shaders.hdr = HDRRenderingCheckBox.Checked;
            }
        }

        private void EnableAnimationCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ClipComboBox.Enabled = EnableAnimationCheckBox.Checked;
            ClipDictComboBox.Enabled = EnableAnimationCheckBox.Checked;
            CustomAnimComboBox.Enabled = EnableAnimationCheckBox.Checked;
            EnableRootMotionCheckBox.Enabled = EnableAnimationCheckBox.Checked;
            PlaybackSpeedTrackBar.Enabled = EnableAnimationCheckBox.Checked;

            if (EnableAnimationCheckBox.Checked)
            {
                LoadAnimsForModel(SelectedPed.Name);
            } 
            else
            {
                ClipDictComboBox.Text = "";
                ClipComboBox.Text = "";
                CustomAnimComboBox.Text = "";
            }
        }

        private void PlaybackSpeedTrackBar_Scroll(object sender, EventArgs e)
        {
            int v = PlaybackSpeedTrackBar.Value;
            float fh = v / 60.0f;
            UpdatePlaybackSpeedLabel();

            lock (Renderer.RenderSyncRoot)
            {
                SelectedPed.AnimClip.PlaybackSpeed = fh;
            }
        }

        private void UpdatePlaybackSpeedLabel()
        {
            int v = PlaybackSpeedTrackBar.Value;
            float fh = v / 60.0f;
            PlaybackSpeedLabel.Text = string.Format("{0:0.00}", fh);
        }

        private void OptionsComponent_UpDown_ValueChanged(object sender, EventArgs e)
        {
            var compId = Convert.ToInt32(((NumericUpDown)sender).Tag);
            var value = Convert.ToInt32(((NumericUpDown)sender).Value);

            SetComponentDrawable(compId, value);
        }

        private void Save_defaultComp_Click(object sender, EventArgs e)
        {
            //ugly as shit but yeah it works

            int index = PedNameComboBox.SelectedIndex;

            var head = Settings.Default.HeadComp.Split(';');
            head[index] = head_updown.Value.ToString();
            Settings.Default.HeadComp = string.Join(";", head);

            var berd = Settings.Default.BerdComp.Split(';');
            berd[index] = berd_updown.Value.ToString();
            Settings.Default.BerdComp = string.Join(";", berd);

            var hair = Settings.Default.HairComp.Split(';');
            hair[index] = hair_updown.Value.ToString();
            Settings.Default.HairComp = string.Join(";", hair);

            var uppr = Settings.Default.UpprComp.Split(';');
            uppr[index] = uppr_updown.Value.ToString();
            Settings.Default.UpprComp = string.Join(";", uppr);

            var lowr = Settings.Default.LowrComp.Split(';');
            lowr[index] = lowr_updown.Value.ToString();
            Settings.Default.LowrComp = string.Join(";", lowr);

            var feet = Settings.Default.FeetComp.Split(';');
            feet[index] = feet_updown.Value.ToString();
            Settings.Default.FeetComp = string.Join(";", feet);



            Settings.Default.Save();
        }

        private void LiveTexturePreview_Click(object sender, EventArgs e)
        {
            if (liveTexturePath != null)
            {
                liveTexturePath = null;
                Renderer.LiveTextureEnabled = false;
            }
            else
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        liveTexturePath = fbd.SelectedPath;
                        Renderer.LiveTextureEnabled = true;
                    }
                }
            }

            liveTxtButton.Text = Renderer.LiveTextureEnabled ? "Отключить" : "Включить";
            diffuseRadio.Enabled = !Renderer.LiveTextureEnabled;
            normalRadio.Enabled = !Renderer.LiveTextureEnabled;
            specularRadio.Enabled = !Renderer.LiveTextureEnabled;
        }

        private void liveTexture_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton.Checked == true)
            {
                switch (radioButton.Text)
                {
                    case "Diffuse":
                        Renderer.LiveTextureSelectedMode = LiveTextureMode.Diffuse;
                        break;
                    case "Normal":
                        Renderer.LiveTextureSelectedMode = LiveTextureMode.Normal;
                        break;
                    case "Specular":
                        Renderer.LiveTextureSelectedMode = LiveTextureMode.Specular;
                        break;
                }
            }
        }

        private void FloorCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            floorUpDown.Enabled = floorCheckbox.Checked;
        }

        private void FloorUpDown_ValueChanged(object sender, EventArgs e)
        {
            var value = Convert.ToInt32(((NumericUpDown)sender).Value);

        }

        private void AutoRotateTimer_Tick(object sender, EventArgs e)
        {
            if (SelectedPed != null)
            {
                autoRotateAngle += AutoRotateSpeed * (float)Math.PI / 180f;
                if (autoRotateAngle > 2 * Math.PI) // Keep the autoRotateAngle within the 0 to 360
                {
                    autoRotateAngle -= 2 * (float)Math.PI;
                }

                Quaternion newRotation = Quaternion.RotationYawPitchRoll(0, 0, autoRotateAngle);
                SelectedPed.Rotation = newRotation;
                SelectedPed.UpdateEntity();
            }
        }

        private void AutoRotatePedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (AutoRotatePedCheckBox.Checked)
            {
                autoRotateTimer.Start();
            }
            else
            {
                autoRotateTimer.Stop();
            }
        }

        private void OnlySelectedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (OnlySelectedCheckBox.Checked == renderOnlySelected) { return; }
            renderOnlySelected = OnlySelectedCheckBox.Checked;
            
            UpdateModelsUI();
        }

        private void SetDefaultCameraPosition()
        {
            camera.FollowEntity = camEntity;
            camera.FollowEntity.Position = Vector3.Zero;// prevworldpos;

            // used to be Vector3.ForwardLH, but default animations rotates ped, so changed it to Vector3.ForwardRH 
            camera.FollowEntity.Orientation = Quaternion.LookAtLH(Vector3.Zero, Vector3.Up, Vector3.ForwardRH); 

            camera.TargetDistance = 2.0f;
            camera.CurrentDistance = 2.0f;
            camera.TargetRotation.Y = 0.2f;
            camera.CurrentRotation.Y = 0.2f;
            camera.TargetRotation.X = 1.0f * (float)Math.PI;
            camera.CurrentRotation.X = 1.0f * (float)Math.PI;

            if (SelectedPed != null)
            {
                // restart rotation angle, so ped faces camera
                autoRotateAngle = 0f;
                SelectedPed.Rotation = Quaternion.Identity;
                SelectedPed.UpdateEntity();
            }
        }

        private void LoadCustomAnimationsFromFolder()
        {
            try
            {
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string animationsFolder = Path.Combine(exeDirectory, "animations");

                if (!Directory.Exists(animationsFolder))
                    return;

                string[] files = Directory.GetFiles(animationsFolder, "*.ycd", SearchOption.TopDirectoryOnly);

                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);

                    if (CustomAnimations.ContainsKey(fileName))
                        continue;

                    try
                    {
                        byte[] data = File.ReadAllBytes(filePath);

                        RpfResourceFileEntry resentry = RpfFile.CreateResourceFileEntry(ref data, 46);
                        byte[] decompressedData = ResourceBuilder.Decompress(data);

                        YcdFile ycd = new YcdFile();
                        ycd.Load(decompressedData, resentry);

                        if (ycd.ClipDictionary != null && ycd.ClipMapEntries != null && ycd.ClipMapEntries.Length > 0)
                        {
                            ycd.Loaded = true;
                            CustomAnimations[fileName] = ycd;
                            CustomAnimationPaths.Add(filePath);

                            CustomAnimComboBox.Items.Add(fileName);
                        }
                        else
                        {
                            Debug.WriteLine($"Skipping invalid custom animation: {fileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error loading custom animation {fileName}: {ex.Message}");
                    }
                }

                CustomAnimComboBox.Refresh();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadCustomAnimationsFromFolder() failed: {ex.Message}");
            }
        }


        private string CopyAnimationToFolder(string sourceFilePath)
        {
            try
            {
                string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string animationsFolder = Path.Combine(exeDirectory, "animations");

                if (!Directory.Exists(animationsFolder))
                {
                    Directory.CreateDirectory(animationsFolder);
                }

                string fileName = Path.GetFileName(sourceFilePath);
                string destinationPath = Path.Combine(animationsFolder, fileName);

                File.Copy(sourceFilePath, destinationPath, true);

                return destinationPath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка копирования файла анимации:\n{ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return sourceFilePath;
            }
        }

        private void UpdateCameraInputsText()
        {
            if (CameraPositionTextBox.Focused || CameraRotationTextBox.Focused || CameraDistanceTextBox.Focused)
                return;

            CameraPositionTextBox.Text = Vector3ToText(camEntity.Position);
            CameraRotationTextBox.Text = Vector3ToText(RadiansToDegrees(camera.CurrentRotation));
            CameraDistanceTextBox.Text = $"{camera.CurrentDistance.ToString("F4", CultureInfo.InvariantCulture)}";
        }

        private void UpdateCameraInputs()
        {
            if (_inputsUpdatePending) return;

            if (CameraPositionTextBox.InvokeRequired)
            {
                _inputsUpdatePending = true;

                BeginInvoke((Action)(() =>
                {
                    try
                    {
                        UpdateCameraInputsText();
                    }
                    finally
                    {
                        _inputsUpdatePending = false;
                    }
                }));
            }
            else
            {
                UpdateCameraInputsText();
            }
        }

        private string Vector3ToText(Vector3 v)
        {
            return $"{v.X.ToString("F4", CultureInfo.InvariantCulture)}, {v.Y.ToString("F4", CultureInfo.InvariantCulture)}, {v.Z.ToString("F4", CultureInfo.InvariantCulture)}";
        }

        private bool TryParseVector3FromText(string text, out Vector3 result)
        {
            result = Vector3.Zero;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var parts = text.Split(',');

            if (parts.Length != 3)
                return false;

            float x, y, z;

            if (!float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out x)) return false;
            if (!float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out y)) return false;
            if (!float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out z)) return false;

            result = new Vector3(x, y, z);
            return true;
        }

        private static Vector3 DegreesToRadians(Vector3 degrees)
        {
            float radiansX = 3.14f * degrees.X / 180f;
            float radiansY = 3.14f * degrees.Y / 180f;
            float radiansZ = 3.14f * degrees.Z / 180f;

            return new Vector3(radiansX, radiansY, radiansZ);
        }

        private static Vector3 RadiansToDegrees(Vector3 radians)
        {
            float degreesX = radians.X * 180f / 3.14f;
            float degreesY = radians.Y * 180f / 3.14f;
            float degreesZ = radians.Z * 180f / 3.14f;

            return new Vector3(degreesX, degreesY, degreesZ);
        }

        private void CameraPositionTextBox_TextChanged(object sender, EventArgs e)
        {
            if (TryParseVector3FromText(CameraPositionTextBox.Text, out Vector3 position))
            {
                camEntity.Position = position;
                if (CameraPositionTextBox.Focused)
                {
                    ClearActivePreset();
                }
            }
        }

        private void CameraRotationTextBox_TextChanged(object sender, EventArgs e)
        {
            if (TryParseVector3FromText(CameraRotationTextBox.Text, out Vector3 rotationDeg))
            {
                var rotationRad = DegreesToRadians(rotationDeg);
                camera.CurrentRotation = rotationRad;
                camera.TargetRotation = rotationRad;
                if (CameraRotationTextBox.Focused)
                {
                    ClearActivePreset();
                }
            }
        }

        private void CameraDistanceTextBox_TextChanged(object sender, EventArgs e)
        {
            if (float.TryParse(CameraDistanceTextBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float distance))
            {
                camera.CurrentDistance = distance;
                camera.TargetDistance = distance;
                if (CameraDistanceTextBox.Focused)
                {
                    ClearActivePreset();
                }
            }
        }

        private void FillDataGridView()
        {
            CameraPresetsDataGridView.Rows.Clear();

            var presets = CameraPresetCollection.Deserialize(Settings.Default.CameraPresets);
            foreach (var mapValue in presets.Values)
            {
                DataGridViewRow NewPresetRow = new DataGridViewRow();

                NewPresetRow.CreateCells(CameraPresetsDataGridView);
                NewPresetRow.Cells[0].Value = mapValue.Name;
                NewPresetRow.Cells[1].Value = "✕";

                // Highlight active preset with green
                if (mapValue.Name == _activePresetName)
                {
                    NewPresetRow.DefaultCellStyle.BackColor = System.Drawing.Color.LightGreen;
                    NewPresetRow.DefaultCellStyle.ForeColor = System.Drawing.Color.Black;
                    NewPresetRow.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.LightGreen;
                    NewPresetRow.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black;
                }
                else
                {
                    NewPresetRow.DefaultCellStyle.SelectionBackColor = CameraPresetsDataGridView.DefaultCellStyle.BackColor;
                    NewPresetRow.DefaultCellStyle.SelectionForeColor = CameraPresetsDataGridView.DefaultCellStyle.ForeColor;
                }

                CameraPresetsDataGridView.Rows.Add(NewPresetRow);
            }
        }

        private void btn_addCameraPreset_Click(object sender, EventArgs e)
        {
            if (CameraSavePresetTextBox.Text.Length == 0)
                return;

            var presetName = CameraSavePresetTextBox.Text;

            var presets = CameraPresetCollection.Deserialize(Settings.Default.CameraPresets);
            var position = CameraPositionTextBox.Text;
            var rotation = CameraRotationTextBox.Text;
            var distance = CameraDistanceTextBox.Text;
            presets.Add(new CameraPreset(presetName, position, rotation, distance));
            Settings.Default.CameraPresets = presets.Serialize();
            Settings.Default.Save();

            CameraSavePresetTextBox.Text = "";
            SetActivePreset(presetName);
        }

        private void CameraPresetsDataGridView_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var presets = CameraPresetCollection.Deserialize(Settings.Default.CameraPresets);
            if (e.RowIndex >= presets.Values.Count) return;

            // Delete preset (X column)
            if (e.ColumnIndex == CameraPresetsDataGridView.Columns["DataGridViewDelete"].Index)
            {
                var preset = presets.GetByIndex(e.RowIndex);
                var presetName = preset.Name;

                presets.RemoveByName(presetName);
                Settings.Default.CameraPresets = presets.Serialize();
                Settings.Default.Save();

                if (_activePresetName == presetName)
                {
                    ClearActivePreset();
                }

                FillDataGridView();
            }
        }

        private void CameraPresetsDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex != CameraPresetsDataGridView.Columns["DataGridViewName"].Index)
                return;

            var presets = CameraPresetCollection.Deserialize(Settings.Default.CameraPresets);
            if (e.RowIndex >= presets.Values.Count) return;

            var preset = presets.GetByIndex(e.RowIndex);

            ApplyCameraPreset(preset);
            SetActivePreset(preset.Name);
        }

        private void RestartCamera_Click(object sender, EventArgs e)
        {
            SetDefaultCameraPosition();
            ClearActivePreset();
        }

        private void SetActivePreset(string presetName)
        {
            _activePresetName = presetName;
            Settings.Default.LastUsedCameraPreset = presetName;
            Settings.Default.Save();
            FillDataGridView();
        }

        private void ClearActivePreset()
        {
            if (_activePresetName != null)
            {
                _activePresetName = null;
                FillDataGridView();
            }
        }

        private void LoadLastUsedPreset()
        {
            var lastUsedPresetName = Settings.Default.LastUsedCameraPreset;
            if (string.IsNullOrEmpty(lastUsedPresetName))
                return;

            var presets = CameraPresetCollection.Deserialize(Settings.Default.CameraPresets);
            var preset = presets.GetByName(lastUsedPresetName);

            if (preset != null)
            {
                _activePresetName = preset.Name;
                ApplyCameraPreset(preset);
                FillDataGridView();
            }
        }

        private void ApplyCameraPreset(CameraPreset preset)
        {
            if (TryParseVector3FromText(preset.Position, out Vector3 position))
            {
                camEntity.Position = position;
            }

            if (TryParseVector3FromText(preset.Rotation, out Vector3 rotationDeg))
            {
                var rotationRad = DegreesToRadians(rotationDeg);
                camera.CurrentRotation = rotationRad;
                camera.TargetRotation = rotationRad;
            }

            if (float.TryParse(preset.Distance, NumberStyles.Float, CultureInfo.InvariantCulture, out float distance))
            {
                camera.CurrentDistance = distance;
                camera.TargetDistance = distance;
            }

            CameraPositionTextBox.Text = preset.Position;
            CameraRotationTextBox.Text = preset.Rotation;
            CameraDistanceTextBox.Text = preset.Distance;
        }

        private void AddCustomAnimButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Файлы YCD (*.ycd)|*.ycd|Все файлы (*.*)|*.*";
                ofd.Title = "Выбор файла анимации";
                ofd.Multiselect = true;

                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                foreach (string filePath in ofd.FileNames)
                {
                    string fileName = Path.GetFileName(filePath);

                    if (CustomAnimations.ContainsKey(fileName))
                    {
                        UpdateStatus($"Анимация уже загружена: {fileName}");
                        continue;
                    }

                    try
                    {
                        string copiedPath = CopyAnimationToFolder(filePath);

                        byte[] data = File.ReadAllBytes(copiedPath);
                        RpfResourceFileEntry resentry = RpfFile.CreateResourceFileEntry(ref data, 46);
                        byte[] decompressedData = ResourceBuilder.Decompress(data);

                        YcdFile ycd = new YcdFile();
                        ycd.Load(decompressedData, resentry);

                        if (ycd.ClipDictionary != null && ycd.ClipMapEntries != null && ycd.ClipMapEntries.Length > 0)
                        {
                            ycd.Loaded = true;
                            CustomAnimationPaths.Add(copiedPath);
                            CustomAnimations[fileName] = ycd;

                            CustomAnimComboBox.Items.Add(fileName);
                            CustomAnimComboBox.Refresh();

                            UpdateStatus($"Пользовательская анимация загружена: {fileName} (клипов: {ycd.ClipMapEntries.Length})");
                        }
                        else
                        {
                            MessageBox.Show($"Недопустимый файл анимации: {fileName}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки файла {fileName}:\n{ex.Message}",
                            "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }


        private void CustomAnimComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CustomAnimComboBox.SelectedIndex < 0)
                return;

            string selectedFileName = CustomAnimComboBox.SelectedItem.ToString();

            if (!CustomAnimations.TryGetValue(selectedFileName, out YcdFile ycd))
                return;

            SelectedPed.Ycd = ycd;

            ClipComboBox.Items.Clear();
            ClipComboBox.Items.Add("");

            if (ycd?.ClipMapEntries != null)
            {
                List<string> items = new List<string>();

                foreach (var cme in ycd.ClipMapEntries)
                {
                    if (cme.Clip != null)
                        items.Add(cme.Clip.ShortName);
                }

                items.Sort();
                foreach (var item in items)
                    ClipComboBox.Items.Add(item);
            }

            _suppressClipDictEvent = true;
            try
            {
                ClipDictComboBox.Text = "";
            }
            finally
            {
                _suppressClipDictEvent = false;
            }

            if (ClipComboBox.Items.Count > 1)
            {
                ClipComboBox.SelectedIndex = 1;
                SelectClip(ClipComboBox.Items[1].ToString());
            }
        }

    }
}
