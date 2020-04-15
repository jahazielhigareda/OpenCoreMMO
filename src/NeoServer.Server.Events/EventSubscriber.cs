﻿using Autofac;
using NeoServer.Game.Contracts;
using NeoServer.Server.Model.Players.Contracts;

namespace NeoServer.Server.Events
{
    public class EventSubscriber
    {
        private readonly IMap map;
        private IComponentContext container;

        public EventSubscriber(IMap map, IComponentContext container)
        {
            this.map = map;
            this.container = container;
        }

        public void AttachEvents()
        {
            map.OnCreatureAddedOnMap += (creature) => container.Resolve<PlayerAddedOnMapEventHandler>().Execute((IPlayer)creature);
            map.OnThingRemovedFromTile += container.Resolve<ThingRemovedFromTileEventHandler>().Execute;
            map.OnThingMoved += container.Resolve<ThingMovedOnFloorEventHandler>().Execute;
            map.OnThingMovedFailed += container.Resolve<ThingMovedFailedEventHandler>().Execute;
        }
    }
}