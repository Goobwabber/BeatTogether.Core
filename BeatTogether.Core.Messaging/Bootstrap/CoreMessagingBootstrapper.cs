﻿using System.Security.Cryptography;
using BeatTogether.Core.Hosting.Extensions;
using BeatTogether.Core.Messaging.Abstractions;
using BeatTogether.Core.Messaging.Configuration;
using BeatTogether.Core.Messaging.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BeatTogether.Core.Messaging.Bootstrap
{
    public static class CoreMessagingBootstrapper
    {
        public static void ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
        {
            services.AddConfiguration<MessagingConfiguration>(hostBuilderContext.Configuration, "Messaging");
            services.AddConfiguration<RabbitMQConfiguration>(hostBuilderContext.Configuration, "Messaging:RabbitMQ");
            services.AddTransient<RNGCryptoServiceProvider>();
            services.AddTransient(serviceProvider =>
                new AesCryptoServiceProvider()
                {
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.None
                }
            );
            services.AddSingleton<IMessageReader, MessageReader>();
            services.AddSingleton<IMessageWriter, MessageWriter>();
            services.AddSingleton<IEncryptedMessageReader, EncryptedMessageReader>();
            services.AddSingleton<IEncryptedMessageWriter, EncryptedMessageWriter>();
        }
    }
}
