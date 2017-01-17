﻿using System.Collections.Generic;
using VRageMath;
using World.Physics;
using Xunit;

namespace ToyWorldTests.Physics
{
    public class ShapesTests
    {
        [Theory]
        [InlineData(0.2, 0.2, 0.5)]
        [InlineData(0, 0, 1.5)]
        [InlineData(0, 0, 2)]
        [InlineData(0.1, 0.1, 1.5)]
        public void RectangleCoverTiles(float x, float y, float size)
        {
            var rectangle = new RectangleShape(new Vector2(x + size/2, y + size/2), new Vector2(size, size));
            List<Vector2I> coverTiles = rectangle.CoverTiles();

            if (TestUtils.FloatEq(x, 0.2f) && TestUtils.FloatEq(y, 0.2f) && TestUtils.FloatEq(size, 0.5f))
            {
                Assert.True(coverTiles.Count == 1);
                Assert.Contains(Vector2I.Zero, coverTiles);
            }

            if (TestUtils.FloatEq(x, 0f) && TestUtils.FloatEq(y, 0f) && TestUtils.FloatEq(size, 1.5f))
            {
                Assert.True(coverTiles.Count == 4);
                Assert.Contains(Vector2I.Zero, coverTiles);
                Assert.Contains(Vector2I.Zero, coverTiles);
                Assert.Contains(new Vector2I(0, 1), coverTiles);
                Assert.Contains(new Vector2I(1, 0), coverTiles);
                Assert.Contains(new Vector2I(1, 1), coverTiles);
                return;
            }

            if (TestUtils.FloatEq(x, 0f) && TestUtils.FloatEq(y, 0f) && TestUtils.FloatEq(size, 2f))
            {
                Assert.True(coverTiles.Count == 4);
                Assert.Contains(Vector2I.Zero, coverTiles);
                Assert.Contains(new Vector2I(0, 1), coverTiles);
                Assert.Contains(new Vector2I(1, 0), coverTiles);
                Assert.Contains(new Vector2I(1, 1), coverTiles);
                return;
            }

            if (TestUtils.FloatEq(x, 0.1f) && TestUtils.FloatEq(y, 0.1f) && TestUtils.FloatEq(size, 1.5f))
            {
                Assert.True(coverTiles.Count == 4);
                Assert.Contains(Vector2I.Zero, coverTiles);
                Assert.Contains(new Vector2I(0, 1), coverTiles);
                Assert.Contains(new Vector2I(1, 0), coverTiles);
                Assert.Contains(new Vector2I(1, 1), coverTiles);
            }
        }

        [Theory]
        [InlineData(0.7, 0.7, 0.2)]
        [InlineData(1.2, 1.2, 0.7)]
        [InlineData(1, 1, 0.8)]
        [InlineData(1, 1, 1)]
        [InlineData(1.1, 1.1, 0.8)]
        [InlineData(2, 2, 1.2)]
        [InlineData(1.5, 2, 1.05)]
        public void CircleCoverTiles(float x, float y, float radius)
        {
            var circle = new CircleShape(new Vector2(x, y), radius);
            List<Vector2I> coverTiles = circle.CoverTiles();

            if (TestUtils.FloatEq(x, 0.7f) && TestUtils.FloatEq(y, 0.7f) && TestUtils.FloatEq(radius, 0.1f))
            {
                Assert.True(coverTiles.Count == 1);
                Assert.Contains(Vector2I.Zero, coverTiles);
            }

            if (TestUtils.FloatEq(x, 1.2f) && TestUtils.FloatEq(y, 1.2f) && TestUtils.FloatEq(radius, 0.7f))
            {
                Assert.True(coverTiles.Count == 4);
                Assert.Contains(Vector2I.Zero, coverTiles);
                Assert.Contains(new Vector2I(0, 1), coverTiles);
                Assert.Contains(new Vector2I(1, 0), coverTiles);
                Assert.Contains(new Vector2I(1, 1), coverTiles);
                return;
            }

            if (TestUtils.FloatEq(x, 1f) && TestUtils.FloatEq(y, 1f) && TestUtils.FloatEq(y, 0.8f))
            {
                Assert.True(coverTiles.Count == 4);
                Assert.Contains(Vector2I.Zero, coverTiles);
                Assert.Contains(new Vector2I(0, 1), coverTiles);
                Assert.Contains(new Vector2I(1, 0), coverTiles);
                Assert.Contains(new Vector2I(1, 1), coverTiles);
                return;
            }

            if (TestUtils.FloatEq(x, 1f) && TestUtils.FloatEq(y, 1f) && TestUtils.FloatEq(radius, 1f))
            {
                Assert.True(coverTiles.Count == 4);
                Assert.Contains(Vector2I.Zero, coverTiles);
                Assert.Contains(new Vector2I(0, 1), coverTiles);
                Assert.Contains(new Vector2I(1, 0), coverTiles);
                Assert.Contains(new Vector2I(1, 1), coverTiles);
                return;
            }

            if (TestUtils.FloatEq(x, 1.1f) && TestUtils.FloatEq(y, 1.1f) && TestUtils.FloatEq(radius, 0.8f))
            {
                Assert.True(coverTiles.Count == 4);
                Assert.Contains(Vector2I.Zero, coverTiles);
                Assert.Contains(new Vector2I(0, 1), coverTiles);
                Assert.Contains(new Vector2I(1, 0), coverTiles);
                Assert.Contains(new Vector2I(1, 1), coverTiles);
                return;
            }

            if (TestUtils.FloatEq(x, 2f) && TestUtils.FloatEq(y, 2f) && TestUtils.FloatEq(radius, 1.2f))
            {
                Assert.True(coverTiles.Count == 12);
                Assert.True(!coverTiles.Exists(a => a == new Vector2I(0, 0)));
                Assert.Contains(new Vector2I(0, 1), coverTiles);
                Assert.Contains(new Vector2I(1, 0), coverTiles);
                Assert.Contains(new Vector2I(1, 1), coverTiles);
                return;
            }

            if (TestUtils.FloatEq(x, 1.5f) && TestUtils.FloatEq(y, 2f) && TestUtils.FloatEq(radius, 1.05f))
            {
                Assert.True(coverTiles.Count == 8);
                Assert.True(!coverTiles.Exists(a => a == new Vector2I(0, 0)));
                Assert.Contains(new Vector2I(0, 1), coverTiles);
                Assert.Contains(new Vector2I(1, 0), coverTiles);
                Assert.Contains(new Vector2I(1, 1), coverTiles);
            }
        }
    }
}
