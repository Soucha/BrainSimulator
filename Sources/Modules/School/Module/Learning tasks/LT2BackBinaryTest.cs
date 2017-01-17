﻿using GoodAI.Modules.School.Common;
using GoodAI.Modules.School.Worlds;
using System;
using System.ComponentModel;
using System.Drawing;

namespace GoodAI.Modules.School.LearningTasks
{
    [DisplayName("Detect objects position in timeseries")]
    public class LT2BackBinaryTest : AbstractLearningTask<ManInWorld>
    {
        protected Random m_rndGen = new Random();
        protected Shape m_target;
        protected Shape.Shapes m_target_type;

        private Shape[] m_lastNShapes;

        private const int N_BACK = 2;

        public LT2BackBinaryTest() : this(null) { }

        public LT2BackBinaryTest(SchoolWorld w)
            : base(w)
        {
            m_lastNShapes = new Shape[N_BACK + 1];

            TSHints = new TrainingSetHints {
                {TSHintAttributes.IMAGE_NOISE, 0},
                {TSHintAttributes.IS_VARIABLE_COLOR, 0},
                {TSHintAttributes.IS_VARIABLE_SIZE, 0},
                {TSHintAttributes.IS_VARIABLE_POSITION, 0},
                {TSHintAttributes.IS_VARIABLE_ROTATION, 0},
                {TSHintAttributes.NUMBER_OF_DIFFERENT_OBJECTS, 2},
                {TSHintAttributes.MAX_NUMBER_OF_ATTEMPTS, 10000}
            };

            TSProgression.Add(TSHints.Clone());
            TSProgression.Add(TSHintAttributes.IS_VARIABLE_COLOR, 1f);
            TSProgression.Add(TSHintAttributes.IMAGE_NOISE, 1f);
            TSProgression.Add(TSHintAttributes.IS_VARIABLE_SIZE, 1f);
            TSProgression.Add(TSHintAttributes.IS_VARIABLE_POSITION, 1f);
            TSProgression.Add(TSHintAttributes.IS_VARIABLE_ROTATION, 1f);
            //TSProgression.Add(TSHintAttributes.NUMBER_OF_DIFFERENT_OBJECTS, 3f);
        }

        public override void PresentNewTrainingUnit()
        {
            WrappedWorld.CreateNonVisibleAgent();

            //random size
            SizeF shapeSize = new SizeF(120, 120);
            PointF shapePosition = new PointF(WrappedWorld.Scene.Width / 2, WrappedWorld.Scene.Height / 2);
            Color shapeColor = Color.White;
            float rotation = 0;

            Shape nthShape = m_lastNShapes[m_lastNShapes.Length - 2];   // check N steps back, but current is not pushed, so it is N - 1
            if (nthShape != null && m_rndGen.NextDouble() < 0.5)
            {
                // with probability 0.5 copy the same
                shapeSize = new SizeF(nthShape.Size.Width, nthShape.Size.Height);
                shapePosition.X = nthShape.Position.X;
                shapePosition.Y = nthShape.Position.Y;
                shapeColor = nthShape.ColorMask;
                rotation = nthShape.Rotation;
                m_target_type = nthShape.ShapeType;
            }
            else
            {
                // with probability 0.5 create a random new one

                // generate random size
                if (TSHints[TSHintAttributes.IS_VARIABLE_SIZE] >= 1.0f)
                {
                    int side = m_rndGen.Next(60, 121);
                    shapeSize = new Size(side, side);
                }

                // random position
                shapePosition.X -= shapeSize.Width / 2;
                shapePosition.Y -= shapeSize.Height / 2;

                if (TSHints[TSHintAttributes.IS_VARIABLE_POSITION] >= 1.0f)
                {
                    shapePosition = WrappedWorld.RandomPositionInsideViewport(m_rndGen, shapeSize);
                }

                // random color
                if (TSHints[TSHintAttributes.IS_VARIABLE_COLOR] >= 1.0f)
                {
                    shapeColor = LearningTaskHelpers.RandomVisibleColor(m_rndGen);
                }

                // random rotation
                if (TSHints[TSHintAttributes.IS_VARIABLE_ROTATION] >= 1.0f)
                {
                    rotation = (float)(m_rndGen.NextDouble() * 360);
                }

                // random shape
                m_target_type = Shape.GetRandomShape(m_rndGen, (int)TSHints[TSHintAttributes.NUMBER_OF_DIFFERENT_OBJECTS]);
            }

            m_target = (Shape)WrappedWorld.CreateShape(m_target_type, shapeColor, shapePosition, shapeSize, rotation);

            Push(m_target);
        }

        private void Push(Shape pushedObject)
        {
            for (int i = m_lastNShapes.Length - 1; i > 0; i--)
            {
                m_lastNShapes[i] = m_lastNShapes[i - 1];
            }
            m_lastNShapes[0] = pushedObject;
        }

        protected override bool DidTrainingUnitComplete(ref bool wasUnitSuccessful)
        {
            bool shapeEqual = shapesEqual(m_lastNShapes[0], m_lastNShapes[m_lastNShapes.Length - 1]);    // shapes are equal

            bool responseYes = WrappedWorld.Controls.Host[0] >= 1;            // agent response >= 1, which means YES
            bool responseNo = WrappedWorld.Controls.Host[0] <= 0;             // agent response <= 0, which means NO

            if (
                (shapeEqual && responseYes)
                ||
                (!shapeEqual && responseNo))
            {
                wasUnitSuccessful = true;
            }
            else
            {
                wasUnitSuccessful = false;
            }
            return true;
        }

        private bool shapesEqual(Shape s1, Shape s2)
        {
            if (s1 == null && s2 == null)
            {
                return true;
            }
            else if (s1 == null || s2 == null)
            {
                return false;
            }

            bool shapeTypes = s1.ShapeType == s2.ShapeType;
            bool colors = s1.ColorMask == s2.ColorMask;
            bool positions = s1.Position.X == s2.Position.X && s1.Position.Y == s2.Position.Y;
            bool sizes = s1.Size.Width == s2.Size.Width && s1.Size.Height == s2.Size.Height;
            bool rotations = s1.Rotation == s2.Rotation;

            return shapeTypes && colors && positions && sizes && rotations;
        }
    }
}
