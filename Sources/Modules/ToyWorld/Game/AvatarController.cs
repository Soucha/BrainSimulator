﻿using System;
using GoodAI.ToyWorld.Control;
using GoodAI.ToyWorldAPI;
using VRageMath;
using World.GameActors.GameObjects;

namespace Game
{
    public class AvatarController : IAvatarController
    {
        private readonly IAvatar m_avatar;
        private AvatarControls m_avatarControls;
        private string m_messageOut;

        public event MessageEventHandler NewMessage = delegate { };

        public string MessageOut
        {
            get { return m_messageOut; }
            set
            {
                m_messageOut = value;
                NewMessage(this, new MessageEventArgs(m_messageOut));
            }
        }

        public string MessageIn { get; set; }

        public AvatarController(IAvatar avatar)
        {
            m_avatar = avatar;
            m_avatarControls = new AvatarControls(int.MaxValue);

            avatar.NewMessage += avatar_NewMessage;
        }

        private void avatar_NewMessage(object sender, MessageEventArgs e)
        {
            NewMessage(this, e);
        }

        public void SetActions(IAvatarControls actions)
        {
            m_avatarControls.Update(actions);
            SetAvatarActionsControllable();
        }

        public IAvatarControls GetActions()
        {
            return m_avatarControls;
        }

        public IStats GetStats()
        {
            throw new NotImplementedException();
        }

        public string GetComment()
        {
            throw new NotImplementedException();
        }

        public void ResetControls()
        {
            m_avatarControls = new AvatarControls(int.MaxValue);
            m_avatar.ResetControls();
        }

        private void SetAvatarActionsControllable()
        {
            float fSpeed = m_avatarControls.DesiredForwardSpeed;
            float rSpeed = m_avatarControls.DesiredRightSpeed;

            // diagonal strafing speed should not be greater than 1
            // speed must be between [0,1]

            var jointSpeed = JointSpeed(fSpeed, rSpeed);
            m_avatar.DesiredSpeed = jointSpeed;

            float jointDirection =
                MathHelper.WrapAngle(m_avatar.Rotation
                                     + (float)Math.Atan2(m_avatarControls.DesiredForwardSpeed, m_avatarControls.DesiredRightSpeed)
                                     - MathHelper.PiOver2); // Our zero angle is the up direction (instead of right)
            m_avatar.Direction = jointDirection;
            m_avatar.DesiredLeftRotation = m_avatarControls.DesiredLeftRotation;
            m_avatar.Interact = m_avatarControls.Interact;
            m_avatar.PickUp = m_avatarControls.PickUp;
            m_avatar.UseTool = m_avatarControls.Use;
            m_avatar.Fof = m_avatarControls.Fof;
        }

        /// <summary>
        /// Diagonal strafing speed should not be greater than 1.
        /// Speed must be between [0,1].
        /// </summary>
        /// <param name="fSpeed">[-1,1]</param>
        /// <param name="rSpeed">[-1,1]</param>
        /// <returns></returns>
        private static float JointSpeed(float fSpeed, float rSpeed)
        {
            // WolframAlpha.com: Plot[Sqrt((a^2+b^2)/(a+b)), {a,0,1}, {b,0,1}]
            //float jointSpeed = (float) Math.Sqrt((fSpeed*fSpeed + rSpeed*rSpeed)/(Math.Sqrt(fSpeed) + Math.Sqrt(rSpeed)));

            // WolframAlpha.com: Plot[Max(a,b), {a,0,1}, {b,0,1}]
            float jointSpeed = Math.Max(Math.Abs(fSpeed), Math.Abs(rSpeed));
            return jointSpeed;
        }
    }
}
