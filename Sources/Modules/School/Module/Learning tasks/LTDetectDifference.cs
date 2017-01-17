﻿using GoodAI.Modules.School.Common;
using GoodAI.Modules.School.Worlds;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace GoodAI.Modules.School.LearningTasks
{
    [DisplayName("Detect discrepancy in set")]
    public class LTDetectDifference : AbstractLearningTask<RoguelikeWorld>
    {
        protected readonly Random m_rndGen = new Random();
        protected bool m_diffObjectetPlaced;

        public LTDetectDifference() : this(null) { }

        public LTDetectDifference(SchoolWorld w)
            : base(w)
        {
            TSHints = new TrainingSetHints {
                {TSHintAttributes.IMAGE_NOISE, 0},
                {TSHintAttributes.IS_VARIABLE_COLOR, 0},
                {TSHintAttributes.IS_VARIABLE_SIZE, 0},
                {TSHintAttributes.NUMBER_OBJECTS, 2},
                {TSHintAttributes.MAX_NUMBER_OF_ATTEMPTS, 10000}
            };

            TSProgression.Add(TSHints.Clone());
            TSProgression.Add(TSHintAttributes.IMAGE_NOISE, 1);
            TSProgression.Add(TSHintAttributes.NUMBER_OBJECTS, 4f);
            TSProgression.Add(TSHintAttributes.IS_VARIABLE_COLOR, 1f);
            TSProgression.Add(TSHintAttributes.NUMBER_OBJECTS, 8f);
            TSProgression.Add(TSHintAttributes.IS_VARIABLE_SIZE, 1f);
            TSProgression.Add(TSHintAttributes.NUMBER_OBJECTS, 10f);
        }

        public override void PresentNewTrainingUnit()
        {
            WrappedWorld.CreateNonVisibleAgent();

            int numberOfShapes = Enum.GetValues(typeof(Shape.Shapes)).Length;
            List<int> uniqueCouple = LearningTaskHelpers.UniqueNumbers(m_rndGen, 0, numberOfShapes, 2);
            Shape.Shapes standardShape = (Shape.Shapes)uniqueCouple[0];
            Shape.Shapes alternativeShape = (Shape.Shapes)uniqueCouple[1];

            int numberOfObjects = (int)TSHints[TSHintAttributes.NUMBER_OBJECTS];

            m_diffObjectetPlaced = m_rndGen.Next(2) == 0;
            bool placeDifferentObj = m_diffObjectetPlaced;

            for (int i = 0; i < numberOfObjects; i++)
            {
                SizeF size;
                if (TSHints[TSHintAttributes.IS_VARIABLE_SIZE] >= 1f)
                {
                    float a = (float)(10 + m_rndGen.NextDouble() * 10);
                    size = new SizeF(a, a);
                }
                else
                {
                    size = new Size(15, 15);
                }

                Color color;
                if (TSHints[TSHintAttributes.IS_VARIABLE_COLOR] >= 1f)
                {
                    color = LearningTaskHelpers.RandomVisibleColor(m_rndGen);
                }
                else
                {
                    color = Color.White;
                }

                PointF position = WrappedWorld.RandomPositionInsidePowNonCovering(m_rndGen, size);

                if (placeDifferentObj)
                {
                    placeDifferentObj = false;
                    WrappedWorld.CreateShape(alternativeShape, color, position, size);
                }
                else
                {
                    WrappedWorld.CreateShape(standardShape, color, position, size);
                }
            }
        }

        protected override bool DidTrainingUnitComplete(ref bool wasUnitSuccessful)
        {
            if (m_diffObjectetPlaced == (WrappedWorld.Controls.Host[0] > 0))
            {
                wasUnitSuccessful = true;
            }
            return true;
        }
    }
}
