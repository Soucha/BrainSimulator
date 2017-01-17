﻿using GoodAI.Modules.School.Common;
using GoodAI.Modules.School.Worlds;
using System;
using System.ComponentModel;
using System.Drawing;
using GoodAI.School.Learning_tasks;

namespace GoodAI.Modules.School.LearningTasks
{
    [DisplayName("SC D1 LT3 - 1 color - fixed position")]
    public class Ltsct3Fp : Ltsct1
    {
        public override string Path
        {
            get { return @"D:\summerCampSamples\D1\SCT3FP\"; }
        }

        public Ltsct3Fp() : this(null) { }

        public Ltsct3Fp(SchoolWorld w)
            : base(w)
        {
            RndGen = new Random(3); // to avoid generating same data as in case of the first task
        }

        public override void InitCheckTable()
        {
            GenerationsCheckTable = new bool[ScConstants.numPositions + 1][];

            for (int i = 0; i < GenerationsCheckTable.Length; i++)
            {
                GenerationsCheckTable[i] = new bool[ScConstants.numColors];
            }
        }

        protected override void CreateScene()
        {
            Actions = new AvatarsActions(false, true, false, false);

            if (RndGen.Next(ScConstants.numShapes + 1) > 0)
            {
                const int fixedLocationIndex = 4;
                AddShape(fixedLocationIndex);
                Actions.Colors[ColorIndex] = true;
            }

            WriteActions();
        }

        protected override void AddShape(int randomLocationIndex)
        {
            SizeF size = new SizeF(WrappedWorld.GetPowGeometry().Width / 4, WrappedWorld.GetPowGeometry().Height / 4);

            Color color = Colors.GetRandomColor(RndGen, out ColorIndex);

            PointF location = Positions.Positions[randomLocationIndex];

            ShapeIndex = RndGen.Next(ScConstants.numShapes);
            Shape.Shapes randomShape = (Shape.Shapes)ShapeIndex;

            WrappedWorld.CreateShape(randomShape, color, location, size);

            GenerationsCheckTable[randomLocationIndex][ColorIndex] = true;
        }
    }
}
