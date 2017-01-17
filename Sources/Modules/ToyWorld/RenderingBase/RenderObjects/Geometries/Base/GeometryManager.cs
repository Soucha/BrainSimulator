﻿using System;
using Utils.VRageRIP.Lib.Collections;
using VRageMath;

namespace RenderingBase.RenderObjects.Geometries
{
    public class GeometryManager
    {
        private readonly TypeSwitch<GeometryBase> m_geometries = new TypeSwitch<GeometryBase>();
        private readonly TypeSwitchParam<GeometryBase, Vector2I> m_vecGeometries = new TypeSwitchParam<GeometryBase, Vector2I>();


        public GeometryManager Case<T>()
            where T : GeometryBase, new()
        {
            m_geometries.Case<T>(() => new T());
            return this;
        }

        public GeometryManager CaseParam<T>()
            where T : GeometryBase
        {
            // Activator is about 11 times slower, than new T() -- should be ok for this usage
            m_vecGeometries.Case<T>(vec => (T)Activator.CreateInstance(typeof(T), vec));
            return this;
        }


        public T Get<T>()
            where T : GeometryBase
        {
            return m_geometries.Switch<T>();
        }

        public T Get<T>(Vector2I param)
            where T : GeometryBase
        {
            return m_vecGeometries.Switch<T>(param);
        }
    }
}
