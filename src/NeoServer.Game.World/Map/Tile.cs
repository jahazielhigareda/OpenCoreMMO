using NeoServer.Game.Contracts;
using NeoServer.Game.Contracts.Creatures;
using NeoServer.Game.Contracts.Items;
using NeoServer.Game.Enums.Location;
using NeoServer.Game.Enums.Location.Structs;
using NeoServer.Server.Helpers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace NeoServer.Game.World.Map
{
    public class Tile : ITile
    {
        private readonly Stack<uint> _creatureIdsOnTile;

        private readonly Stack<IItem> _topItems1OnTile;

        private readonly Stack<IItem> _topItems2OnTile;

        private readonly Stack<IItem> _downItemsOnTile;

        private byte[] _cachedDescription;

        public Location Location { get; }

        public byte Flags { get; private set; }

        public IItem Ground { get; set; }
        public uint GroundStepSpeed => Ground?.Type?.Speed != 0 ? Ground.Type.Speed : (uint)150;

        public IEnumerable<uint> CreatureIds => _creatureIdsOnTile;

        public IEnumerable<IItem> TopItems1 => _topItems1OnTile;

        public IEnumerable<IItem> TopItems2 => _topItems2OnTile;

        public IEnumerable<IItem> DownItems => _downItemsOnTile;

        public bool CannotLogout => HasFlag(TileFlags.NoLogout);
        public bool ProtectionZone => HasFlag(TileFlags.ProtectionZone);

        private bool HasFlag(TileFlags flag) => ((uint)flag & Flags) != 0;

        public bool HandlesCollision
        {
            get
            {
                return (Ground != null && Ground.HasCollision) || TopItems1.Any(i => i.HasCollision) || TopItems2.Any(i => i.HasCollision) || DownItems.Any(i => i.HasCollision);
            }
        }

        public PathError PathError =>
             this switch
             {
                 { HasCollision: true } => PathError.NotEnoughRoom,
                 _ => PathError.None
             };

        public IEnumerable<IItem> ItemsWithCollision
        {
            get
            {
                var items = new List<IItem>();

                if (Ground.HasCollision)
                {
                    items.Add(Ground);
                }

                items.AddRange(TopItems1.Where(i => i.HasCollision));
                items.AddRange(TopItems2.Where(i => i.HasCollision));
                items.AddRange(DownItems.Where(i => i.HasCollision));

                return items;
            }
        }

        public bool HandlesSeparation
        {
            get
            {
                return (Ground != null && Ground.HasSeparation) || TopItems1.Any(i => i.HasSeparation) || TopItems2.Any(i => i.HasSeparation) || DownItems.Any(i => i.HasSeparation);
            }
        }

        public IEnumerable<IItem> ItemsWithSeparation
        {
            get
            {
                var items = new List<IItem>();

                if (Ground.HasSeparation)
                {
                    items.Add(Ground);
                }

                items.AddRange(TopItems1.Where(i => i.HasSeparation));
                items.AddRange(TopItems2.Where(i => i.HasSeparation));
                items.AddRange(DownItems.Where(i => i.HasSeparation));

                return items;
            }
        }

        public bool IsHouse => false;

        public bool BlocksThrow
        {
            get
            {
                return (Ground != null && Ground.BlocksThrow) || TopItems1.Any(i => i.BlocksThrow) || TopItems2.Any(i => i.BlocksThrow) || DownItems.Any(i => i.BlocksThrow);
            }
        }

        public bool BlocksPass
        {
            get
            {
                return (Ground != null && Ground.BlocksPass) || CreatureIds.Any() || TopItems1.Any(i => i.BlocksPass) || TopItems2.Any(i => i.BlocksPass) || DownItems.Any(i => i.BlocksPass);
            }
        }

        public bool BlocksLay
        {
            get
            {
                return (Ground != null && Ground.BlocksLay) || TopItems1.Any(i => i.BlocksLay) || TopItems2.Any(i => i.BlocksLay) || DownItems.Any(i => i.BlocksLay);
            }
        }

        public byte[] Cache
        {
            get
            {
                if (_cachedDescription == null)
                {
                    _cachedDescription = UpdateCache();
                }

                return _cachedDescription;
            }
        }

        public bool HasAnyFloorDestination => FloorChangeDestination != FloorChangeDirection.None;


        public bool HasFloorDestination(FloorChangeDirection direction)
        {
            return FloorChangeDestination == direction;
        }

        public FloorChangeDirection FloorChangeDestination
        {
            get
            {
                IConvertible floorChange;
                if (Ground?.Attributes != null && Ground.Attributes.TryGetValue(Enums.ItemAttribute.FloorChange, out floorChange))
                {
                    return ParseFloorChange((string)floorChange);
                }

                foreach (var item in TopItems1)
                {
                    if (item?.Attributes != null && item.Attributes.TryGetValue(Enums.ItemAttribute.FloorChange, out floorChange))
                    {
                        return ParseFloorChange((string)floorChange);
                    }

                }
                return FloorChangeDirection.None;
            }
        }

        private FloorChangeDirection ParseFloorChange(string floorChange)
        {
            switch (floorChange)
            {
                case "down": return FloorChangeDirection.Down;
                case "north": return FloorChangeDirection.North;
                case "south": return FloorChangeDirection.South;
                case "southalt": return FloorChangeDirection.SouthAlternative;
                case "west": return FloorChangeDirection.West;
                case "east": return FloorChangeDirection.East;
                case "eastalt": return FloorChangeDirection.EastAlternative;
                default: break;
            }
            return FloorChangeDirection.None;
        }


        private byte[] UpdateCache() //todo: code duplication
        {
            // not valid to cache response if there are creatures.
            if (_creatureIdsOnTile.Count > 0)
            {
                return null;
            }

            var pool = ArrayPool<byte>.Shared;
            var minimumLength = 27; //max of possible bytes length

            var stream = new StreamArray(pool.Rent(minimumLength));

            var count = 0;
            const int numberOfObjectsLimit = 9;

            if (Ground != null)
            {
                stream.AddUInt16(Ground.Type.ClientId);
                count++;
            }

            var items = TopItems1.Reverse().Concat(TopItems2.Reverse()).Concat(DownItems.Reverse());

            foreach (var item in items)
            {
                if (count == numberOfObjectsLimit)
                {
                    break;
                }

                stream.AddUInt16(item.Type.ClientId);

                if (item.IsCumulative)
                {
                    stream.AddByte(item.Amount);
                }
                else if (item.IsLiquidPool || item.IsLiquidContainer)
                {
                    stream.AddByte((byte)item.LiquidType);
                }

                count++;
            }

            var buffer = stream.GetStream();
            var streamResult = buffer.AsSpan(0, stream.Length).ToArray();

            pool.Return(buffer, true);
            return streamResult;

        }

        public bool CanBeWalked(byte avoidDamageType = 0)
        {
            return !CreatureIds.Any()
                && Ground != null
                && !Ground.IsPathBlocking(avoidDamageType)
                && !TopItems1.Any(i => i.IsPathBlocking(avoidDamageType))
                && !TopItems2.Any(i => i.IsPathBlocking(avoidDamageType))
                && !DownItems.Any(i => i.IsPathBlocking(avoidDamageType));
        }

        public bool HasThing(IThing thing, byte count = 1)
        {
            if (count == 0)
            {
                throw new ArgumentException("Invalid count zero.", nameof(count));
            }

            var creaturesCheck = thing is ICreature creature && _creatureIdsOnTile.Contains(creature.CreatureId);

            var top1Check = thing is IItem && _topItems1OnTile.Count > 0 && _topItems1OnTile.Peek() == thing && thing.Count >= count;
            var top2Check = thing is IItem && _topItems2OnTile.Count > 0 && _topItems2OnTile.Peek() == thing && thing.Count >= count;
            var downCheck = thing is IItem && _downItemsOnTile.Count > 0 && _downItemsOnTile.Peek() == thing && thing.Count >= count;

            return creaturesCheck || top1Check || top2Check || downCheck;
        }

        // public static HashSet<string> PropSet = new HashSet<string>();

        // public string LoadedFrom { get; set; }



        public Tile(Coordinate coordinate) : this(new Location()
        {
            X = coordinate.X,
            Y = coordinate.Y,
            Z = coordinate.Z
        })
        {
        }
        public Tile(ushort x, ushort y, sbyte z)
            : this(new Location { X = x, Y = y, Z = z })
        {

        }

        public Tile(Location loc)
        {
            Location = loc;
            _creatureIdsOnTile = new Stack<uint>();
            _topItems1OnTile = new Stack<IItem>();
            _topItems2OnTile = new Stack<IItem>();
            _downItemsOnTile = new Stack<IItem>();
        }


        public void AddThing(ref IThing thing, byte count)
        {

            if (count == 0)
            {
                throw new ArgumentException("Invalid count zero.");
            }

            var item = thing as IItem;

            if (thing is ICreature creature)
            {
                _creatureIdsOnTile.Push(creature.CreatureId);

                creature.Tile = this;
                creature.Added();

            }
            else if (item != null)
            {
                if (item.IsGround)
                {
                    Ground = item;
                    item.Added();
                }
                else if (item.IsTop1)
                {
                    _topItems1OnTile.Push(item);
                    item.Added();
                }
                else if (item.IsTop2)
                {
                    _topItems2OnTile.Push(item);
                    item.Added();
                }
                else
                {
                    if (item.IsCumulative)
                    {
                        var currentItem = _downItemsOnTile.Count > 0 ? _downItemsOnTile.Peek() as IItem : null;

                        if (currentItem != null && currentItem.Type == item.Type && currentItem.Amount < 100)
                        {
                            // add these up.
                            var remaining = currentItem.Amount + count;

                            var newCount = (byte)Math.Min(remaining, 100);

                            currentItem.Amount = newCount;

                            remaining -= newCount;

                            if (remaining > 0)
                            {
                                item.Amount = (byte)remaining;
                                _downItemsOnTile.Push(item);
                                item.Added();
                            }
                        }
                        else
                        {
                            item.Amount = count;
                            _downItemsOnTile.Push(item);
                            item.Added();
                        }
                    }
                    else
                    {
                        _downItemsOnTile.Push(item);
                        item.Added();
                    }
                }

                item.Tile = this;


                // invalidate the cache.
            }

            _cachedDescription = null;

            //var stackPos = GetStackPositionOfThing(thing);
            //thing.OnThingRemoved += (thing) => dispatcher.Dispatch(new ThingRemovedFromTileEvent(this, stackPos));

        }

        public void RemoveThing(ref IThing thing, byte count)
        {
            var fromStackPosition = thing.GetStackPosition();

            if (count == 0)
            {
                throw new ArgumentException("Invalid count zero.");
            }

            var item = thing as IItem;

            if (thing is ICreature creature)
            {
                RemoveCreature(creature);
                creature.Tile = null;
                //creature.Removed();
            }
            else if (item != null)
            {
                var removeItem = true;

                if (item.IsGround)
                {
                    Ground = null;
                    item.Removed();
                    removeItem = false;
                }
                else if (item.IsTop1)
                {
                    _topItems1OnTile.Pop();
                    item.Removed();
                    removeItem = false;
                }
                else if (item.IsTop2)
                {
                    _topItems2OnTile.Pop();
                    item.Removed();
                    removeItem = false;
                }
                else
                {
                    if (item.IsCumulative)
                    {
                        if (item.Amount < count) // throwing because this should have been checked before.
                        {
                            throw new ArgumentException("Remove count is greater than available.");
                        }

                        if (item.Amount > count)
                        {
                            throw new NotImplementedException(); //todo
                                                                 // create a new item (it got split...)

                            // var newItem = ItemFactory.Create(item.Type.TypeId);
                            // newItem.SetAmount(count);
                            // item.Amount -= count;

                            // thing = newItem;
                            // removeItem = false;
                        }
                    }
                }

                if (removeItem)
                {
                    _downItemsOnTile.Pop();
                    item.Removed();
                    item.Tile = null;
                }
            }
            else
            {
                throw new InvalidCastException("Thing did not cast to either a CreatureId or Item.");
            }
            // invalidate the cache.
            _cachedDescription = null;
        }


        private void RemoveCreature(ICreature c)
        {
            var tempStack = new Stack<uint>();
            ICreature removed = null;

            lock (_creatureIdsOnTile)
            {
                while (removed == null && _creatureIdsOnTile.Count > 0)
                {
                    var temp = _creatureIdsOnTile.Pop();

                    if (c.CreatureId == temp)
                    {
                        removed = c;
                    }
                    else
                    {
                        tempStack.Push(temp);
                    }
                }

                while (tempStack.Count > 0)
                {
                    _creatureIdsOnTile.Push(tempStack.Pop());
                }
            }

        }

        private void AddTopItem1(IItem i)
        {
            lock (_topItems1OnTile)
            {
                _topItems1OnTile.Push(i);

                // invalidate the cache.
                _cachedDescription = null;
            }
        }

        private void AddTopItem2(IItem i)
        {
            lock (_topItems2OnTile)
            {
                _topItems2OnTile.Push(i);

                // invalidate the cache.
                _cachedDescription = null;
            }
        }

        private void AddDownItem(IItem i)
        {
            lock (_downItemsOnTile)
            {
                _downItemsOnTile.Push(i);

                // invalidate the cache.
                _cachedDescription = null;
            }
        }

        public void AddContent(IItem item)
        {
            var downItemStackToReverse = new Stack<IItem>();
            var top1ItemStackToReverse = new Stack<IItem>();
            var top2ItemStackToReverse = new Stack<IItem>();

            if (item.IsGround)
                Ground = item;
            else if (item.IsTop1)
                top1ItemStackToReverse.Push(item);
            else if (item.IsTop2)
                top2ItemStackToReverse.Push(item);
            else
                downItemStackToReverse.Push(item);

            item.Tile = this;

            while (top1ItemStackToReverse.Count > 0)
                AddTopItem1(top1ItemStackToReverse.Pop());

            while (top2ItemStackToReverse.Count > 0)
                AddTopItem2(top2ItemStackToReverse.Pop());

            while (downItemStackToReverse.Count > 0)
                AddDownItem(downItemStackToReverse.Pop());
        }

        public IItem BruteFindItemWithId(ushort id)
        {
            if (Ground != null && Ground.ThingId == id)
            {
                return Ground;
            }

            foreach (var item in _topItems1OnTile.Union(_topItems2OnTile).Union(_downItemsOnTile))
            {
                if (item.ThingId == id)
                {
                    return item;
                }
            }

            return null;
        }

        public IItem BruteRemoveItemWithId(ushort id)
        {
            if (Ground != null && Ground.ThingId == id)
            {
                var ground = Ground;

                Ground = null;

                return ground;
            }

            var downItemStackToReverse = new Stack<IItem>();
            var top1ItemStackToReverse = new Stack<IItem>();
            var top2ItemStackToReverse = new Stack<IItem>();

            var keepLooking = true;
            IItem itemFound = null;

            while (keepLooking && _topItems1OnTile.Count > 0)
            {
                var item = _topItems1OnTile.Pop();

                if (item.ThingId == id)
                {
                    itemFound = item;
                    keepLooking = false;
                    continue;
                }

                top1ItemStackToReverse.Push(item);
            }

            while (keepLooking && _topItems2OnTile.Count > 0)
            {
                var item = _topItems2OnTile.Pop();

                if (item.ThingId == id)
                {
                    itemFound = item;
                    keepLooking = false;
                    break;
                }

                top2ItemStackToReverse.Push(item);
            }

            while (keepLooking && _downItemsOnTile.Count > 0)
            {
                var item = _downItemsOnTile.Pop();

                if (item.ThingId == id)
                {
                    itemFound = item;
                    break;
                }

                downItemStackToReverse.Push(item);
            }

            // Reverse and add the stacks back
            while (top1ItemStackToReverse.Count > 0)
            {
                AddTopItem1(top1ItemStackToReverse.Pop());
            }

            while (top2ItemStackToReverse.Count > 0)
            {
                AddTopItem2(top2ItemStackToReverse.Pop());
            }

            while (downItemStackToReverse.Count > 0)
            {
                AddDownItem(downItemStackToReverse.Pop());
            }

            return itemFound;
        }

        public IThing GetThingAtStackPosition(byte stackPosition)
        {

            if (stackPosition == 0 && Ground != null)
            {
                return Ground;
            }

            var currentPos = Ground == null ? -1 : 0;

            if (stackPosition > currentPos + _topItems1OnTile.Count)
            {
                currentPos += _topItems1OnTile.Count;
            }
            else
            {
                foreach (var item in TopItems1)
                {
                    if (++currentPos == stackPosition)
                    {
                        return item;
                    }
                }
            }

            if (stackPosition > currentPos + _topItems2OnTile.Count)
            {
                currentPos += _topItems2OnTile.Count;
            }
            else
            {
                foreach (var item in TopItems2)
                {
                    if (++currentPos == stackPosition)
                    {
                        return item;
                    }
                }
            }

            if (stackPosition > currentPos + _creatureIdsOnTile.Count)
            {
                currentPos += _creatureIdsOnTile.Count;
            }
            else
            {
                foreach (var creatureId in CreatureIds)
                {
                    if (++currentPos == stackPosition)
                    {
                        //    return Game.Instance.GetCreatureWithId(creatureId);
                    }
                }
            }

            return stackPosition <= currentPos + _downItemsOnTile.Count ? DownItems.FirstOrDefault(item => ++currentPos == stackPosition) : null;
        }

        public byte GetStackPositionOfThing(IThing thing)
        {
            if (thing == null)
            {
                throw new ArgumentNullException(nameof(thing));
            }

            if (Ground != null && thing == Ground)
            {
                return 0;
            }

            var n = 0;

            foreach (var item in TopItems1)
            {
                ++n;
                if (thing == item)
                {
                    return (byte)n;
                }
            }

            foreach (var item in TopItems2)
            {
                ++n;
                if (thing == item)
                {
                    return (byte)n;
                }
            }

            foreach (var creatureId in CreatureIds)
            {
                ++n;

                if (thing is ICreature creature && creature.CreatureId == creatureId)
                {
                    return (byte)n;
                }
            }

            foreach (var item in DownItems)
            {
                ++n;
                if (thing == item)
                {
                    return (byte)n;
                }
            }

            // return byte.MaxValue; // TODO: throw?
            throw new Exception("Thing not found in tile.");
        }

        public void SetFlag(TileFlag flag)
        {
            Flags |= (byte)flag;
        }

        public bool HasCollision
        {
            get
            {
                return Ground != null && (ItemsWithCollision?.Any() ?? false);
            }
        }

    }
}