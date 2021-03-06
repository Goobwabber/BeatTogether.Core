﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BeatTogether.Core.Messaging.Abstractions;
using BeatTogether.Core.Messaging.Messages;
using NetCoreServer;
using Serilog;

namespace BeatTogether.Core.Messaging.Implementations
{
    public abstract class BaseUdpServer : UdpServer
    {
        private readonly IMessageSource _messageSource;
        private readonly IMessageDispatcher _messageDispatcher;
        private readonly ILogger _logger;

        public BaseUdpServer(
            IPEndPoint endPoint,
            IMessageSource messageSource,
            IMessageDispatcher messageDispatcher)
            : base(endPoint)
        {
            _messageSource = messageSource;
            _messageDispatcher = messageDispatcher;
            _logger = Log.ForContext<BaseUdpServer>();

            _messageDispatcher.OnSent += (session, buffer) =>
            {
                _logger.Verbose(
                    "Handling OnSent " +
                    $"(EndPoint='{session.EndPoint}', " +
                    $"Data='{BitConverter.ToString(buffer.ToArray())}')."
                );
                SendAsync(session.EndPoint, buffer);
            };
            _messageSource.Subscribe<AcknowledgeMessage>((session, message) =>
            {
                _messageDispatcher.Acknowledge(message.ResponseId, message.MessageHandled);
                return Task.CompletedTask;
            });
            _messageSource.Subscribe((session, message) =>
            {
                if (message is IReliableRequest reliableRequest)
                    _messageDispatcher.Send(session, new AcknowledgeMessage()
                    {
                        ResponseId = reliableRequest.RequestId,
                        MessageHandled = true
                    });
                return Task.CompletedTask;
            });
        }

        #region Abstract Methods

        protected abstract ISession GetSession(EndPoint endPoint);

        #endregion

        #region Protected Methods

        protected override void OnStarted() => ReceiveAsync();

        protected override void OnReceived(EndPoint endPoint, ReadOnlySpan<byte> buffer)
        {
            _logger.Verbose(
                "Handling OnReceived " +
                $"(EndPoint='{endPoint}', " +
                $"Data='{BitConverter.ToString(buffer.ToArray())}')."
            );
            if (buffer.Length > 0)
            {
                var session = GetSession(endPoint);
                _messageSource.Signal(session, buffer);
            }
            ReceiveAsync();
        }

        protected override void OnError(SocketError error) =>
            _logger.Error($"Handling OnError (Error={error}).");

        #endregion
    }
}
