﻿using GoodAI.Modules.School.Common;
using GoodAI.Modules.School.Worlds;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace GoodAI.Modules.School.LearningTasks
{
    [DisplayName("Imitate sequence")]
    public class LTCopySequence : AbstractLearningTask<RoguelikeWorld>
    {
        private static readonly TSHintAttribute STOP_REQUEST = new TSHintAttribute("Stop request", "", typeof(bool), 0, 1);

        // True if teacher agent can spawn on different places
        // Teacher should not cover agent
        private static readonly TSHintAttribute TEACHER_ON_DIFF_START_POSITION = new TSHintAttribute("Teacher on diff start position", "", typeof(bool), 0, 1);

        protected Random m_rndGen = new Random();
        protected int m_stepsSincePresented = 0;
        protected MovableGameObject m_agent;
        protected AgentsHistory m_agentsHistory;
        protected AbstractTeacherInWorld m_teacher;
        protected AgentsHistory m_teachersHistory;
        protected bool m_delayedCheck = false;

        public LTCopySequence() : this(null) { }

        public LTCopySequence(SchoolWorld w)
            : base(w)
        {
            TSHints = new TrainingSetHints
            {
                { STOP_REQUEST, 0},
                { TSHintAttributes.DEGREES_OF_FREEDOM, 1 },
                { TSHintAttributes.IMAGE_NOISE, 0 },
                { TEACHER_ON_DIFF_START_POSITION, 0},
                { TSHintAttributes.MAX_NUMBER_OF_ATTEMPTS, 10000 },
                { TSHintAttributes.IMAGE_TEXTURE_BACKGROUND, 1 },
            };

            TSProgression.Add(TSHints.Clone());
            TSProgression.Add(TSHintAttributes.DEGREES_OF_FREEDOM, 2);
            TSProgression.Add(TSHintAttributes.IMAGE_NOISE, 1);
            TSProgression.Add(TEACHER_ON_DIFF_START_POSITION, 1);
            TSProgression.Add(STOP_REQUEST, 1);
        }

        public override void PresentNewTrainingUnit()
        {
            CreateAgent();
            CreateTeacher();

            m_stepsSincePresented = 0;
            m_agentsHistory = new AgentsHistory { m_agent.Position };
            m_teachersHistory = new AgentsHistory { m_teacher.Position };
        }

        protected override bool DidTrainingUnitComplete(ref bool wasUnitSuccessful)
        {
            if (SchoolWorld.IsEmulatingUnitCompletion())
            {
                return SchoolWorld.EmulateIsTrainingUnitCompleted(out wasUnitSuccessful);
            }
            else
            {
                m_stepsSincePresented++;

                if (DidTrainingUnitFail())
                {
                    wasUnitSuccessful = false;
                    return true;
                }

                if (!m_teacher.IsDone() && m_agent.IsMoving())
                {
                    wasUnitSuccessful = false;
                    return true;
                }

                // save history for agent and teacher
                m_agentsHistory.Add(m_agent.Position.X, m_agent.Position.Y);
                m_teachersHistory.Add(m_teacher.Position.X, m_teacher.Position.Y);

                wasUnitSuccessful = false;

                int numberOfTeachersSteps = m_teachersHistory.numberOfSteps();
                int numberOfAgentsSteps = m_agentsHistory.numberOfSteps();

                // simple version of the task
                if (TSHints[STOP_REQUEST] == .0f)
                {
                    if (numberOfTeachersSteps == numberOfAgentsSteps && m_teacher.IsDone())
                    {
                        // compare step
                        wasUnitSuccessful = m_teachersHistory.CompareTo(m_agentsHistory, m_stepsSincePresented);
                        return true;
                    }
                }
                // hard version
                else
                {
                    if (numberOfTeachersSteps == numberOfAgentsSteps && m_teacher.IsDone()) m_delayedCheck = true;

                    if (m_delayedCheck)
                    {
                        m_delayedCheck = false;
                        // compare steps
                        wasUnitSuccessful = m_teachersHistory.CompareTo(m_agentsHistory, m_stepsSincePresented);
                        return true;
                    }
                }
                return false;
            }
        }

        protected bool DidTrainingUnitFail()
        {
            return m_stepsSincePresented > 2 * m_teacher.ActionsCount() + 3;
        }

        protected void CreateAgent()
        {
            m_agent = WrappedWorld.CreateAgent();
        }

        protected void CreateTeacher()
        {
            List<RogueTeacher.Actions> actions = new List<RogueTeacher.Actions>
            {
                RogueTeacher.GetRandomAction(m_rndGen, (int) TSHints[TSHintAttributes.DEGREES_OF_FREEDOM]),
                RogueTeacher.GetRandomAction(m_rndGen, (int) TSHints[TSHintAttributes.DEGREES_OF_FREEDOM]),
                RogueTeacher.GetRandomAction(m_rndGen, (int) TSHints[TSHintAttributes.DEGREES_OF_FREEDOM]),
                RogueTeacher.GetRandomAction(m_rndGen, (int) TSHints[TSHintAttributes.DEGREES_OF_FREEDOM]),
                RogueTeacher.GetRandomAction(m_rndGen, (int) TSHints[TSHintAttributes.DEGREES_OF_FREEDOM])
            };

            RectangleF restrcitedRectangle = WrappedWorld.GetPowGeometry();
            restrcitedRectangle = LearningTaskHelpers.ResizeRectangleAroundCentre(restrcitedRectangle, 0.8f);

            PointF teachersPoint;
            if ((int)TSHints[TEACHER_ON_DIFF_START_POSITION] != 0)
            {
                teachersPoint = WrappedWorld.RandomPositionInsideRectangleNonCovering(m_rndGen, RogueTeacher.GetDefaultSize(), restrcitedRectangle, 10);
            }
            else
            {
                teachersPoint = new PointF(m_agent.Position.X + WrappedWorld.Viewport.Width / 3, m_agent.Position.Y);
            }

            m_teacher = WrappedWorld.CreateTeacher(teachersPoint, actions) as RogueTeacher;
        }
    }

    public class AgentsHistory : LinkedList<PointF>
    {
        public void Add(PointF position)
        {
            this.AddLast(position);
        }

        public void Add(float x, float y)
        {
            this.AddLast(new PointF(x, y));
        }

        // compare two histories. Second can be shifted forward
        public bool CompareTo(AgentsHistory h, int numberOfMoves)
        {
            if (this.Count != h.Count)
            {
                throw new ArgumentException();
            }

            Enumerator h1 = this.GetEnumerator();
            Enumerator h2 = h.GetEnumerator();

            h1.MoveNext();
            h2.MoveNext();

            SizeF diff = SizeF.Subtract(new SizeF(h2.Current), new SizeF(h1.Current));

            for (int i = 0; i < numberOfMoves; i++)
            {
                while (true)
                {
                    PointF norm = h2.Current - diff;
                    if (h1.Current == norm) break;
                    bool moves = h2.MoveNext();
                    if (!moves) return false;
                }
                h1.MoveNext();
                h2.MoveNext();
            }
            return true;
        }

        public bool IsLastDifferent()
        {
            if (this.Count <= 1)
            {
                return false;
            }
            return !this.Last().Equals(this.ElementAt(this.Count() - 2));
        }

        public int numberOfUniquePositions()
        {
            HashSet<PointF> h = new HashSet<PointF>(this);
            return h.Count;
        }

        public int numberOfSteps()
        {
            int numberOfSteps = 0;
            Enumerator e1 = this.GetEnumerator();
            Enumerator e2 = this.GetEnumerator();
            e2.MoveNext();
            for (int i = 0; i < Count - 1; i++)
            {
                e1.MoveNext();
                e2.MoveNext();
                if (e1.Current != e2.Current) numberOfSteps++;
            }
            return numberOfSteps;
        }
    }
}
