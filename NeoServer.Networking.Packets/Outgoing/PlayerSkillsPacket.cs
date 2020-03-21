﻿using NeoServer.Server.Model.Creatures;
using NeoServer.Server.Model.Players;
using System;
using System.Collections.Generic;
using System.Text;

namespace NeoServer.Networking.Packets.Outgoing
{
    public class PlayerSkillsPacket:OutgoingPacket
    {
        public PlayerSkillsPacket(Player player) : base(false)
        {
            OutputMessage.AddByte((byte)GameOutgoingPacketType.PlayerSkills);
            OutputMessage.AddByte(player.GetSkillInfo(SkillType.Fist));
            OutputMessage.AddByte(player.GetSkillPercent(SkillType.Fist));
            
            OutputMessage.AddByte(player.GetSkillInfo(SkillType.Club));
            OutputMessage.AddByte(player.GetSkillPercent(SkillType.Club));
            
            OutputMessage.AddByte(player.GetSkillInfo(SkillType.Sword));
            OutputMessage.AddByte(player.GetSkillPercent(SkillType.Sword));
            
            OutputMessage.AddByte(player.GetSkillInfo(SkillType.Axe));
            OutputMessage.AddByte(player.GetSkillPercent(SkillType.Axe));
            
            OutputMessage.AddByte(player.GetSkillInfo(SkillType.Distance));
            OutputMessage.AddByte(player.GetSkillPercent(SkillType.Distance));
            
            OutputMessage.AddByte(player.GetSkillInfo(SkillType.Shield));
            OutputMessage.AddByte(player.GetSkillPercent(SkillType.Shield));
            
            OutputMessage.AddByte(player.GetSkillInfo(SkillType.Fishing));
            OutputMessage.AddByte(player.GetSkillPercent(SkillType.Fishing));
        }
    }
}