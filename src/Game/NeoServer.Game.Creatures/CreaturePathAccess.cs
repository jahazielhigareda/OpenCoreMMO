﻿using NeoServer.Game.Common.Contracts.Creatures;

namespace NeoServer.Game.Creatures;

public record CreaturePathAccess(PathFinder FindPathToDestination, CanGoToDirection CanGoToDirection) : IPathAccess;