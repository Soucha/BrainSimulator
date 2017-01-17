﻿using System;
using GoodAI.ToyWorld.Control;
using OpenTK.Graphics.OpenGL;
using Render.RenderObjects.Effects;
using RenderingBase.Renderer;
using VRageMath;
using World.ToyWorldCore;

namespace Render.RenderRequests
{
    internal class PostprocessPainter
        : PainterBase<PostprocessingSettings, RenderRequestBase>, IDisposable
    {
        #region Fields

        protected NoiseEffect m_noiseEffect;

        #endregion

        #region Genesis

        public PostprocessPainter(RenderRequestBase owner)
            : base(owner)
        { }

        public virtual void Dispose()
        {
            if (m_noiseEffect != null)
                m_noiseEffect.Dispose();
        }

        #endregion

        #region Init

        public override void Init(RendererBase<ToyWorld> renderer, ToyWorld world, PostprocessingSettings settings)
        {
            Settings = settings;

            if (Settings.EnabledPostprocessing.HasFlag(RenderRequestPostprocessing.Noise))
            {
                if (m_noiseEffect == null)
                    m_noiseEffect = renderer.EffectManager.Get<NoiseEffect>();
                renderer.EffectManager.Use(m_noiseEffect); // Need to use the effect to set uniforms
                m_noiseEffect.ViewportSizeUniform((Vector2I)Owner.Resolution);
                m_noiseEffect.SceneTextureUniform((int)RenderRequestBase.TextureBindPosition.PostEffectTextureBindPosition);
            }
        }

        #endregion

        #region Draw

        public override void Draw(RendererBase<ToyWorld> renderer, ToyWorld world)
        {
            if (Settings.EnabledPostprocessing == RenderRequestPostprocessing.None)
                return;


            GL.Disable(EnableCap.Blend);

            // Always draw post-processing from the front to the back buffer
            Owner.BackFbo.Bind();

            if (Settings.EnabledPostprocessing.HasFlag(RenderRequestPostprocessing.Noise))
            {
                renderer.EffectManager.Use(m_noiseEffect);
                renderer.TextureManager.Bind(
                    Owner.FrontFbo[FramebufferAttachment.ColorAttachment0], // Use data from front Fbo
                    Owner.GetTextureUnit(RenderRequestBase.TextureBindPosition.PostEffectTextureBindPosition));

                // Advance noise time by a visually pleasing step; wrap around if we run for waaaaay too long.
                double step = 0.005d;
                double seed = renderer.SimTime * step % 3e6d;
                m_noiseEffect.TimeStepUniform(new Vector2((float)seed, (float)step));
                m_noiseEffect.VarianceUniform(Settings.NoiseIntensityCoefficient);

                Owner.Quad.Draw();
                Owner.SwapBuffers();
            }


            // more stuffs

            // The final scene should be left in the front buffer
        }

        #endregion
    }
}
