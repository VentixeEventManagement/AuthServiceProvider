﻿using Azure.Messaging.ServiceBus;
using Presentation.Interfaces;

namespace Presentation.Services;

public class AuthServiceBusHandler : IAuthServiceBusHandler
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public AuthServiceBusHandler(IConfiguration configuration)
    {
        _client = new ServiceBusClient(configuration["ServiceBus:ConnectionString"]);
        _sender = _client.CreateSender(configuration["ServiceBus:AccountVerification"]);
    }

    public virtual async Task PublishAsync(string payload)
    {
        var message = new ServiceBusMessage(payload);
        await _sender.SendMessageAsync(message);
    }

}
