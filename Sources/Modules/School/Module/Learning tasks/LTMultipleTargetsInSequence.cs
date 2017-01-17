﻿using GoodAI.Modules.School.Common;
using GoodAI.Modules.School.Worlds;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace GoodAI.Modules.School.LearningTasks
{
    /// <author>GoodAI</author>
    /// <meta>Os</meta>
    /// <status>WIP</status>
    /// <summary>"Multiple targets in a sequence" learning task</summary>
    /// <description>
    /// Ability description: The agent learns to navigate to multiple targets in a defined sequence. One target Type is always the first target, another target Type is always the second and so on.
    /// </description>
    [DisplayName("Reach multiple targets")]
    public class LTMultipleTargetsSequence : AbstractLearningTask<RoguelikeWorld>
    {
        protected readonly Random m_rndGen = new Random();
        protected GameObject m_target;
        protected GameObject m_agent;
        protected int m_stepsSincePresented = 0;

        public readonly TSHintAttribute SEQUENCE_LENGTH = new TSHintAttribute("Sequence length", "", typeof(int), 0, 5);
        public readonly TSHintAttribute TIMESTEPS_LIMIT = new TSHintAttribute("Timesteps limit", "", typeof(int), 0, 1);
        public readonly TSHintAttribute DISTANCE_BONUS_COEFFICENT = new TSHintAttribute("Multiply coefficent", "", typeof(float), 0, 1);

        private readonly List<GameObject> m_gameObjectReferences = new List<GameObject>();     // Create a vector of references to GameObjects, we will need the index for the sequence order
        public int currentIndex = 0;                                        // Represents current index of the sequence

        public LTMultipleTargetsSequence() : this(null) { }

        public LTMultipleTargetsSequence(SchoolWorld w)
            : base(w)
        {
            TSHints[TSHintAttributes.DEGREES_OF_FREEDOM] = 2;               // Set degrees of freedom to 2: move in 4 directions (1 means move only right-left)
            TSHints[TSHintAttributes.NUMBER_OF_DIFFERENT_OBJECTS] = 1;
            TSHints[TSHintAttributes.MAX_NUMBER_OF_ATTEMPTS] = 10000;

            TSHints.Add(SEQUENCE_LENGTH, 1);
            TSHints.Add(TIMESTEPS_LIMIT, 500);                             // Training unit fails if the TIMESTEP_LIMIT is reached, this is used to avoid that the agent stands without moving
            TSHints.Add(DISTANCE_BONUS_COEFFICENT, 3.0f);                  // Manhattan distance is calculated to estimate how many steps are needed to finish the sequence, DISTANCE_BONUS_COEFFICENT can be used to increase that number, if for example it's 2.0f then TIMESTEPS_LIMIT is (2 * EstimatedManahattanDistance)
            TSHints.Add(TSHintAttributes.IMAGE_TEXTURE_BACKGROUND, 1f);

            TSProgression.Add(TSHints.Clone());

            TSProgression.Add(
                new TrainingSetHints {
                    { SEQUENCE_LENGTH, 2 },
                    { DISTANCE_BONUS_COEFFICENT, 3f }
            });

            TSProgression.Add(
                new TrainingSetHints {
                    { SEQUENCE_LENGTH, 3 },
                    { DISTANCE_BONUS_COEFFICENT, 3f }
            });

            TSProgression.Add(
                new TrainingSetHints {
                    { SEQUENCE_LENGTH, 4 },
                    { DISTANCE_BONUS_COEFFICENT, 2.5f }
            });

            TSProgression.Add(
                new TrainingSetHints {
                    { SEQUENCE_LENGTH, 5 },
                    { DISTANCE_BONUS_COEFFICENT, 2.0f }
            });

            TSProgression.Add(
                new TrainingSetHints {
                    { DISTANCE_BONUS_COEFFICENT, 1.5f }
            });

            TSProgression.Add(
                new TrainingSetHints {
                    { DISTANCE_BONUS_COEFFICENT, 1.0f },
            });
        }

        /*
        public override void ExecuteStep()                                  // UpdateState calls base's equivalent and then its own additional functions
        {
            UpdateSequenceState();
        }
        */

        public override void PresentNewTrainingUnit()
        {
            CreateAgent();
            CreateTargets();

            m_stepsSincePresented = 0;
        }

        protected void CreateAgent()
        {
            WrappedWorld.CreateAgent();
            m_agent = WrappedWorld.Agent;

            m_agent.Position.X = WrappedWorld.Scene.Width / 2;
            m_agent.Position.Y = WrappedWorld.Scene.Height / 2;
        }

        public void CreateTargets()                                                                                     // Create a number of targets and fill the list(m_gameObjectReferences) of references for the sequence indexing
        {
            currentIndex = 0;
            m_gameObjectReferences.Clear();

            float cumulatedManhattanDistance = 0;

            GameObject previousGameObject = m_agent;

            for (int n = 0; n < (int)TSHints[SEQUENCE_LENGTH]; n++)                                                   // Generate a number of targets corresponding to the length of the sequence
            {
                m_target = new GameObject(GetNumberTargetImage(n), type: GameObjectType.NonColliding);
                WrappedWorld.AddGameObject(m_target);

                m_target.Size.Width = 10;
                m_target.Size.Height = 15;

                m_gameObjectReferences.Add(m_target);                                                                     // Add the current GameObject to the vector of GameObject references

                // PositionFree = WrappedWorld.RandomPositionInsidePowNonCovering(m_rndGen, m_target.GetGeometry().Size);  // Generate a random point where the corresponding GameObject doesn't cover any other GameObject

                // The generated GameObjects should be close to the agent, close enough so that no objects are outside the screen while following the sequence
                RectangleF r1 = new RectangleF
                {
                    X = m_agent.Position.X - WrappedWorld.Viewport.Width / 4,
                    Y = m_agent.Position.Y - WrappedWorld.Viewport.Height / 4,
                    Width = WrappedWorld.Viewport.Width / 2,
                    Height = WrappedWorld.Viewport.Height / 2
                };

                m_target.Position = WrappedWorld.RandomPositionInsideRectangleNonCovering(m_rndGen, m_target.GetGeometry().Size, r1);

                float estimatedManhattanDistance = Math.Abs(m_gameObjectReferences[n].Position.X - previousGameObject.Position.X)
                    + Math.Abs(m_gameObjectReferences[n].Position.Y - previousGameObject.Position.Y);
                estimatedManhattanDistance /= 4;                                                                        // Assuming that agent travels using 1 actuator at a time, (In such case it can move max 4 pixels per step, otherwise it's 8)
                cumulatedManhattanDistance += estimatedManhattanDistance;

                //MyLog.DEBUG.WriteLine("Calculating Manhattan distance between previous Object X,Y: " + previousGameObject.X + ", " + previousGameObject.Y + " AND " + m_gameObjectReferences[n].X + ", " + m_gameObjectReferences[n].Y + " := " + estimatedManhattanDistance);

                previousGameObject = m_gameObjectReferences[n];
            }

            //MyLog.DEBUG.WriteLine("Final estimatedManhattan Distance: " + CumulatedManhattanDistance);

            TSHints[TIMESTEPS_LIMIT] = cumulatedManhattanDistance * TSHints[DISTANCE_BONUS_COEFFICENT];
        }

        // Given an integer parameter, returns ther relevant path to its texture
        protected virtual string GetNumberTargetImage(int number)
        {
            switch (number)
            {
                case 0:
                    return "Number0_30x50.png";
                case 1:
                    return "Number1_30x50.png";
                case 2:
                    return "Number2_30x50.png";
                case 3:
                    return "Number3_30x50.png";
                case 4:
                    return "Number4_30x50.png";
                case 5:
                    return "Number5_30x50.png";
                default:
                    return "Number0_30x50.png";
            }
        }

        protected override bool DidTrainingUnitComplete(ref bool wasUnitSuccessful)
        {
            // expect this method to be called once per simulation step
            m_stepsSincePresented++;

            // Training unit is completed if the last index of the sequence is reached AND the last GameObject is reached
            if ((currentIndex == (int)TSHints[SEQUENCE_LENGTH] - 1) && (m_agent.DistanceTo(m_gameObjectReferences[currentIndex]) < 1))
            {
                currentIndex = 0;
                wasUnitSuccessful = true;
                return true;
            }

            //MyLog.DEBUG.WriteLine("StepsSincePresented: " + m_stepsSincePresented);
            //MyLog.DEBUG.WriteLine("LIMIT in TIMESTEPS: " + TSHints[TIMESTEPS_LIMIT]);

            if (m_stepsSincePresented >= (int)TSHints[TIMESTEPS_LIMIT])                 // If the limit of timesteps is reached, declare the current training unit failed
            {
                wasUnitSuccessful = false;
                return true;
            }

            // Updates the state of the sequence when necessary
            float dist = m_agent.DistanceTo(m_gameObjectReferences[currentIndex]);        // Check if the agent reached the GameObject corresponding to the current index of the sequence, this is done by reference
            if (dist < 1)
            {
                WrappedWorld.GameObjects.Remove(m_gameObjectReferences[currentIndex]);    // If agent reached the right object (the one corresponding to the current index sequence), delete corresponding object
                currentIndex++;                                                         // And now change the index (the index will denote a reference to the GameObject that needs to be reached next)
            }

            wasUnitSuccessful = false;
            return false;
        }
    }
}
