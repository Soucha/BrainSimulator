using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.IO;
using System.Windows.Forms.Design;
using GoodAI.Core.Memory;
using GoodAI.Core.Nodes;
using GoodAI.Core.Utils;
using GoodAI.ToyWorld.Control;
using Logger;
using GoodAI.ToyWorld.Language;
using ToyWorldFactory;
using YAXLib;

namespace GoodAI.ToyWorld
{
    /// <author>GoodAI</author>
    /// <meta>mm,mp,mv,ms</meta>
    /// <status>Working</status>
    /// <summary> A continuous 2D learning environment.</summary>
    /// <description>
    /// <p>
    /// A more generally customizable continuous 2D world (has a prototype 3D version). The agent can control an avatar that moves in <br />
    /// a simple environment and can interact with various objects. The world is viewed from top-down in 2D mode and from the avatar's <br />
    /// view in the 3D mode.
    /// </p>
    /// <p>
    /// For a more detailed description please see the <a href="http://docs.goodai.com/brainsimulator/examples/toyworld/index.html">Toy World's overview page</a>.
    /// </p>
    /// </description>
    public partial class ToyWorld : MyWorld, IMyVariableBranchViewNodeBase
    {
        private readonly int m_controlsCount = 13;

        public TWInitTask InitTask { get; protected set; }
        public TWGetInputTask GetInputTask { get; protected set; }
        public TWUpdateTask UpdateTask { get; protected set; }

        public event EventHandler WorldInitialized = delegate { };

        #region Memblocks

        [MyOutputBlock(0), MyUnmanaged]
        public MyMemoryBlock<float> VisualFov
        {
            get { return GetOutput(0); }
            set { SetOutput(0, value); }
        }

        [MyOutputBlock(1), MyUnmanaged]
        public MyMemoryBlock<float> VisualFovDepth
        {
            get { return GetOutput(1); }
            set { SetOutput(1, value); }
        }

        [MyOutputBlock(2), MyUnmanaged]
        public MyMemoryBlock<float> VisualFof
        {
            get { return GetOutput(2); }
            set { SetOutput(2, value); }
        }

        [MyOutputBlock(3), MyUnmanaged]
        public MyMemoryBlock<float> VisualFofDepth
        {
            get { return GetOutput(3); }
            set { SetOutput(3, value); }
        }

        [MyOutputBlock(4), MyUnmanaged]
        public MyMemoryBlock<float> VisualFree
        {
            get { return GetOutput(4); }
            set { SetOutput(4, value); }
        }

        [MyOutputBlock(5), MyUnmanaged]
        public MyMemoryBlock<float> VisualFreeDepth
        {
            get { return GetOutput(5); }
            set { SetOutput(5, value); }
        }

        [MyOutputBlock(6), MyUnmanaged]
        public MyMemoryBlock<float> VisualTool
        {
            get { return GetOutput(6); }
            set { SetOutput(6, value); }
        }


        [MyOutputBlock(7)]
        public MyMemoryBlock<float> Text
        {
            get { return GetOutput(7); }
            set { SetOutput(7, value); }
        }

        [MyOutputBlock(8)]
        public MyMemoryBlock<float> ChosenActions
        {
            get { return GetOutput(8); }
            set { SetOutput(8, value); }
        }

        [MyInputBlock(0)]
        public MyMemoryBlock<float> Controls
        {
            get { return GetInput(0); }
        }

        [MyInputBlock(1)]
        public MyMemoryBlock<float> TextIn
        {
            get { return GetInput(1); }
        }

        #endregion

        #region BrainSim properties

        [MyBrowsable, Category("Runtime"), DisplayName("Run every Nth")]
        [YAXSerializableField(DefaultValue = 1)]
        public int RunEvery { get; set; }

        [MyBrowsable, Category("Runtime"), DisplayName("Use 60 FPS cap")]
        [YAXSerializableField(DefaultValue = false)]
        public bool UseFpsCap { get; set; }

        [MyBrowsable, Category("Runtime"), DisplayName("Copy data through CPU")]
        [YAXSerializableField(DefaultValue = false)]
        public bool CopyDataThroughCPU { get; set; }

        [MyBrowsable, Category("Runtime"), DisplayName("Copy depth data")]
        [YAXSerializableField(DefaultValue = false)]
        public bool CopyDepthData { get; set; }


        [MyBrowsable, Category("Files"), EditorAttribute(typeof(FileNameEditor), typeof(UITypeEditor))]
        [YAXSerializableField(DefaultValue = null), YAXCustomSerializer(typeof(MyPathSerializer))]
        public string TilesetTable { get; set; }

        [MyBrowsable, Category("Files"), EditorAttribute(typeof(FileNameEditor), typeof(UITypeEditor))]
        [YAXSerializableField(DefaultValue = null), YAXCustomSerializer(typeof(MyPathSerializer))]
        public string SaveFile { get; set; }

        [MyBrowsable, Category("Controls"), DisplayName("Control mode")]
        [YAXSerializableField(DefaultValue = ControlMapper.ControlMode.Autodetect)]
        public ControlMapper.ControlMode ControlModeVisible { get; set; } // only for the user - do not use otherwise

        public ControlMapper.ControlMode ControlModeHidden // translates autodetect into the appropriate mode in UpdateMemoryBlocks
        {
            get
            {
                return ControlMapper.Mode;
            }
            set
            {
                ControlMapper.Mode = value;
            }
        }


        #region Effects

        [MyBrowsable, Category("Effects - General"), DisplayName("Rotate Map")]
        [YAXSerializableField(DefaultValue = false)]
        public bool RotateMap { get; set; }

        [MyBrowsable, Category("Effects - General"), DisplayName("Use 3D")]
        [YAXSerializableField(DefaultValue = false)]
        public bool Use3D { get; set; }


        [MyBrowsable, Category("Effects - Noise"), DisplayName("Draw noise")]
        [YAXSerializableField(DefaultValue = false)]
        public bool DrawNoise { get; set; }

        [MyBrowsable, Category("Effects - Noise"), DisplayName("Noise intensity")]
        [YAXSerializableField(DefaultValue = 0.5f)]
        public float NoiseIntensity { get; set; }


        [MyBrowsable, Category("Effects - Smoke"), DisplayName("Draw smoke")]
        [YAXSerializableField(DefaultValue = false)]
        public bool DrawSmoke { get; set; }

        [MyBrowsable, Category("Effects - Smoke"), DisplayName("Smoke intensity")]
        [YAXSerializableField(DefaultValue = 0.5f)]
        public float SmokeIntensity { get; set; }

        [MyBrowsable, Category("Effects - Smoke"), DisplayName("Smoke scale")]
        [YAXSerializableField(DefaultValue = 1.0f)]
        public float SmokeScale { get; set; }

        [MyBrowsable, Category("Effects - Smoke"), DisplayName("Smoke transf. speed")]
        [YAXSerializableField(DefaultValue = 1.0f)]
        public float SmokeTransformationSpeed { get; set; }

        [MyBrowsable, Category("Effects - Lighting"), DisplayName("Day/Night cycle")]
        [YAXSerializableField(DefaultValue = false)]
        public bool EnableDayAndNightCycle { get; set; }

        [MyBrowsable, Category("Effects - Lighting"), DisplayName("Draw lights")]
        [YAXSerializableField(DefaultValue = false)]
        public bool DrawLights { get; set; }

        #endregion

        #region RenderRequests

        [MyBrowsable, Category("RR: FoF view"), DisplayName("Size")]
        [YAXSerializableField(DefaultValue = 3)]
        public int FoFSize { get; set; }

        [MyBrowsable, Category("RR: FoF view"), DisplayName("Resolution width")]
        [YAXSerializableField(DefaultValue = 1024)]
        public int FoFResWidth { get; set; }

        [MyBrowsable, Category("RR: FoF view"), DisplayName("Resolution height")]
        [YAXSerializableField(DefaultValue = 1024)]
        public int FoFResHeight { get; set; }

        [MyBrowsable, Category("RR: FoF view"), DisplayName("Multisample level")]
        [YAXSerializableField(DefaultValue = RenderRequestMultisampleLevel.x4)]
        public RenderRequestMultisampleLevel FoFMultisampleLevel { get; set; }


        [MyBrowsable, Category("RR: FoV view"), DisplayName("Size")]
        [YAXSerializableField(DefaultValue = 21)]
        public int FoVSize { get; set; }

        [MyBrowsable, Category("RR: FoV view"), DisplayName("Resolution width")]
        [YAXSerializableField(DefaultValue = 1024)]
        public int FoVResWidth { get; set; }

        [MyBrowsable, Category("RR: FoV view"), DisplayName("Resolution height")]
        [YAXSerializableField(DefaultValue = 1024)]
        public int FoVResHeight { get; set; }

        [MyBrowsable, Category("RR: FoV view"), DisplayName("Multisample level")]
        [YAXSerializableField(DefaultValue = RenderRequestMultisampleLevel.x4)]
        public RenderRequestMultisampleLevel FoVMultisampleLevel { get; set; }


        [MyBrowsable, Category("RR: Free view"), DisplayName("\tCenter - X")]
        [YAXSerializableField(DefaultValue = 25)]
        public float CenterX { get; set; }

        [MyBrowsable, Category("RR: Free view"), DisplayName("\tCenter - Y")]
        [YAXSerializableField(DefaultValue = 25)]
        public float CenterY { get; set; }

        [MyBrowsable, Category("RR: Free view"), DisplayName("\tWidth")]
        [YAXSerializableField(DefaultValue = 50)]
        public float Width { get; set; }

        [MyBrowsable, Category("RR: Free view"), DisplayName("\tHeight")]
        [YAXSerializableField(DefaultValue = 50)]
        public float Height { get; set; }

        [MyBrowsable, Category("RR: Free view"), DisplayName("\tResolution width")]
        [YAXSerializableField(DefaultValue = 1024)]
        public int ResolutionWidth { get; set; }

        [MyBrowsable, Category("RR: Free view"), DisplayName("\tResolution height")]
        [YAXSerializableField(DefaultValue = 1024)]
        public int ResolutionHeight { get; set; }

        [MyBrowsable, Category("RR: Free view"), DisplayName("Multisample level")]
        [YAXSerializableField(DefaultValue = RenderRequestMultisampleLevel.x4)]
        public RenderRequestMultisampleLevel FreeViewMultisampleLevel { get; set; }


        [MyBrowsable, Category("RR: Tool view"), DisplayName("Size")]
        [YAXSerializableField(DefaultValue = 0.9f)]
        public float ToolSize { get; set; }

        [MyBrowsable, Category("RR: Tool view"), DisplayName("Resolution width")]
        [YAXSerializableField(DefaultValue = 128)]
        public int ToolResWidth { get; set; }

        [MyBrowsable, Category("RR: Tool view"), DisplayName("Resolution height")]
        [YAXSerializableField(DefaultValue = 128)]
        public int ToolResHeight { get; set; }

        [MyBrowsable, Category("RR: Tool view"), DisplayName("Background type")]
        [YAXSerializableField(DefaultValue = ToolBackgroundType.BrownBorder)]
        public ToolBackgroundType ToolBackgroundType { get; set; }

        #endregion


        [MyBrowsable, Category("Language interface"), DisplayName("Maximum message length")]
        [YAXSerializableField(DefaultValue = 128)]
        public int MaxMessageLength { get; set; }

        [MyBrowsable, Category("Language interface"), DisplayName("Word vector dimensions")]
        [YAXSerializableField(DefaultValue = 50)]
        public int WordVectorDimensions { get; set; }

        [MyBrowsable, Category("Language interface"), DisplayName("Maximum input words")]
        [YAXSerializableField(DefaultValue = 4)]
        public int MaxInputWordCount { get; set; }

        #endregion

        public Vocabulary Vocabulary { get; private set; }

        public IGameController GameCtrl { get; set; }
        public IAvatarController AvatarCtrl { get; set; }

        private IFovAvatarRR FovRR { get; set; }
        private IFofAvatarRR FofRR { get; set; }
        private IFreeMapRR FreeRR { get; set; }
        private IToolAvatarRR ToolRR { get; set; }

        protected int SignalCount { get; set; }

        public ToyWorld()
        {
            if (TilesetTable == null)
                TilesetTable = GetDllDirectory() + @"\res\GameActors\Tiles\Tilesets\TilesetTable.csv";
            if (SaveFile == null)
                SaveFile = GetDllDirectory() + @"\res\Worlds\mockup999_pantry_world.tmx";

            SignalCount = GameFactory.GetSignalCount();
            AddOutputs(SignalCount, "Signal_");

            Vocabulary = new Vocabulary(WordVectorDimensions);
        }

        public override void Validate(MyValidator validator)
        {
            validator.AssertError(Controls != null, this, "No controls available");

            validator.AssertError(File.Exists(SaveFile), this, "Please specify a correct SaveFile path in world properties.");
            validator.AssertError(File.Exists(TilesetTable), this, "Please specify a correct TilesetTable path in world properties.");

            validator.AssertError(FoFSize > 0, this, "FoF size has to be positive.");
            validator.AssertError(FoFResWidth > 0, this, "FoF resolution width has to be positive.");
            validator.AssertError(FoFResHeight > 0, this, "FoF resolution height has to be positive.");
            validator.AssertError(FoVSize > 0, this, "FoV size has to be positive.");
            validator.AssertError(FoVResWidth > 0, this, "FoV resolution width has to be positive.");
            validator.AssertError(FoVResHeight > 0, this, "FoV resolution height has to be positive.");
            validator.AssertError(Width > 0, this, "Free view width has to be positive.");
            validator.AssertError(Height > 0, this, "Free view height has to be positive.");
            validator.AssertError(ResolutionWidth > 0, this, "Free view resolution width has to be positive.");
            validator.AssertError(ResolutionHeight > 0, this, "Free view resolution height has to be positive.");
            validator.AssertError(ToolSize > 0, this, "Tool size has to be positive.");
            validator.AssertError(ToolResWidth > 0, this, "Tool resolution width has to be positive.");
            validator.AssertError(ToolResHeight > 0, this, "Tool resolution height has to be positive.");

            ControlMapper.CheckControlSize(validator, Controls, this);

            TryToyWorld();

            foreach (TWLogMessage message in TWLog.GetAllLogMessages())
                switch (message.Severity)
                {
                    case TWSeverity.Error:
                        {
                            validator.AssertError(false, this, message.ToString());
                            break;
                        }
                    case TWSeverity.Warn:
                        {
                            validator.AssertWarning(false, this, message.ToString());
                            break;
                        }
                }
        }

        private void CleanInternal()
        {
            if (GameCtrl != null)
                GameCtrl.Dispose(); // Should dispose RRs and controllers too
        }

        public override void Cleanup()
        {
            CleanInternal();
            base.Cleanup();
        }

        public override void Dispose()
        {
            CleanInternal();
            base.Dispose();
        }

        private void TryToyWorld()
        {
            if (GameCtrl != null)
                GameCtrl.Dispose(); // Should dispose RRs and controllers too

            GameSetup setup = new GameSetup(
                    new FileStream(SaveFile, FileMode.Open, FileAccess.Read, FileShare.Read),
                    new StreamReader(TilesetTable));
            GameCtrl = GameFactory.GetThreadSafeGameController(setup);
            GameCtrl.Init();
        }

        private static string GetDllDirectory()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public override void UpdateMemoryBlocks()
        {
            if (!File.Exists(SaveFile) || !File.Exists(TilesetTable) || FoFSize <= 0 || FoVSize <= 0 || Width <= 0 || Height <= 0 || ResolutionWidth <= 0 || ResolutionHeight <= 0 || FoFResHeight <= 0 || FoFResWidth <= 0 || FoVResHeight <= 0 || FoVResWidth <= 0)
                return;

            foreach (MyMemoryBlock<float> memBlock in new[] { VisualFov, VisualFof, VisualFree, VisualTool, })
            {
                memBlock.Unmanaged = !CopyDataThroughCPU;
                memBlock.Metadata[MemoryBlockMetadataKeys.RenderingMethod] = RenderingMethod.Raw;
            }

            foreach (MyMemoryBlock<float> memBlock in new[] { VisualFovDepth, VisualFofDepth, VisualFreeDepth, })
            {
                memBlock.Unmanaged = !CopyDataThroughCPU;
                memBlock.Metadata[MemoryBlockMetadataKeys.RenderingMethod] = RenderingMethod.BlackWhite;
            }

            VisualFov.Dims = VisualFovDepth.Dims = new TensorDimensions(FoVResWidth, FoVResHeight);
            VisualFof.Dims = VisualFofDepth.Dims = new TensorDimensions(FoFResWidth, FoFResHeight);
            VisualFree.Dims = VisualFreeDepth.Dims = new TensorDimensions(ResolutionWidth, ResolutionHeight);
            VisualTool.Dims = new TensorDimensions(ToolResWidth, ToolResHeight);

            Text.Count = MaxMessageLength;

            DetectControlMode();

            if (Controls == null)
                return;

            ChosenActions.Count = Controls.Count;
        }

        private void DetectControlMode()
        {
            if (ControlModeVisible == ControlMapper.ControlMode.Autodetect)
            {
                MyNode connectedNode = null;
                if (InputConnections[0] != null &&
                    (InputConnections[0].From as MyNetwork) != null &&
                    (InputConnections[0].From as MyNetwork).GroupOutputNodes[0] != null &&
                    (InputConnections[0].From as MyNetwork).GroupOutputNodes[0].InputConnections[0] != null)
                {
                    connectedNode =
                        ((MyNetwork) (InputConnections[0].From)).GroupOutputNodes[0].InputConnections[0].From;
                }

                ControlModeHidden = (connectedNode is DeviceInput)
                    ? ControlMapper.ControlMode.KeyboardMouse
                    : ControlMapper.ControlMode.Simple;
            }
            else
            {
                ControlModeHidden = ControlModeVisible;
            }
        }

        private void SetDummyOutputs(int howMany, string dummyName, int dummySize)
        {
            int idx = 1;
            for (int i = OutputBranches - howMany; i < OutputBranches; ++i)
            {
                MyMemoryBlock<float> mb = MyMemoryManager.Instance.CreateMemoryBlock<float>(this);
                mb.Name = dummyName + idx++;
                mb.Count = dummySize;
                m_outputs[i] = mb;
            }
        }

        private void AddOutputs(int branchesToAdd, string dummyName, int dummySize = 1)
        {
            int oldOutputBranches = OutputBranches;
            // backup current state of memory blocks -- setting value to OutputBranches will reset m_outputs
            List<MyAbstractMemoryBlock> backup = new List<MyAbstractMemoryBlock>();
            for (int i = 0; i < oldOutputBranches; ++i)
                backup.Add(m_outputs[i]);

            OutputBranches = oldOutputBranches + branchesToAdd;

            for (int i = 0; i < oldOutputBranches; ++i)
                m_outputs[i] = backup[i];

            SetDummyOutputs(SignalCount, dummyName, dummySize);
        }

        /// <summary>
        /// Returns Signal node with given index (from 0 to SignalCount)
        /// </summary>
        /// <param name="index">Index of Signal node</param>
        /// <returns></returns>
        public MyParentInput GetSignalNode(int index)
        {
            int offset = OutputBranches - SignalCount;
            return Owner.Network.GroupInputNodes[offset + index];
        }

        /// <summary>
        /// Returns memory block assigned to Signal with given index (from 0 to SignalCount)
        /// </summary>
        /// <param name="index">Index of Signal</param>
        /// <returns></returns>
        public MyMemoryBlock<float> GetSignalMemoryBlock(int index)
        {
            int offset = OutputBranches - SignalCount;
            return (m_outputs[offset + index] as MyMemoryBlock<float>);
            //return GetSignalNode(index).Output;
        }
    }
}
