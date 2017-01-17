﻿using GoodAI.Modules.School.Common;
using GoodAI.Modules.School.Worlds;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace GoodAI.Modules.School.LearningTasks
{
    [DisplayName("Categorize to arbitrary sets")]
    public class LTClassComposition : AbstractLearningTask<RoguelikeWorld>
    {
        private static readonly TSHintAttribute IS_TARGET_MOVING = new TSHintAttribute("Is target moving", "", typeof(bool), 0, 1); //check needed;

        protected Random m_rndGen = new Random();
        protected bool m_positiveExamplePlaced;

        //private int m_numberOfObjects;
        private List<Shape.Shapes> m_positiveExamples = new List<Shape.Shapes>();
        private List<Shape.Shapes> m_negativeExamples = new List<Shape.Shapes>();

        public LTClassComposition() : this(null) { }

        public LTClassComposition(SchoolWorld w)
            : base(w)
        {
            TSHints = new TrainingSetHints {
                {TSHintAttributes.IMAGE_NOISE, 0},
                {TSHintAttributes.IS_VARIABLE_POSITION, 0},
                {TSHintAttributes.IS_VARIABLE_COLOR, 0},
                {TSHintAttributes.IS_VARIABLE_SIZE, 0},
                {TSHintAttributes.NUMBER_OF_DIFFERENT_OBJECTS, 4f},
                {TSHintAttributes.MAX_NUMBER_OF_ATTEMPTS, 10000}
            };

            TSProgression.Add(TSHints.Clone());

            TSProgression.Add(TSHintAttributes.IS_VARIABLE_COLOR, 1.0f);
            TSProgression.Add(TSHintAttributes.IS_VARIABLE_POSITION, 1.0f);
            TSProgression.Add(TSHintAttributes.IMAGE_NOISE, 1);
            TSProgression.Add(TSHintAttributes.IS_VARIABLE_SIZE, 1.0f);
            TSProgression.Add(TSHintAttributes.NUMBER_OF_DIFFERENT_OBJECTS, 6f);
            TSProgression.Add(TSHintAttributes.NUMBER_OF_DIFFERENT_OBJECTS, 8f);

            m_positiveExamples.Add(Shape.Shapes.Star);
            m_positiveExamples.Add(Shape.Shapes.Circle);
            m_positiveExamples.Add(Shape.Shapes.T);
            m_positiveExamples.Add(Shape.Shapes.Tent);
            m_positiveExamples.Add(Shape.Shapes.Mountains);

            m_negativeExamples.Add(Shape.Shapes.DoubleRhombus);
            m_negativeExamples.Add(Shape.Shapes.Pentagon);
            m_negativeExamples.Add(Shape.Shapes.Rhombus);
            m_negativeExamples.Add(Shape.Shapes.Square);
            m_negativeExamples.Add(Shape.Shapes.Triangle);
        }

        public override void PresentNewTrainingUnit()
        {
            int numberOfObjects = (int)TSHints[TSHintAttributes.NUMBER_OF_DIFFERENT_OBJECTS];
            List<Shape.Shapes> positiveExamplesRed = new List<Shape.Shapes>();
            List<Shape.Shapes> negativeExamplesRed = new List<Shape.Shapes>();
            for (int i = 0; i < numberOfObjects / 2; i++)
            {
                positiveExamplesRed.Add(m_positiveExamples[i]);
                negativeExamplesRed.Add(m_negativeExamples[i]);
            }

            WrappedWorld.CreateNonVisibleAgent();

            SizeF size;
            float standardSideSize = WrappedWorld.Viewport.Width / 10;
            if (TSHints[TSHintAttributes.IS_VARIABLE_SIZE] >= 1.0f)
            {
                float a = (float)(standardSideSize + m_rndGen.NextDouble() * standardSideSize);
                size = new SizeF(a, a);
            }
            else
            {
                size = new SizeF(standardSideSize, standardSideSize);
            }

            Color color;
            if (TSHints[TSHintAttributes.IS_VARIABLE_COLOR] >= 1.0f)
            {
                color = LearningTaskHelpers.RandomVisibleColor(m_rndGen);
            }
            else
            {
                color = Color.White;
            }

            PointF position;
            if (TSHints[TSHintAttributes.IS_VARIABLE_POSITION] >= 1.0f)
            {
                position = WrappedWorld.RandomPositionInsideViewport(m_rndGen, size);
            }
            else
            {
                position = WrappedWorld.Agent.GetGeometry().Location;
            }

            m_positiveExamplePlaced = LearningTaskHelpers.FlipCoin(m_rndGen);

            Shape.Shapes shape;
            if (m_positiveExamplePlaced)
            {
                int randShapePointer = m_rndGen.Next(0, positiveExamplesRed.Count);
                shape = positiveExamplesRed[randShapePointer];
            }
            else
            {
                int randShapePointer = m_rndGen.Next(0, negativeExamplesRed.Count);
                shape = negativeExamplesRed[randShapePointer];
            }

            WrappedWorld.CreateShape(shape, color, position, size);
        }

        protected override bool DidTrainingUnitComplete(ref bool wasUnitSuccessful)
        {
            if (m_positiveExamplePlaced == (WrappedWorld.Controls.Host[0] != 0))
            {
                wasUnitSuccessful = true;
            }
            else
            {
                wasUnitSuccessful = false;
            }
            return true;
        }
    }
}
