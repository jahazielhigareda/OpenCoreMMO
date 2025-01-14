﻿using NeoServer.Game.Common.Contracts.Creatures;
using NeoServer.Game.Common.Texts;
using NeoServer.Networking.Packets.Outgoing;
using NeoServer.Networking.Packets.Outgoing.Effect;
using NeoServer.Networking.Packets.Outgoing.Player;
using NeoServer.Server.Common.Contracts;

namespace NeoServer.Server.Events.Player;

public class PlayerGainedExperienceEventHandler
{
    private readonly IGameServer game;

    public PlayerGainedExperienceEventHandler(IGameServer game)
    {
        this.game = game;
    }

    public void Execute(ICreature player, uint experience)
    {
        var experienceText = experience.ToString();
        foreach (var spectator in game.Map.GetPlayersAtPositionZone(player.Location))
            if (game.CreatureManager.GetPlayerConnection(spectator.CreatureId, out var connection))
            {
                connection.OutgoingPackets.Enqueue(new AnimatedTextPacket(player.Location, TextColor.White,
                    experienceText));

                if (spectator == player)
                {
                    connection.OutgoingPackets.Enqueue(new TextMessagePacket(
                        $"You gained {experienceText} experience points.",
                        TextMessageOutgoingType.MESSAGE_STATUS_DEFAULT));
                    connection.OutgoingPackets.Enqueue(new PlayerStatusPacket((IPlayer)player));
                }
                else
                {
                    connection.OutgoingPackets.Enqueue(new TextMessagePacket(
                        $"{player.Name} gained {experienceText} experience points.",
                        TextMessageOutgoingType.MESSAGE_STATUS_DEFAULT));
                    connection.OutgoingPackets.Enqueue(new PlayerStatusPacket((IPlayer)player));
                }

                connection.Send();
            }
    }
}