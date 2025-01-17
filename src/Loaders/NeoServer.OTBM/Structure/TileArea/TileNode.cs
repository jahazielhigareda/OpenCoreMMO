using System;
using System.Collections.Generic;
using NeoServer.Game.Common.Location;
using NeoServer.Game.Common.Location.Structs;
using NeoServer.OTB.Enums;
using NeoServer.OTB.Parsers;
using NeoServer.OTB.Structure;
using NeoServer.OTBM.Enums;

namespace NeoServer.OTBM.Structure.TileArea;

public struct TileNode : ITileNode
{
    public Coordinate Coordinate { get; set; }
    public NodeAttribute NodeAttribute { get; set; }

    public uint HouseId { get; set; }

    public bool IsFlag => NodeAttribute == NodeAttribute.TileFlags;
    public bool IsItem => NodeAttribute == NodeAttribute.Item;
    public NodeType NodeType { get; }
    public TileFlags Flag { get; set; }
    public List<ItemNode> Items { get; set; }

    // public abstract void LoadTile(OTBParsingStream stream); //template method

    public TileNode(TileArea tileArea, OTBNode node)
    {
        Items = new List<ItemNode>();
        NodeAttribute = NodeAttribute.None;
        Flag = TileFlags.None;
        HouseId = default;

        var stream = new OTBParsingStream(node.Data);

        var x = (ushort)(tileArea.X + stream.ReadByte());
        var y = (ushort)(tileArea.Y + stream.ReadByte());

        Coordinate = new Coordinate(x, y, tileArea.Z);

        if (node.Type == NodeType.HouseTile) HouseId = stream.ReadUInt32();

        NodeType = node.Type;

        ParseAttributes(stream);

        var tileNode = this;

        foreach (var c in node.Children) Items.Add(new ItemNode(tileNode, c));
    }

    private void ParseAttributes(OTBParsingStream stream)
    {
        while (!stream.IsOver)
        {
            NodeAttribute = (NodeAttribute)stream.ReadByte();

            if (IsFlag)
                Flag = ParseTileFlags((OTBMTileFlags)stream.ReadUInt32());
            else if (IsItem)
                Items.Add(new ItemNode(stream));
            else
                //Console.WriteLine($"{Coordinate}: Unknown tile attribute");
                throw new Exception($"{Coordinate}: Unknown tile attribute");
        }
    }

    private TileFlags ParseTileFlags(OTBMTileFlags newFlags)
    {
        var oldFlags = TileFlags.None;

        if ((newFlags & OTBMTileFlags.ProtectionZone) != 0)
            oldFlags |= TileFlags.ProtectionZone;
        else if ((newFlags & OTBMTileFlags.NoPvpZone) != 0)
            oldFlags |= TileFlags.NoPvpZone;
        else if ((newFlags & OTBMTileFlags.PvpZone) != 0)
            oldFlags |= TileFlags.PvpZone;

        if ((newFlags & OTBMTileFlags.NoLogout) != 0)
            oldFlags |= TileFlags.NoLogout;

        return oldFlags;
    }
}