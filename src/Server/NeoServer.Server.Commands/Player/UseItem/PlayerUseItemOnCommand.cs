﻿using System;
using NeoServer.Game.Common.Contracts.Creatures;
using NeoServer.Game.Common.Contracts.Items;
using NeoServer.Game.Common.Contracts.Items.Types.Usable;
using NeoServer.Game.Common.Contracts.World.Tiles;
using NeoServer.Game.Common.Location;
using NeoServer.Networking.Packets.Incoming;
using NeoServer.Server.Common.Contracts;
using NeoServer.Server.Common.Contracts.Commands;

namespace NeoServer.Server.Commands.Player.UseItem;

public class PlayerUseItemOnCommand : ICommand
{
    private readonly IGameServer game;
    private readonly HotkeyService hotkeyService;

    public PlayerUseItemOnCommand(IGameServer game, HotkeyService hotkeyService)
    {
        this.game = game;
        this.hotkeyService = hotkeyService;
    }

    public void Execute(IPlayer player, UseItemOnPacket useItemPacket)
    {
        IItem onItem = null;
        ITile onTile = null;

        if (useItemPacket.ToLocation.Type == LocationType.Ground)
        {
            if (game.Map[useItemPacket.ToLocation] is not ITile tile) return;
            onTile = tile;
        }

        if (useItemPacket.ToLocation.Type == LocationType.Slot)
        {
            if (player.Inventory[useItemPacket.ToLocation.Slot] is null) return;
            onItem = player.Inventory[useItemPacket.ToLocation.Slot];
        }

        if (useItemPacket.ToLocation.Type == LocationType.Container)
        {
            if (player.Containers[useItemPacket.ToLocation.ContainerId][useItemPacket.ToLocation.ContainerSlot] is
                not IItem item) return;
            onItem = item;
        }

        if (onItem is not { } && onTile is not { }) return;

        Action action = null;

        IThing thingToUse = null;

        if (useItemPacket.Location.IsHotkey)
        {
            thingToUse = hotkeyService.GetItem(player, useItemPacket.ClientId);
        }
        else if (useItemPacket.Location.Type == LocationType.Ground)
        {
            if (game.Map[useItemPacket.Location] is not ITile tile) return;
            thingToUse = tile.TopItemOnStack;
        }
        else if (useItemPacket.Location.Type == LocationType.Slot)
        {
            thingToUse = player.Inventory[useItemPacket.Location.Slot];
        }
        else if (useItemPacket.Location.Type == LocationType.Container)
        {
            thingToUse =
                player.Containers[useItemPacket.Location.ContainerId][useItemPacket.Location.ContainerSlot];
        }

        if (thingToUse is not IUsableOn itemToUse) return;

        action = onTile is not null ? () => player.Use(itemToUse, onTile) : () => player.Use(itemToUse, onItem);

        if (useItemPacket.Location.Type == LocationType.Ground)
        {
            action?.Invoke();
            return;
        }

        action?.Invoke();
    }
}