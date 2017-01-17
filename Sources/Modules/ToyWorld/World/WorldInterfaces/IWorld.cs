﻿using System.Collections.Generic;
using World.GameActors.GameObjects;

namespace World.WorldInterfaces
{
    public interface IWorld
    {
        /// <summary>
        /// Updates every object implementing IAutoupdatable interface and all their's GameActions
        /// Order of update is: 
        ///     1. Tiles actions
        ///     2. Avatar(s) actions
        ///     4. Characters actions
        ///     5. GameObjects actions
        ///     6. GamoObjects move (Physics)
        /// </summary>
        void Update();

        List<int> GetAvatarsIds();

        List<string> GetAvatarsNames();

        IAvatar GetAvatar(int id);
    }
}
