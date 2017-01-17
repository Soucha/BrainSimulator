﻿using VRageMath;
using World.Atlas;
using World.Atlas.Layers;
using World.GameActions;
using World.ToyWorldCore;

namespace World.GameActors.Tiles.OnGroundInteractable
{
    public class Fireplace : DynamicTile, IInteractableGameActor, ICombustibleGameActor
    {
        public Fireplace(ITilesetTable tilesetTable, Vector2I position)
            : base(tilesetTable, position)
        {
        }

        public Fireplace(int tileType, Vector2I position)
            : base(tileType, position)
        {
        }

        public void ApplyGameAction(IAtlas atlas, GameAction gameAction, Vector2 position, ITilesetTable tilesetTable)
        {
            var interact = gameAction as Interact;

            if (interact != null)
            {
                Ignite(atlas, tilesetTable);
            }
        }

        public void Burn(GameActorPosition gameActorPosition, IAtlas atlas, ITilesetTable table)
        {
            Ignite(atlas, table);
        }

        private void Ignite(IAtlas atlas, ITilesetTable tilesetTable)
        {
            var fireplaceBurning = new FireplaceBurning(tilesetTable, Position);
            atlas.ReplaceWith(ThisGameActorPosition(LayerType.OnGroundInteractable), fireplaceBurning);
            atlas.RegisterToAutoupdate(fireplaceBurning);
        }
    }
}