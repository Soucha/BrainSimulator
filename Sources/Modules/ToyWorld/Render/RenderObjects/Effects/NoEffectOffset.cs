﻿using VRageMath;

namespace Render.RenderObjects.Effects
{
    public class NoEffectOffset : NoEffect
    {
        private enum Uniforms
        {
            // Names must correspond to names as defined in the shaders
            tileTypesTexture,
            tileTypesIdxOffset,
            texSizeCount,
            tileSizeMargin,
            tileBorder,
            tileVertexCount,
            mvp,

            tilesetTexture,
            tilesetTextureWinter,
            diffuse,
            ambient,
        }


        public NoEffectOffset()
            : base(typeof(Uniforms), "BasicOffset.vert", "BasicOffset.frag")
        { }


        public void TileTypesTextureUniform(int val)
        {
            SetUniform1(base[Uniforms.tileTypesTexture], val);
        }

        public void TileTypesIdxOffsetUniform(int val)
        {
            SetUniform1(base[Uniforms.tileTypesIdxOffset], val);
        }

        public void TexSizeCountUniform(Vector3I val)
        {
            SetUniform3(base[Uniforms.texSizeCount], val);
        }

        public void TileSizeMarginUniform(Vector4I val)
        {
            SetUniform4(base[Uniforms.tileSizeMargin], val);
        }

        public void TileBorderUniform(Vector2I val)
        {
            SetUniform2(base[Uniforms.tileBorder], val);
        }

        public void TileVertexCountUniform(int val)
        {
            SetUniform1(base[Uniforms.tileVertexCount], val);
        }

        public void ModelViewProjectionUniform(ref Matrix val)
        {
            SetUniformMatrix4(base[Uniforms.mvp], val);
        }

        
        public void TextureUniform(int val)
        {
            SetUniform1(base[Uniforms.tilesetTexture], val);
        }

        public void TextureWinterUniform(int val)
        {
            SetUniform1(base[Uniforms.tilesetTextureWinter], val);
        }

        public void AmbientUniform(Vector4 val)
        {
            SetUniform4(base[Uniforms.ambient], val);
        }

        public void DiffuseUniform(Vector4 val)
        {
            SetUniform4(base[Uniforms.diffuse], val);
        }
    }
}
