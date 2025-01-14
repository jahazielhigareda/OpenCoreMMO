﻿using System.Collections.Generic;

namespace NeoServer.Server.Configurations;

public enum DatabaseType
{
    INMEMORY,
    MONGODB,
    MYSQL,
    MSSQL,
    SQLITE
}

public record ServerConfiguration(int Version, string OTBM, string OTB, string Data, string ServerName, string ServerIp,
    string Extensions, SaveConfiguration Save)
{
}

public record LogConfiguration(string MinimumLevel);

public record SaveConfiguration(uint Players);

public record DatabaseConfiguration(Dictionary<DatabaseType, string> Connections, DatabaseType Active);