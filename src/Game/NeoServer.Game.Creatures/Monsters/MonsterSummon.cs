﻿using NeoServer.Game.Common.Contracts.Creatures.Monsters;

namespace NeoServer.Game.Creatures.Monsters;

public readonly struct MonsterSummon : IMonsterSummon
{
    public MonsterSummon(string name, uint interval, byte chance, byte max)
    {
        Name = name;
        Interval = interval;
        Chance = chance;
        Max = max;
    }

    public string Name { get; }
    public uint Interval { get; }
    public byte Chance { get; }
    public byte Max { get; }
}