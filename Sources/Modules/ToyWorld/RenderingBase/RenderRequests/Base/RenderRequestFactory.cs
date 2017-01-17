﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using GoodAI.ToyWorld.Control;
using Utils.VRageRIP.Lib.Collections;

namespace RenderingBase.RenderRequests
{
    // NOTE: All renderRequests must inherit from RenderRequest
    public static class RenderRequestFactory
    {
        private static readonly TypeSwitch<IRenderRequest> RRSwitch = new TypeSwitch<IRenderRequest>();
        public static IEnumerable<IRenderRequest> RRs { get { return RRSwitch.Matches.Values.Select(rr => rr()); } }

        private static readonly TypeSwitchParam<IAvatarRenderRequest, int> avatarRRSwitch = new TypeSwitchParam<IAvatarRenderRequest, int>();
        public static IEnumerable<IAvatarRenderRequest> AvatarRRs { get { return avatarRRSwitch.Matches.Values.Select(rr => rr(0)); } }


        public static void CaseInternal<T, TNew>()
            where T : class, IRenderRequest
            where TNew : class, T, new()
        {
            RRSwitch.Case<T>(() => new TNew());
        }

        public static void CaseParamInternal<T, TNew>()
            where T : class, IAvatarRenderRequest
            where TNew : class, T
        {
            // Activator is about 11 times slower, than new T() -- should be ok for this usage
            avatarRRSwitch.Case<T>(vec => (TNew)Activator.CreateInstance(typeof(TNew), vec));
        }


        // TODO: log when returning null

        public static T CreateRenderRequest<T>()
            where T : class, IRenderRequest // unf cannot constrain T to be an interface, only a class
        {
            if (typeof(T) == typeof(IRenderRequest))
                throw new RenderRequestNotImplementedException("The supplied interface cannot be the base interface. You have to use a derived type. Used type: " + typeof(IRenderRequest).Name);

            return RRSwitch.Switch<T>();
        }

        public static T CreateRenderRequest<T>(int avatarID)
            where T : class, IAvatarRenderRequest // unf cannot constrain T to be an interface, only a class
        {
            if (typeof(T) == typeof(IAvatarRenderRequest))
                throw new RenderRequestNotImplementedException("The supplied interface cannot be the base interface. You have to use a derived type. Used type: " + typeof(IAvatarRenderRequest).Name);

            return avatarRRSwitch.Switch<T>(avatarID);
        }
    }
}
