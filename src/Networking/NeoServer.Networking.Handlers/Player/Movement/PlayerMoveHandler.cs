﻿using NeoServer.Game.Common.Location;
using NeoServer.Server.Common.Contracts;
using NeoServer.Server.Common.Contracts.Network;
using NeoServer.Server.Common.Contracts.Network.Enums;
using NeoServer.Server.Tasks;

namespace NeoServer.Networking.Handlers.Player.Movement;

public class PlayerMoveHandler : PacketHandler
{
    private readonly IGameServer game;

    public PlayerMoveHandler(IGameServer game)
    {
        this.game = game;
    }

    public override void HandlerMessage(IReadOnlyNetworkMessage message, IConnection connection)
    {
        var direction = ParseMovementPacket(message.IncomingPacket);

        if (game.CreatureManager.TryGetPlayer(connection.CreatureId, out var player))
            game.Dispatcher.AddEvent(new Event(() => player.WalkTo(direction)));
    }

    private Direction ParseMovementPacket(GameIncomingPacketType walkPacket)
    {
        var direction = Direction.North;

        switch (walkPacket)
        {
            case GameIncomingPacketType.WalkEast:
                direction = Direction.East;
                break;
            case GameIncomingPacketType.WalkNorth:
                direction = Direction.North;
                break;
            case GameIncomingPacketType.WalkSouth:
                direction = Direction.South;
                break;
            case GameIncomingPacketType.WalkWest:
                direction = Direction.West;
                break;
            case GameIncomingPacketType.WalkNorteast:
                direction = Direction.NorthEast;
                break;
            case GameIncomingPacketType.WalkNorthwest:
                direction = Direction.NorthWest;
                break;
            case GameIncomingPacketType.WalkSoutheast:
                direction = Direction.SouthEast;
                break;
            case GameIncomingPacketType.WalkSouthwest:
                direction = Direction.SouthWest;
                break;
        }

        return direction;
    }
}