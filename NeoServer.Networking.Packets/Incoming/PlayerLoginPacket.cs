﻿using System;
using NeoServer.Networking.Packets.Messages;
using NeoServer.Networking.Packets.Outgoing;
using NeoServer.Server.Contracts.Network;
using NeoServer.Server.Model;
using NeoServer.Server.Security;

namespace NeoServer.Networking.Packets.Incoming
{
    public class PlayerLogInPacket : IncomingPacket
    {
        public string Account { get; set; }
        public string Password { get; set; }
        public string CharacterName { get; set; }
        public bool GameMaster { get; set; }
        public byte[] GameServerNonce { get; set; }
        public PlayerLogInPacket(IReadOnlyNetworkMessage message)
        {
            var packetLength = message.GetUInt16();
            var tcpPayload = packetLength + 2;
            message.SkipBytes(9);

            //// todo: version validation

            var encryptedData = message.GetBytes(tcpPayload - message.BytesRead);
            var data = new ReadOnlyNetworkMessage(RSA.Decrypt(encryptedData));

            LoadXtea(data);

            GameMaster = Convert.ToBoolean(data.GetByte());
            Account = data.GetString();
            CharacterName = data.GetString();
            Password = data.GetString();
            GameServerNonce = data.GetBytes(5);
        }



    }
}
