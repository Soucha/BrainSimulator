﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoodAI.Core.Execution;
using GoodAI.Core.Memory;
using GoodAI.Core.Nodes;
using GoodAI.Core.Task;
using GoodAI.Core.Utils;
using ManagedCuda.BasicTypes;
using YAXLib;

namespace GoodAI.Core.Nodes
{
    /// <author>GoodAI</author>
    /// <status>Working</status>
    /// <summary>DeviceInput node takes inputs from Keyboard.</summary>
    /// <description>The node has an output of size 1*256, the values are mapped from Keyboard device <br />  
    /// <b>Note:</b> To use inputs from Keyboard while the simulation is running, DeviceInput node needs to be selected in BrainSim <br />  <br /> 
    /// In "SchoolWorld", the environments "Pong", "Tetris", "ToyWorld" and "RogueLike" allow you to interact with the world by using the WASD Keyboard mapping (and Q+E keys for Tetris)
    /// </description>
    public class DeviceInput : MyWorkingNode
    {
        // The keyboard keys should fit into a short (add some fields for continuous inputs, like mouse).
        // The mapping should match: https://msdn.microsoft.com/en-us/library/windows/desktop/dd375731(v=vs.85).aspx
        private const int TotalOutputSize = 256;

        [MyBrowsable]
        [YAXSerializableField(DefaultValue = false)]
        public bool StepOnKeyDown { get; set; }

        private LinkedList<Tuple<int, bool>> m_keyPresses = new LinkedList<Tuple<int,bool>>();

        [MyOutputBlock(0)]
        public MyMemoryBlock<float> Output
        {
            get { return GetOutput(0); }
            set { SetOutput(0, value); }
        }

        public override void UpdateMemoryBlocks()
        {
            Output.Count = TotalOutputSize;
        }

        public void SetKeyUp(int keyValue)
        {
            m_keyPresses.AddLast(new Tuple<int, bool>(keyValue, false));
            ExecuteIfPaused();
        }

        public void SetKeyDown(int keyValue)
        {
            bool wasNotPressed = Output.Host[keyValue] < 1.0f; // valid only when simulation state is PAUSED, otherwise the result is undefined

            m_keyPresses.AddLast(new Tuple<int, bool>(keyValue, true));

            ExecuteIfPaused();

            MySimulationHandler handler = Owner.SimulationHandler;
            if (wasNotPressed && StepOnKeyDown && (handler.State == MySimulationHandler.SimulationState.PAUSED))
                handler.StartSimulation(1);
        }

        private void ExecuteIfPaused()
        {
            if (Owner.SimulationHandler.State == MySimulationHandler.SimulationState.PAUSED)
                ProcessInputTask.Execute();
        }

        private DeviceInputTask ProcessInputTask { get; set; }

        /// <summary>
        /// When key presses are detected, copies values to output
        /// </summary>
        public class DeviceInputTask : MyTask<DeviceInput>
        {
            public override void Init(int nGPU)
            {
            }

            public override void Execute()
            {
                while (Owner.m_keyPresses.Count > 0)
                {
                    Tuple<int, bool> keyPress = Owner.m_keyPresses.First.Value;
                    Owner.m_keyPresses.RemoveFirst();
                    Owner.Output.Host[keyPress.Item1] = keyPress.Item2 ? 1.0f : 0.0f;
                }
                Owner.Output.SafeCopyToDevice();
            }
        }
    }
}
