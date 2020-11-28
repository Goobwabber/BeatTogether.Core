﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BeatTogether.Core.Messaging.Abstractions;
using BeatTogether.Core.Messaging.Messages;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace BeatTogether.Core.Messaging.Implementations
{
    public abstract class BaseUdpClient<TSession> : NetCoreServer.UdpClient
        where TSession : class, ISession, new()
    {
        public ISession Session { get; }

        private readonly IMessageSource _messageSource;
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly ILogger _logger;

        public BaseUdpClient(
            IPEndPoint endPoint,
            IMessageSource messageSource,
            IMessageDispatcher messageDispatcher)
            : base(endPoint)
        {
            Session = new TSession()
            {
                EndPoint = endPoint
            };

            _messageSource = messageSource;
            _messageDispatcher = messageDispatcher;
            _logger = Log.ForContext<BaseUdpClient<TSession>>();

            // TODO: Remove byte array allocation
            _messageDispatcher.OnSend += (session, buffer) => SendAsync(session.EndPoint, buffer.ToArray());
            _messageSource.Subscribe<AcknowledgeMessage>((session, message) =>
            {
                _messageDispatcher.Acknowledge(message.ResponseId, message.MessageHandled);
                return Task.CompletedTask;
            });
        }

        #region Protected Methods

        protected override void OnConnected() => ReceiveAsync();

        protected override void OnReceived(EndPoint endPoint, ReadOnlySpan<byte> buffer)
        {
            _logger.Verbose($"Handling OnReceived (EndPoint='{endPoint}', Size={buffer.Length}).");
            if (buffer.Length > 0)
                _messageSource.Signal(Session, buffer);
            ReceiveAsync();
        }

        protected override void OnError(SocketError error) =>
            _logger.Error($"Handling OnError (Error={error}).");

        #endregion
    }
}