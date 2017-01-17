﻿using GoodAI.Modules.School.Common;
using GoodAI.Modules.School.Worlds;
using System;
using System.ComponentModel;
using System.Drawing;

namespace GoodAI.Modules.School.LearningTasks
{
    /// <summary>Visual object for comparison. Similar to the Shape class.</summary>
    public class ComparisonShape : GameObject
    {
        // The shapes used for the targets
        public enum Shapes { L, T, Circle, Rhombus, Mountains, Count }

        // The shape of the object
        public Shapes Shape { get; protected set; }

        // Construct the object
        public ComparisonShape(Shapes shape, PointF location = default(PointF), SizeF size = default(SizeF), float rotation = 0)
            : base(GetShapeAddr(shape), location, size, rotation: rotation)
        {
            Shape = shape;
        }

        // Get the image corresponding to the shape
        public static string GetShapeAddr(Shapes shape)
        {
            switch (shape)
            {
                case Shapes.L:
                    return @"WhiteL50x50.png";
                case Shapes.T:
                    return @"WhiteT50x50.png";
                case Shapes.Circle:
                    return @"WhiteCircle50x50.png";
                case Shapes.Rhombus:
                    return @"WhiteRhombus50x50.png";
                case Shapes.Mountains:
                    return @"WhiteMountains50x50.png";
            }
            throw new ArgumentException("Unknown shape");
        }

        // Get a random shape
        public static Shapes GetRandomShape(Random rndGen, int numberOfShapes)
        {
            if (numberOfShapes > (int)Shapes.Count)
            {
                throw new ArgumentException("Not Enought Shapes.");
            }
            return (Shapes)rndGen.Next(numberOfShapes);
        }

        // Test for shape equivalence
        public bool IsSameShape(ComparisonShape otherShape)
        {
            return Shape == otherShape.Shape;
        }
    }

    /// <summary>
    /// The learning task tests the comparison of simple objects.
    /// Two shapes are presented; they are considered equivalent if they differ
    /// only in color, translation, scaling, and rotation.
    /// </summary>
    [DisplayName("Detect similar tranasformed objects")]
    public class LTVisualEquivalence : AbstractLearningTask<RoguelikeWorld>
    {
        // Attributes
        protected readonly TSHintAttribute NUMBER_OF_DIFFERENT_OBJECTS = new TSHintAttribute("Number of different objects", "", typeof(int), 1, 4);
        protected readonly TSHintAttribute ROTATE_SHAPE = new TSHintAttribute("Rotate shape", "", typeof(bool), 0, 1);
        protected readonly TSHintAttribute SCALE_SHAPE = new TSHintAttribute("Scale shape", "", typeof(bool), 0, 1);

        // Random generator
        private static readonly Random m_rand = new Random();

        // How large a share of the examples are guaranteed to be positive -- to maintain a balance between negative and positive examples
        protected const float MIN_SHARE_OF_POSITIVE_EXAMPLES = .3f;

        // First shape
        protected ComparisonShape targetA;

        // Second shape
        protected ComparisonShape targetB;

        public LTVisualEquivalence() : this(null) { }

        public LTVisualEquivalence(SchoolWorld w)
            : base(w)
        {
            TSHints = new TrainingSetHints {
                {TSHintAttributes.IMAGE_NOISE, 0},
                { TSHintAttributes.MAX_NUMBER_OF_ATTEMPTS, 10000 },
                { NUMBER_OF_DIFFERENT_OBJECTS, 2 },
                { ROTATE_SHAPE, 0 },
                { SCALE_SHAPE, 0 }
            };

            TSProgression.Add(TSHints.Clone());
            TSProgression.Add(NUMBER_OF_DIFFERENT_OBJECTS, 3);
            TSProgression.Add(SCALE_SHAPE, 1);
            TSProgression.Add(ROTATE_SHAPE, 1);
            TSProgression.Add(NUMBER_OF_DIFFERENT_OBJECTS, 5);
        }

        public override void PresentNewTrainingUnit()
        {
            WrappedWorld.CreateNonVisibleAgent();
            targetA = CreateTarget(ComparisonShape.GetRandomShape(m_rand, (int)TSHints[NUMBER_OF_DIFFERENT_OBJECTS]));
            if (GuaranteePositiveExample())
            {
                targetB = CreateTarget(targetA.Shape);
            }
            else
            {
                targetB = CreateTarget(ComparisonShape.GetRandomShape(m_rand, (int)TSHints[NUMBER_OF_DIFFERENT_OBJECTS]));
            }
        }

        protected override bool DidTrainingUnitComplete(ref bool wasUnitSuccessful)
        {
            wasUnitSuccessful = targetA.IsSameShape(targetB) == DoesAgentSayShapesAreSame();
            return true;
        }

        private bool DoesAgentSayShapesAreSame()
        {
            return SchoolWorld.ActionInput.Host[0] == 1;
        }

        // Create a shape
        protected ComparisonShape CreateTarget(ComparisonShape.Shapes shape)
        {
            SizeF size = GetSize(TSHints[SCALE_SHAPE] == 1);
            PointF location = GetRandomLocation(size);
            float rotation = GetRotation(TSHints[ROTATE_SHAPE] == 1);
            ComparisonShape target = new ComparisonShape(shape, location, size, rotation);
            WrappedWorld.AddGameObject(target);
            return target;
        }

        // True if the next example should be (guaranteed to be) positive
        private bool GuaranteePositiveExample()
        {
            return m_rand.NextDouble() <= MIN_SHARE_OF_POSITIVE_EXAMPLES;
        }

        // Get a target size
        private SizeF GetSize(bool doScaleShape)
        {
            SizeF size = new Size(32, 32);
            if (doScaleShape)
            {
                const float MIN_SCALE_FACTOR = 1;
                const float MAX_SCALE_FACTOR = 2;
                float scale = GetRandom(MIN_SCALE_FACTOR, MAX_SCALE_FACTOR);
                size.Width = (float)Math.Round(size.Width * scale);
                size.Height = (float)Math.Round(size.Height * scale);
            }
            return size;
        }

        // Get target rotation
        private float GetRotation(bool doRotateShape)
        {
            return doRotateShape ? GetRandom(0, 360) : 0;
        }

        // Return a random number lowerBound <= r < upperBound
        private float GetRandom(float lowerBound, float upperBound)
        {
            return (float)m_rand.NextDouble() * (upperBound - lowerBound) + lowerBound;
        }

        // Get a target location
        private PointF GetRandomLocation(SizeF size)
        {
            // Need to separate objects enough so that even large objects do not touch when
            // rotated.
            const int OBJECT_MARGIN = 10;
            return WrappedWorld.RandomPositionInsidePowNonCovering(m_rand, size, OBJECT_MARGIN);
        }
    }
}
