﻿using NeoServer.Game.Common.Contracts.Creatures;
using NeoServer.Game.Common.Contracts.Items.Types.Containers;
using NeoServer.Networking.Packets.Outgoing.Player;
using NeoServer.Server.Common.Contracts;

namespace NeoServer.Server.Events.Player.Containers;

public class PlayerClosedContainerEventHandler
{
    private readonly IGameServer game;

    public PlayerClosedContainerEventHandler(IGameServer game)
    {
        this.game = game;
    }

    public void Execute(IPlayer player, byte containerId, IContainer container)
    {
        if (!game.CreatureManager.GetPlayerConnection(player.CreatureId, out var connection)) return;

        connection.OutgoingPackets.Enqueue(new CloseContainerPacket(containerId));
        connection.Send();
    }
}