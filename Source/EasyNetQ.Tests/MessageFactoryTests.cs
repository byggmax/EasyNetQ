using System;
using System.Collections.Generic;
using Xunit;

namespace EasyNetQ.Tests;

public class MessageFactoryTests
{
    [Theory]
    [MemberData(nameof(GetSerializers))]
    public void Should_correctly_create_generic_message(object message)
    {
        var correlationId = Guid.NewGuid().ToString();
        var properties = new MessageProperties { CorrelationId = correlationId };
        var genericMessageWithProperties = MessageFactory.CreateInstance(message.GetType(), message, properties);

        Assert.IsType(typeof(Message<>).MakeGenericType(message.GetType()), genericMessageWithProperties);
        Assert.Equal(message, genericMessageWithProperties.GetBody());
        Assert.Equal(genericMessageWithProperties.MessageType, message.GetType());
        Assert.Equal(genericMessageWithProperties.Properties.CorrelationId, correlationId);
    }

    public static IEnumerable<object[]> GetSerializers()
    {
        yield return new object[] { new MyMessage { Text = "Hello World" } };
        yield return new object[] { Guid.NewGuid() };
    }
}
