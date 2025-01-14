﻿using System;
using System.Collections.Generic;
using System.Linq;
using NeoServer.Data.Model;
using NeoServer.Game.Chats;
using NeoServer.Game.Common.Contracts.Creatures;
using NeoServer.Game.Common.Contracts.DataStores;
using NeoServer.Game.Common.Contracts.Items;
using NeoServer.Game.Common.Contracts.Items.Types;
using NeoServer.Game.Common.Contracts.Items.Types.Containers;
using NeoServer.Game.Common.Contracts.World;
using NeoServer.Game.Common.Creatures;
using NeoServer.Game.Common.Creatures.Players;
using NeoServer.Game.Common.Item;
using NeoServer.Game.Common.Location.Structs;
using NeoServer.Game.Creatures.Model;
using NeoServer.Game.Creatures.Model.Players;
using NeoServer.Loaders.Interfaces;
using Serilog;

namespace NeoServer.Loaders.Players;

public class PlayerLoader : IPlayerLoader
{
    private readonly ChatChannelFactory _chatChannelFactory;
    private readonly ICreatureFactory _creatureFactory;
    private readonly IGuildStore _guildStore;
    private readonly IItemFactory _itemFactory;
    private readonly ILogger _logger;
    private readonly IMapTool _mapTool;
    private readonly IVocationStore _vocationStore;
    private readonly IWalkToMechanism _walkToMechanism;

    public PlayerLoader(IItemFactory itemFactory, ICreatureFactory creatureFactory,
        ChatChannelFactory chatChannelFactory,
        IGuildStore guildStore,
        IVocationStore vocationStore,
        IMapTool mapTool,
        IWalkToMechanism walkToMechanism,
        ILogger logger)
    {
        _itemFactory = itemFactory;
        _creatureFactory = creatureFactory;
        _chatChannelFactory = chatChannelFactory;
        _guildStore = guildStore;
        _vocationStore = vocationStore;
        _mapTool = mapTool;
        _walkToMechanism = walkToMechanism;
        _logger = logger;
    }

    public virtual bool IsApplicable(PlayerModel player)
    {
        return player.PlayerType == 1;
    }

    public virtual IPlayer Load(PlayerModel playerModel)
    {
        if (!_vocationStore.TryGetValue(playerModel.Vocation, out var vocation))
            _logger.Error($"Player vocation not found: {playerModel.Vocation}");

        var player = new Player(
            (uint)playerModel.PlayerId,
            playerModel.Name,
            playerModel.ChaseMode,
            playerModel.Capacity,
            playerModel.Health,
            playerModel.MaxHealth,
            vocation,
            playerModel.Gender,
            playerModel.Online,
            playerModel.Mana,
            playerModel.MaxMana,
            playerModel.FightMode,
            playerModel.Soul,
            vocation.SoulMax,
            ConvertToSkills(playerModel),
            playerModel.StaminaMinutes,
            new Outfit
            {
                Addon = (byte)playerModel.LookAddons, Body = (byte)playerModel.LookBody,
                Feet = (byte)playerModel.LookFeet, Head = (byte)playerModel.LookHead,
                Legs = (byte)playerModel.LookLegs, LookType = (byte)playerModel.LookType
            },
            0,
            new Location((ushort)playerModel.PosX, (ushort)playerModel.PosY, (byte)playerModel.PosZ),
            _mapTool,
            _walkToMechanism
        )
        {
            AccountId = (uint)playerModel.AccountId,
            Guild = _guildStore.Get((ushort)(playerModel?.GuildMember?.GuildId ?? 0)),
            GuildLevel = (ushort)(playerModel?.GuildMember?.RankId ?? 0)
        };

        player.AddInventory(ConvertToInventory(player, playerModel));

        AddExistingPersonalChannels(player);

        return _creatureFactory.CreatePlayer(player);
    }

    /// <summary>
    ///     Adds all PersonalChatChannel assemblies to Player
    /// </summary>
    public virtual void AddExistingPersonalChannels(IPlayer player)
    {
        if (player is null) return;

        var personalChannels = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
            .Where(x => typeof(PersonalChatChannel).IsAssignableFrom(x));
        foreach (var channel in personalChannels)
        {
            if (channel == typeof(PersonalChatChannel)) continue;

            var createdChannel = _chatChannelFactory.Create(channel, null, player);
            player.Channels.AddPersonalChannel(createdChannel);
        }
    }

    protected Dictionary<SkillType, ISkill> ConvertToSkills(PlayerModel playerRecord)
    {
        return new()
        {
            [SkillType.Axe] = new Skill(SkillType.Axe, (ushort)playerRecord.SkillAxe, playerRecord.SkillAxeTries),
            [SkillType.Club] = new Skill(SkillType.Club, (ushort)playerRecord.SkillClub, playerRecord.SkillClubTries),
            [SkillType.Distance] = new Skill(SkillType.Distance, (ushort)playerRecord.SkillDist,
                playerRecord.SkillDistTries),
            [SkillType.Fishing] = new Skill(SkillType.Fishing, (ushort)playerRecord.SkillFishing,
                playerRecord.SkillFishingTries),
            [SkillType.Fist] = new Skill(SkillType.Fist, (ushort)playerRecord.SkillFist, playerRecord.SkillFistTries),
            [SkillType.Shielding] = new Skill(SkillType.Shielding, (ushort)playerRecord.SkillShielding,
                playerRecord.SkillShieldingTries),
            [SkillType.Level] = new Skill(SkillType.Level, playerRecord.Level, playerRecord.Experience),
            [SkillType.Magic] =
                new Skill(SkillType.Magic, (ushort)playerRecord.MagicLevel, playerRecord.MagicLevelTries),
            [SkillType.Sword] =
                new Skill(SkillType.Sword, (ushort)playerRecord.SkillSword, playerRecord.SkillSwordTries)
        };
    }

    protected IInventory ConvertToInventory(IPlayer player, PlayerModel playerRecord)
    {
        var inventory = new Dictionary<Slot, Tuple<IPickupable, ushort>>();
        var attrs = new Dictionary<ItemAttribute, IConvertible> { { ItemAttribute.Count, 0 } };

        foreach (var item in playerRecord.PlayerInventoryItems)
        {
            attrs[ItemAttribute.Count] = (byte)item.Amount;
            var location = item.SlotId <= 10 ? Location.Inventory((Slot)item.SlotId) : Location.Container(0, 0);

            if (!(_itemFactory.Create((ushort)item.ServerId, location, attrs) is IPickupable createdItem)) continue;

            if (item.SlotId == (int)Slot.Backpack)
            {
                if (createdItem is not IContainer container) continue;
                BuildContainer(playerRecord.PlayerItems.Where(c => c.ParentId.Equals(0)).ToList(), 0, location,
                    container, playerRecord.PlayerItems.ToList());
            }

            if (item.SlotId == (int)Slot.Necklace)
                inventory.Add(Slot.Necklace, new Tuple<IPickupable, ushort>(createdItem, (ushort)item.ServerId));
            else if (item.SlotId == (int)Slot.Head)
                inventory.Add(Slot.Head, new Tuple<IPickupable, ushort>(createdItem, (ushort)item.ServerId));
            else if (item.SlotId == (int)Slot.Backpack)
                inventory.Add(Slot.Backpack, new Tuple<IPickupable, ushort>(createdItem, (ushort)item.ServerId));
            else if (item.SlotId == (int)Slot.Left)
                inventory.Add(Slot.Left, new Tuple<IPickupable, ushort>(createdItem, (ushort)item.ServerId));
            else if (item.SlotId == (int)Slot.Body)
                inventory.Add(Slot.Body, new Tuple<IPickupable, ushort>(createdItem, (ushort)item.ServerId));
            else if (item.SlotId == (int)Slot.Right)
                inventory.Add(Slot.Right, new Tuple<IPickupable, ushort>(createdItem, (ushort)item.ServerId));
            else if (item.SlotId == (int)Slot.Ring)
                inventory.Add(Slot.Ring, new Tuple<IPickupable, ushort>(createdItem, (ushort)item.ServerId));
            else if (item.SlotId == (int)Slot.Legs)
                inventory.Add(Slot.Legs, new Tuple<IPickupable, ushort>(createdItem, (ushort)item.ServerId));
            else if (item.SlotId == (int)Slot.Ammo)
                inventory.Add(Slot.Ammo, new Tuple<IPickupable, ushort>(createdItem, (ushort)item.ServerId));
            else if (item.SlotId == (int)Slot.Feet)
                inventory.Add(Slot.Feet, new Tuple<IPickupable, ushort>(createdItem, (ushort)item.ServerId));
        }

        return new Inventory(player, inventory);
    }

    private IContainer BuildContainer(List<PlayerItemModel> items, int index, Location location,
        IContainer container, List<PlayerItemModel> all)
    {
        if (items == null || items.Count == index) return container;

        var itemModel = items[index];

        var item = _itemFactory.Create((ushort)itemModel.ServerId, location,
            new Dictionary<ItemAttribute, IConvertible>
            {
                { ItemAttribute.Count, (byte)itemModel.Amount }
            });

        if (item is IContainer childrenContainer)
        {
            childrenContainer.SetParent(container);
            container.AddItem(BuildContainer(all.Where(c => c.ParentId.Equals(itemModel.Id)).ToList(), 0, location,
                childrenContainer, all));
        }
        else
        {
            container.AddItem(item);
        }

        return BuildContainer(items, ++index, location, container, all);
    }
}