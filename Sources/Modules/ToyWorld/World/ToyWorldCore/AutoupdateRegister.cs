﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using VRage.Collections;
using World.Atlas;
using World.GameActors;
using World.GameActors.Tiles;

namespace World.ToyWorldCore
{
    public class AutoupdateRegister
    {
        private readonly CircularList<List<IAutoupdateableGameActor>> m_register;

        public int Size { get { return m_register.Size; } }

        protected List<IAutoupdateableGameActor> CurrentUpdateRequests
        {
            get
            {
                Contract.Ensures(Contract.Result<List<IAutoupdateableGameActor>>() != null);
                return m_register[0];
            }
        }

        public AutoupdateRegister(int registerSize = 100)
        {
            if (registerSize <= 0)
                throw new ArgumentOutOfRangeException("registerSize");
            Contract.EndContractBlock();

            m_register = new CircularList<List<IAutoupdateableGameActor>>(registerSize);
            m_register.MoveNext();
        }

        public void Register(IAutoupdateableGameActor actor, int timePeriod = 1)
        {
            if (actor == null)
                throw new ArgumentNullException("actor");
            if (timePeriod <= 0)
                throw new ArgumentOutOfRangeException("timePeriod", "Update period has to be larger than zero.");
            Contract.EndContractBlock();

            m_register[timePeriod].Add(actor);
        }

        public void Tick()
        {
            m_register.MoveNext();
        }

        public void UpdateItems(IAtlas atlas, TilesetTable table)
        {
            if (atlas.NewAutoupdateables != null)
            {
                CurrentUpdateRequests.AddRange(atlas.NewAutoupdateables);
                atlas.NewAutoupdateables.Clear();
            }
            foreach (IAutoupdateableGameActor actor in CurrentUpdateRequests)
            {
                actor.Update(atlas, table);
                if (actor.NextUpdateAfter > 0)
                    Register(actor, actor.NextUpdateAfter);
            }
            CurrentUpdateRequests.Clear();
        }

        [ContractInvariantMethod]
        private void Invariants()
        {
            Contract.Invariant(m_register != null, "Register cannot be null");
        }
    }
}