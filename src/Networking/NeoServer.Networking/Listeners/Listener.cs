﻿using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NeoServer.Networking.Packets.Connection;
using NeoServer.Networking.Protocols;
using NeoServer.Server.Common.Contracts.Network;
using Serilog;

namespace NeoServer.Networking.Listeners;

public abstract class Listener : TcpListener, IListener
{
    private readonly ILogger _logger;
    private readonly IProtocol _protocol;

    protected Listener(int port, IProtocol protocol, ILogger logger) : base(IPAddress.Any, port)
    {
        _protocol = protocol;
        _logger = logger;
    }

    public void BeginListening(CancellationToken cancellationToken)
    {
        Task.Run(async () =>
        {
            Start();
            _logger.Information("{protocol} is online", _protocol);

            while (!cancellationToken.IsCancellationRequested)
            {
                var connection = await CreateConnection(cancellationToken);

                _protocol.OnAccept(connection);
            }
        }, cancellationToken);
    }

    public void EndListening()
    {
        Stop();
    }

    private async Task<IConnection> CreateConnection(CancellationToken cancellationToken)
    {
        var socket = await AcceptSocketAsync(cancellationToken).ConfigureAwait(false);

        var connection = new Connection(socket, _logger);

        connection.OnCloseEvent += OnConnectionClose;
        connection.OnProcessEvent += _protocol.ProcessMessage;
        connection.OnPostProcessEvent += _protocol.PostProcessMessage;
        return connection;
    }

    private void OnConnectionClose(object sender, IConnectionEventArgs args)
    {
        // De-subscribe to this event first.
        args.Connection.OnCloseEvent -= OnConnectionClose;
        args.Connection.OnProcessEvent -= _protocol.ProcessMessage;
        args.Connection.OnPostProcessEvent -= _protocol.PostProcessMessage;
    }
}