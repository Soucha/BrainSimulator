﻿namespace World.GameActors.Tiles
{
    /// <summary>
    ///     StaticTile is tile, which cannot be updated, but can be replaced by dynamic tile. Only one static
    /// </summary>
    public abstract class StaticTile : Tile
    {
        protected StaticTile(ITilesetTable tilesetTable) : base(tilesetTable)
        {
        }

        protected StaticTile(int tileType) : base(tileType)
        {
        }
    }
}
