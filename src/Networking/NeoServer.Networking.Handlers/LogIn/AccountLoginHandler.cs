﻿using NeoServer.Data.Interfaces;
using NeoServer.Networking.Packets.Incoming;
using NeoServer.Networking.Packets.Outgoing.Login;
using NeoServer.Server.Common.Contracts.Network;
using NeoServer.Server.Configurations;

namespace NeoServer.Networking.Handlers.LogIn;

public class AccountLoginHandler : PacketHandler
{
    private readonly IAccountRepository _repositoryNeo;
    private readonly ServerConfiguration serverConfiguration;

    public AccountLoginHandler(IAccountRepository repositoryNeo, ServerConfiguration serverConfiguration)
    {
        _repositoryNeo = repositoryNeo;
        this.serverConfiguration = serverConfiguration;
    }

    public override async void HandlerMessage(IReadOnlyNetworkMessage message, IConnection connection)
    {
        var account = new AccountLoginPacket(message);

        connection.SetXtea(account.Xtea);

        if (account == null)
        {
            //todo: use option
            connection.Disconnect("Invalid account.");
            return;
        }

        if (!account.IsValid())
        {
            connection.Disconnect("Invalid account name or password."); //todo: use gameserverdisconnect
            return;
        }

        var foundedAccount = await _repositoryNeo.GetAccount(account.Account, account.Password);

        if (foundedAccount == null)
        {
            connection.Disconnect("Account name or password is not correct.");
            return;
        }

        connection.Send(new CharacterListPacket(foundedAccount, serverConfiguration.ServerName,
            serverConfiguration.ServerIp));
    }
}