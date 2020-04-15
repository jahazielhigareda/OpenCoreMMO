﻿using NeoServer.Game.Contracts.Items;
using NeoServer.Game.Enums;
using System;
using System.Collections.Generic;
using System.IO;

namespace NeoServer.Game.Items
{
    public class ItemType : IItemType
    {
        public ushort TypeId { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public ISet<ItemFlag> Flags { get; }

        public IDictionary<ItemAttribute, IConvertible> DefaultAttributes { get; }

        public bool Locked { get; private set; }

        public ushort ClientId { get; private set; }

        public ItemTypeAttribute TypeAttribute { get; private set; }

        public ItemGroup Group { get; private set; }
        public ushort WareId { get; private set; }
        public LightBlock LightBlock { get; private set; }
        public byte AlwaysOnTopOrder { get; private set; }
        public ushort Speed { get; private set; }
        public string Article { get; private set; }
        public string Plural { get; private set; }

        void ThrowIfLocked()
        {
            if (Locked)
            {
                throw new InvalidOperationException("This ItemType is locked and cannot be altered.");
            }
        }
        public void SetGroup(byte type)
        {
            ThrowIfLocked();
            Group = (ItemGroup)type;

        }

        public void SetType(byte type)
        {
            ThrowIfLocked();
            switch ((ItemGroup)type)
            {
                case ItemGroup.GroundContainer:
                    TypeAttribute = ItemTypeAttribute.ITEM_TYPE_CONTAINER;
                    break;
                case ItemGroup.ITEM_GROUP_DOOR:
                    //not used
                    TypeAttribute = ItemTypeAttribute.ITEM_TYPE_DOOR;
                    break;
                case ItemGroup.ITEM_GROUP_MAGICFIELD:
                    //not used
                    TypeAttribute = ItemTypeAttribute.ITEM_TYPE_MAGICFIELD;
                    break;
                case ItemGroup.ITEM_GROUP_TELEPORT:
                    //not used
                    TypeAttribute = ItemTypeAttribute.ITEM_TYPE_TELEPORT;
                    break;
                case ItemGroup.None:
                case ItemGroup.Ground:
                case ItemGroup.Splash:
                case ItemGroup.ITEM_GROUP_FLUID:
                case ItemGroup.ITEM_GROUP_CHARGES:
                case ItemGroup.ITEM_GROUP_DEPRECATED:
                    break;
                default:
                    break;
            }
        }

        public void SetSpeed(ushort speed)
        {
            ThrowIfLocked();
            Speed = speed;
        }

        public ItemType()
        {
            TypeId = 0;
            Name = string.Empty;
            Description = string.Empty;
            Flags = new HashSet<ItemFlag>();
            DefaultAttributes = new Dictionary<ItemAttribute, IConvertible>();
            Locked = false;
        }

        public void SetAlwaysOnTopOrder(byte alwaysOnTopOrder)
        {
            ThrowIfLocked();
            AlwaysOnTopOrder = alwaysOnTopOrder;
        }

        public void SetLight(LightBlock lightBlock)
        {
            ThrowIfLocked();
            LightBlock = lightBlock;
        }

        public void LockChanges()
        {
            Locked = true;
        }

        public void SetWareId(ushort wareId)
        {
            ThrowIfLocked();
            WareId = wareId;
        }

        public void SetId(ushort typeId)
        {
            ThrowIfLocked();

            TypeId = typeId;
        }

        public void SetClientId(ushort clientId)
        {
            ClientId = clientId;
        }

        public void SetName(string name)
        {
            ThrowIfLocked();

            Name = name;
        }

        public void SetDescription(string description)
        {
            ThrowIfLocked();

            Description = description.Trim('"');
        }

        public void SetFlag(ItemFlag flag)
        {
            ThrowIfLocked();

            Flags.Add(flag);
        }

        public void SetAttribute(ItemAttribute attribute, int attributeValue)
        {
            ThrowIfLocked();

            DefaultAttributes[attribute] = attributeValue;
        }
        public void SetAttribute(ItemAttribute attribute, IConvertible attributeValue)
        {
            ThrowIfLocked();

            DefaultAttributes[attribute] = attributeValue;
        }

        public void SetAttribute(string attributeName, int attributeValue)
        {
            ThrowIfLocked();

            if (!Enum.TryParse(attributeName, out ItemAttribute attribute))
            {
                throw new InvalidDataException($"Attempted to set an unknown Item attribute [{attributeName}].");
            }

            DefaultAttributes[attribute] = attributeValue;
        }

        public void ParseOTFlags(uint flags)
        {
            if (HasOTFlag(flags, 1 << 0)) // blockSolid
                SetFlag(ItemFlag.BlockSolid);

            if (HasOTFlag(flags, 1 << 1)) // blockProjectile
                SetFlag(ItemFlag.BlockProjectTile);

            if (HasOTFlag(flags, 1 << 2)) // blockPathFind
                SetFlag(ItemFlag.BlockPathFind);

            if (HasOTFlag(flags, 1 << 3)) // hasElevation
                SetFlag(ItemFlag.HasHeight);

            if (HasOTFlag(flags, 1 << 4)) // isUsable
                SetFlag(ItemFlag.Useable);

            if (HasOTFlag(flags, 1 << 5)) // isPickupable
                SetFlag(ItemFlag.Pickupable);

            if (HasOTFlag(flags, 1 << 6)) // isMoveable
                SetFlag(ItemFlag.Moveable);

            if (HasOTFlag(flags, 1 << 7)) // isStackable
                SetFlag(ItemFlag.Stackable);

            if (HasOTFlag(flags, 1 << 13)) // alwaysOnTop
                SetFlag(ItemFlag.AlwaysOnTop);

            if (HasOTFlag(flags, 1 << 14)) // isReadable
                SetFlag(ItemFlag.Readable);

            if (HasOTFlag(flags, 1 << 15)) // isRotatable
                SetFlag(ItemFlag.Rotatable);

            if (HasOTFlag(flags, 1 << 16)) // isHangable
                SetFlag(ItemFlag.Hangable);

            if (HasOTFlag(flags, 1 << 17)) // isVertical
                SetFlag(ItemFlag.Vertical);

            if (HasOTFlag(flags, 1 << 18)) // isHorizontal
                SetFlag(ItemFlag.Horizontal);

            //if (HasFlag(flags, 1 << 19)) // cannotDecay -- unused

            if (HasOTFlag(flags, 1 << 20)) // allowDistRead
                SetFlag(ItemFlag.AllowDistRead);

            //if (HasFlag(flags, 1 << 21)) // unused -- unused

            //if (HasFlag(flags, 1 << 22)) // isAnimation -- unused

            if (HasOTFlag(flags, 1 << 23)) // lookTrough
                SetFlag(ItemFlag.LookTrough);




            if (HasOTFlag(flags, 1 << 26)) // forceUse
                SetFlag(ItemFlag.ForceUse);
        }

        private bool HasOTFlag(UInt32 flags, UInt32 flag)
        {
            return (flags & flag) != 0;
        }



        public void SetArticle(string article)
        {
            Article = article;
        }

        public void SetPlural(string plural)
        {
            Plural = plural;
        }
    }
}