﻿using GoodAI.Modules.School.Common;
using GoodAI.Modules.School.Worlds;
using System;
using System.ComponentModel;
using GoodAI.Core.Utils;
using GoodAI.School.Learning_tasks;

namespace GoodAI.Modules.School.LearningTasks
{
    [DisplayName("SC D1 LT2 - 2 shapes")]
    public class Ltsct2 : Ltsct1
    {
        public override string Path
        {
            get { return @"D:\summerCampSamples\D1\SCT2\"; }
        }

        public Ltsct2() : this(null) { }

        public Ltsct2(SchoolWorld w)
            : base(w)
        {
        }

        protected override void CreateScene()
        {
            Actions = new AvatarsActions(true,false,false,false);

            int randomLocationIdx = RndGen.Next(ScConstants.numPositions);

            if (RndGen.Next(ScConstants.numShapes + 1) > 0)
            {
                AddShape(randomLocationIdx);
                Actions.Shapes[ShapeIndex] = true;
            }


            int nextRandomLocationIdx = RndGen.Next(randomLocationIdx + 1, randomLocationIdx + ScConstants.numPositions);
            nextRandomLocationIdx %= ScConstants.numPositions;

            if (RndGen.Next(ScConstants.numShapes + 1) > 0)
            {
                AddShape(nextRandomLocationIdx);
                Actions.Shapes[ShapeIndex] = true;
            }

            WriteActions();
        }
    }
}
