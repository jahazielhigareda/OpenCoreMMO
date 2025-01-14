﻿using NeoServer.Game.Common.Contracts;
using NeoServer.Game.Common.Contracts.Items;
using NeoServer.Game.Common.Location.Structs;
using NeoServer.Game.Items.Bases;
using NeoServer.Game.Items.Factories.AttributeFactory;

namespace NeoServer.Game.Items.Factories;

public class GenericItemFactory : IFactory
{
    public event CreateItem OnItemCreated;

    public IItem Create(IItemType itemType, Location location)
    {
        var hasDecayable = DecayableFactory.HasDecayable(itemType);
        if (!hasDecayable) return new StaticItem(itemType, location);

        return new Item(itemType, location);
    }
}