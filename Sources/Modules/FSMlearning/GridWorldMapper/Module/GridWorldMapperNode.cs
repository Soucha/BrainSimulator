﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoodAI.Core.Memory;
using GoodAI.Core.Nodes;
using GoodAI.Core.Task;
using GoodAI.Core.Utils;
using YAXLib;

namespace MyCompany.Modules.GridWorldMapperModule
{
    /// <author>Michal Soucha</author>
    /// <meta></meta>
    /// <status>TEST</status>
    /// <summary>A summary of the node's function</summary>
    /// <description>A more detailed description.</description>
    public class GridWorldMapperNode : MyWorkingNode
    {
        [MyInputBlock(0)]
        public MyMemoryBlock<int> ActionInput
        {
            get { return GetInput<int>(0); }
        }

        [MyInputBlock(1)]
        public MyMemoryBlock<float> WorldVariables
        {
            get { return GetInput(1); }
        }

        [MyOutputBlock(0)]
        public MyMemoryBlock<float> ActionOutput
        {
            get { return GetOutput(0); }
            set { SetOutput(0, value); }
        }

        [MyOutputBlock(1)]
        public MyMemoryBlock<int> WorldResponse
        {
            get { return GetOutput<int>(1); }
            set { SetOutput<int>(1, value); }
        }

        [MyBrowsable, Category("Parameters")]
        [YAXSerializableField(DefaultValue = true), YAXElementFor("Parameters")]
        public bool ParameterProperty { get; set; }

        [MyBrowsable, Category("Parameters")]
        [YAXSerializableField(DefaultValue = 6)]
        public int ActionCount { get; set; }// = 6;

        public override void UpdateMemoryBlocks()
        {
            ActionOutput.Count = ActionCount;
            WorldResponse.Count = 1;
        }

        public override void Validate(MyValidator validator)
        {
            base.Validate(validator);

            validator.AssertError(ActionInput.Count == 1, this, "Action input must be one float.");
        }

        public GridWorldMapperTask NewModuleTask { get; private set; }
    }

    /// <summary>
    /// The task that pushes the current simulation step into the output block.
    /// </summary>
    public class GridWorldMapperTask : MyTask<GridWorldMapperNode>
    {
        private List<int> lastActions = new List<int>();
        private int state;
        private int resetAction;
        private float agentPosX, agentPosY;

        public override void Init(int nGPU)
        {
            lastActions.Add(0);
            state = 0;
            resetAction = 0;
            Owner.WorldVariables.SafeCopyToHost();
            agentPosX = Owner.WorldVariables.Host[0];
            agentPosY = Owner.WorldVariables.Host[1];

            Owner.WorldResponse.SafeCopyToHost();
            Owner.WorldResponse.Host[0] = -1;
            Owner.WorldResponse.SafeCopyToDevice();
        }

        public override void Execute()
        {
            if (state == 0)
            {
                Owner.ActionOutput.SafeCopyToHost();
                Owner.ActionOutput.Host[lastActions[lastActions.Count - 1]] = 0;
                Owner.ActionOutput.SafeCopyToDevice();
                state = 1;
            }
            else if (state == 1)
            {
                Owner.WorldVariables.SafeCopyToHost();
                int output = 0;
                if (lastActions[lastActions.Count - 1] > 0 && lastActions[lastActions.Count - 1] < 5) // move
                {
                    if (agentPosX == Owner.WorldVariables.Host[0] && agentPosY == Owner.WorldVariables.Host[1])
                    {
                        output = -2;
                        lastActions.RemoveAt(lastActions.Count - 1);
                    }
                }
                agentPosX = Owner.WorldVariables.Host[0];
                agentPosY = Owner.WorldVariables.Host[1];
                if (output == 0)
                {
                    for (int i = 3; i < Owner.WorldVariables.Host.Length; i += 3)
                    {
                        if (agentPosX == Owner.WorldVariables.Host[i] && agentPosY == Owner.WorldVariables.Host[i + 1])
                        {
                            output = i / 3;
                            break;
                        }
                    }
                }
                Owner.WorldResponse.SafeCopyToHost();
                Owner.WorldResponse.Host[0] = output;
                Owner.WorldResponse.SafeCopyToDevice();
                state = 2;
            }
            else if (state == 2)
            {
                Owner.WorldResponse.SafeCopyToHost();
                Owner.WorldResponse.Host[0] = -1;
                Owner.WorldResponse.SafeCopyToDevice();
                state = 3;
            }
            else if (state == 3) 
            {
                Owner.ActionInput.SafeCopyToHost();
                //int action = (int)Math.Round(Owner.ActionInput.Host[0]);
                int action = Owner.ActionInput.Host[0];
                if (action < -1 || action > 5)
                {
                    MyLog.WARNING.WriteLine("Action needs to be in range [-1, 5]");
                    return;
                }
                if (action == -1) { // reset
                    state = -1;
                    resetAction = 0;
                }
                else {
                    Owner.ActionOutput.SafeCopyToHost();
                    Owner.ActionOutput.Host[action] = 1;
                    Owner.ActionOutput.SafeCopyToDevice();    
                    lastActions.Add(action);
                    state = 0;
                }
                
            }
            else // reset
            {
                Owner.ActionOutput.SafeCopyToHost();
                Owner.ActionOutput.Host[resetAction] = 0;
                if (lastActions.Count == 1)
                {
                    state = 0;
                }
                else
                {
                    resetAction = lastActions[lastActions.Count - 1];
                    switch (resetAction)
                    {
                        case 1: resetAction = 2; break;
                        case 2: resetAction = 1; break;
                        case 3: resetAction = 4; break;
                        case 4: resetAction = 3; break;
                    }
                    Owner.ActionOutput.Host[resetAction] = 1;
                    lastActions.RemoveAt(lastActions.Count - 1);
                }
                Owner.ActionOutput.SafeCopyToDevice();
            }
            
        }
    }
}