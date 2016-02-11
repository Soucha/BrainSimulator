﻿using GoodAI.Core.Execution;
using GoodAI.Core.Memory;
using GoodAI.Core.Nodes;
using GoodAI.Core.Task;
using GoodAI.Core.Utils;
using GoodAI.Modules.School.Common;
using GoodAI.Platform.Core.Nodes;
using GoodAI.TypeMapping;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using YAXLib;

namespace GoodAI.Modules.School.Worlds
{

    public class SchoolWorld : MyWorld, IModelChanger, IMyCustomExecutionPlanner
    {
        #region Constants
        // Constants defining the memory layout of LTStatus information
        private const int NEW_LT_FLAG = 0;
        private const int NEW_LEVEL_FLAG = NEW_LT_FLAG + 1;
        private const int NEW_TU_FLAG = NEW_LEVEL_FLAG + 1;
        private const int LT_IDENTIFIER = NEW_TU_FLAG + 1;
        private const int LEVEL_INDEX = LT_IDENTIFIER + 1;
        private const int TU_INDEX = LEVEL_INDEX + 1;
        private const int LT_STATUS_COUNT = TU_INDEX + 1;
        #endregion

        #region Input and Output MemoryBlocks
        [MyInputBlock]
        public MyMemoryBlock<float> ActionInput
        {
            get { return GetInput(0); }
        }

        [MyOutputBlock(0)]
        public MyMemoryBlock<float> Visual
        {
            get { return GetOutput(0); }
            set { SetOutput(0, value); }
        }

        [MyOutputBlock(1)]
        public MyMemoryBlock<float> Text
        {
            get { return GetOutput(1); }
            set { SetOutput(1, value); }
        }

        [MyOutputBlock(2)]
        public MyMemoryBlock<float> Data
        {
            get { return GetOutput(2); }
            set { SetOutput(2, value); }
        }

        [MyOutputBlock(3)]
        public MyMemoryBlock<float> DataLength
        {
            get { return GetOutput(3); }
            set { SetOutput(3, value); }
        }

        [MyOutputBlock(4)]
        public MyMemoryBlock<float> Reward
        {
            get { return GetOutput(4); }
            set { SetOutput(4, value); }
        }

        // Memory block informing the agent of changes in learning task,
        // level, and training unit.
        //
        // Consists of 
        // - flags signifying a new task, level, and unit, respectively
        // - numbers identifying the current task, level, and unit
        [MyOutputBlock(5)]
        public MyMemoryBlock<float> LTStatus
        {
            get { return GetOutput(5); }
            set { SetOutput(5, value); }
        }



        #endregion

        #region MemoryBlocks sizes
        [MyBrowsable, Category("World Sizes")]
        [YAXSerializableField(DefaultValue = 65536)] // 196608 768 * 256
        public int VisualSize { get; set; }

        [MyBrowsable, Category("World Sizes")]
        [YAXSerializableField(DefaultValue = 1000)]
        public int TextSize { get; set; }

        [MyBrowsable, Category("World Sizes")]
        [YAXSerializableField(DefaultValue = 100)]
        public int DataSize { get; set; }

        public override void UpdateMemoryBlocks()
        {
            Visual.Count = VisualSize;
            Text.Count = TextSize;
            Data.Count = DataSize;
            DataLength.Count = 1;
            Reward.Count = 1;
            LTStatus.Count = LT_STATUS_COUNT;
        }
        #endregion

        #region World machinery
        // for serialization purposes
        private string CurrentWorldType
        {
            get
            {
                if (CurrentWorld != null)
                {
                    return CurrentWorldType.ToString();
                }
                else
                {
                    return String.Empty;
                }
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    CurrentWorld = null;
                }
                else
                {
                    CurrentWorld = (IWorldAdapter)Type.GetType(value);
                }
            }
        }


        [YAXSerializableField, YAXSerializeAs("CurrentWorld"), YAXCustomSerializer(typeof(WorldAdapterSerializer))]
        private IWorldAdapter m_currentWorld;

        private bool m_switchModel = true;

        [MyBrowsable, Category("World"), TypeConverter(typeof(IWorldAdapterConverter))]
        public IWorldAdapter CurrentWorld
        {
            get
            {
                return m_currentWorld;
            }
            set
            {
                // TODO m_currentWorld Init memory of wrapped world
                m_switchModel = true;
                m_currentWorld = value;
                (m_currentWorld as MyWorld).UpdateMemoryBlocks();
                m_currentWorld.InitAdapterMemory(this);
            }
        }

        public override void UpdateAfterDeserialization()
        {
            CurrentWorld = m_currentWorld;
        }
        #endregion

        public SchoolWorld()
        {
            Visual.Metadata[MemoryBlockMetadataKeys.RenderingMethod] = RenderingMethod.Raw.ToString();
        }

        Random m_rndGen = new Random();

        public SchoolCurriculum Curriculum { get; set; }
        ILearningTask m_currentLearningTask;

        // The curriculum to use.
        [MyBrowsable, Category("Curriculum"), Description("Choose which type of curriculum you want to use.")]
        [YAXSerializableField(DefaultValue = CurriculumType.TrainingCurriculum)]
        public CurriculumType TypeOfCurriculum { get; set; }

        // For testing the progression of learning tasks when we don't have an agent or
        // available agents can't complete the task, we can emulate training unit success
        // with the probability set by this parameter.
        [MyBrowsable, Category("World"), Description("Set to 0 < p <= 1 to emulate the success (with probability p) of training units.")]
        [YAXSerializableField(DefaultValue = 0)]
        public float EmulatedUnitSuccessProbability { get; set; }


        public override void Validate(MyValidator validator)
        {
            validator.AssertError(ActionInput != null, this, "ActionInput must not be null");
        }

        public MyNode AffectedNode { get { return this; } }

        public bool ChangeModel(IModelChanges changes)
        {
            if (!m_switchModel)
                return false;

            (CurrentWorld as MyWorld).EnableDefaultTasks();
            changes.AddNode(CurrentWorld as MyWorld);
            m_switchModel = false;
            return true;
        }

        public virtual MyExecutionBlock CreateCustomInitPhasePlan(MyExecutionBlock defaultInitPhasePlan)
        {
            var executionPlanner = TypeMap.GetInstance<IMyExecutionPlanner>();

            MyExecutionBlock plan = executionPlanner.CreateNodeExecutionPlan(CurrentWorld as MyWorld, true);

            return new MyExecutionBlock(new IMyExecutable[] { defaultInitPhasePlan, plan });
        }

        public virtual MyExecutionBlock CreateCustomExecutionPlan(MyExecutionBlock defaultPlan)
        {
            var executionPlanner = TypeMap.GetInstance<IMyExecutionPlanner>();

            IMyExecutable[] thisWorldTasks = defaultPlan.Children;

            var blocks = new List<IMyExecutable>();
            // The default plan will only contain one block with: signals in, world tasks, signals out.
            blocks.Add(thisWorldTasks[0]);
            blocks.Add(thisWorldTasks[1]);
            blocks.Add(executionPlanner.CreateNodeExecutionPlan(CurrentWorld as MyWorld, false));
            blocks.AddRange(thisWorldTasks.Skip(2));

            return new MyExecutionBlock(blocks.ToArray());
        }

        public void ExecuteCurriculumStep()
        {
            ResetLTStatusFlags();

            // first evaluate previus step
            if (m_currentLearningTask != null)
            {
                // 
                m_currentLearningTask.ExecuteStep();
                // set new level, training unit or step
                // this also partially sets LTStatus
                m_currentLearningTask.EvaluateStep();
            }
            else
            {
                NotifyNewLearningTask();
            }

            // set new learning task or stop simulation
            if(LTStatus.Host[NEW_LT_FLAG] == 1)
            {
                if(!InitNewLearningTask())
                {
                    // try stop simulation
                    if (Owner.SimulationHandler.CanPause)
                    {
                        Owner.SimulationHandler.PauseSimulation();
                        return;
                    }
                }
            }

            // if new TU is requested, present new training unit
            if (LTStatus.Host[NEW_TU_FLAG] == 1)
            {
                m_currentLearningTask.SchoolWorld.ClearWorld();
                m_currentLearningTask.PresentNewTrainingUnit();
            }

            // LTStatus should be complete in this moment
            LTStatus.SafeCopyToDevice();
        }

        private bool InitNewLearningTask()
        {
            m_currentLearningTask = Curriculum.GetNextLearningTask();

            // end of curriculum - there are no more LTs
            if (m_currentLearningTask == null)
            {
                return false;
            }
            // inform user about new LT
            MyLog.Writer.WriteLine(MyLogLevel.INFO,
                "Switching to LearningTask: " +
                m_currentLearningTask.GetTypeName());

            m_currentLearningTask.Init();

            return true;
        }

        // Notify of the start of a new curriculum
        private void NotifyNewCurriculum()
        {
            // Will be incremented when LT is presented
            LTStatus.Host[LT_IDENTIFIER] = -1;
        }

        public void ResetLTStatusFlags()
        {
            LTStatus.Host[NEW_LT_FLAG] = 0;
            LTStatus.Host[NEW_LEVEL_FLAG] = 0;
            LTStatus.Host[NEW_TU_FLAG] = 0;
        }

        // Notify of the start of a new learning task
        public void NotifyNewLearningTask()
        {
            LTStatus.Host[NEW_LT_FLAG] = 1;
            LTStatus.Host[LT_IDENTIFIER]++;
            LTStatus.Host[TU_INDEX] = 0;
            LTStatus.Host[LEVEL_INDEX] = 0;
        }

        public void NotifyNewLevel()
        {
            LTStatus.Host[NEW_LEVEL_FLAG] = 1;
            LTStatus.Host[LEVEL_INDEX]++;
            LTStatus.Host[TU_INDEX] = 0;
        }

        public void NotifyNewTrainingUnit()
        {
            LTStatus.Host[NEW_TU_FLAG] = 1;
            LTStatus.Host[TU_INDEX]++;
        }

        public void InitializeCurriculum()
        {
            Curriculum = SchoolCurriculumPlanner.GetCurriculumForWorld(this);
            m_currentLearningTask = null;
            NotifyNewCurriculum();
        }

        public void ClearWorld()
        {
            CurrentWorld.ClearWorld();
        }

        public void SetHints(TrainingSetHints trainingSetHints)
        {
            foreach (var kvp in trainingSetHints)
            {
                CurrentWorld.SetHint(kvp.Key, kvp.Value);
            }
        }

        // Return true if we are emulating the success of training units
        public bool IsEmulatingUnitCompletion()
        {
            return EmulatedUnitSuccessProbability > 0;
        }

        // Emulate the successful completion with a specified probability of the current training unit
        public bool EmulateIsTrainingUnitCompleted(out bool wasUnitSuccessful)
        {
            wasUnitSuccessful = m_rndGen.NextDouble() < EmulatedUnitSuccessProbability;
            return wasUnitSuccessful;
        }

        // Emulate the successful completion with a specified probability of the current training unit
        public bool EmulateIsTrainingUnitCompleted()
        {
            return m_rndGen.NextDouble() < EmulatedUnitSuccessProbability;
        }

        public InitSchoolWorldTask InitSchool { get; protected set; }
        public InputAdapterTask AdapterInputStep { get; protected set; }
        public OutputAdapterTask AdapterOutputStep { get; protected set; }
        public LearningStepTask LearningStep { get; protected set; }

        /// <summary>
        /// Initialize the world's curriculum
        /// </summary>
        [MyTaskInfo(OneShot = true)]
        public class InitSchoolWorldTask : MyTask<SchoolWorld>
        {
            public override void Init(int nGPU)
            {
            }

            public override void Execute()
            {
                Owner.InitializeCurriculum();
            }
        }

        /// <summary>
        /// Performs Output memory blocks mapping
        /// </summary>
        public class OutputAdapterTask : MyTask<SchoolWorld>
        {
            public override void Init(int nGPU)
            {
                Owner.CurrentWorld.InitWorldOutputs(nGPU, Owner);
            }

            public override void Execute()
            {
                Owner.CurrentWorld.MapWorldOutputs(Owner);
            }
        }

        /// <summary>
        /// Performs Input memory blocks mapping
        /// </summary>
        public class InputAdapterTask : MyTask<SchoolWorld>
        {
            public override void Init(int nGPU)
            {
                Owner.CurrentWorld.InitWorldInputs(nGPU, Owner);
            }

            public override void Execute()
            {
                Owner.CurrentWorld.MapWorldInputs(Owner);
            }
        }

        /// <summary>
        /// Update the state of the training task(s)
        /// </summary>
        public class LearningStepTask : MyTask<SchoolWorld>
        {
            public override void Init(int nGPU)
            {

            }

            public override void Execute()
            {
                Owner.ExecuteCurriculumStep();
            }
        }

    }
}
