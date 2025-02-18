// ReSharper disable InconsistentNaming;

using System;
using System.Text;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Logging;
using EasyNetQ.Tests.Mocking;
using NSubstitute;
using RabbitMQ.Client;
using Xunit;

namespace EasyNetQ.Tests;

public class When_using_default_consumer_error_strategy
{
    public When_using_default_consumer_error_strategy()
    {
        var customConventions = new Conventions(new DefaultTypeNameSerializer())
        {
            ErrorQueueNamingConvention = _ => "CustomEasyNetQErrorQueueName",
            ErrorExchangeNamingConvention = info => "CustomErrorExchangePrefixName." + info.RoutingKey
        };

        mockBuilder = new MockBuilder();

        var connectionConfiguration = new ConnectionConfiguration();
        var connection = new ConsumerConnection(
            Substitute.For<ILogger<ConsumerConnection>>(),
            connectionConfiguration,
            mockBuilder.ConnectionFactory,
            new EventBus(Substitute.For<ILogger<EventBus>>())
        );

        errorStrategy = new DefaultConsumerErrorStrategy(
            Substitute.For<ILogger<DefaultConsumerErrorStrategy>>(),
            connection,
            new JsonSerializer(),
            customConventions,
            new DefaultTypeNameSerializer(),
            new DefaultErrorMessageSerializer(),
            connectionConfiguration
        );

        const string originalMessage = "";
        var originalMessageBody = Encoding.UTF8.GetBytes(originalMessage);

        consumerExecutionContext = new ConsumerExecutionContext(
            (_, _, _, _) => Task.FromResult(AckStrategies.Ack),
            new MessageReceivedInfo("consumerTag", 0, false, "orginalExchange", "originalRoutingKey", "queue"),
            new MessageProperties
            {
                CorrelationId = string.Empty,
                AppId = string.Empty
            },
            originalMessageBody
        );
    }

    private readonly DefaultConsumerErrorStrategy errorStrategy;
    private readonly MockBuilder mockBuilder;
    private readonly ConsumerExecutionContext consumerExecutionContext;

    [Fact]
    public async Task Should_Ack_canceled_message()
    {
        var cancelAckStrategy = await errorStrategy.HandleConsumerCancelledAsync(consumerExecutionContext, default);

        Assert.Same(AckStrategies.NackWithRequeue, cancelAckStrategy);
    }

    [Fact]
    public async Task Should_Ack_failed_message()
    {
        var errorAckStrategy = await errorStrategy.HandleConsumerErrorAsync(consumerExecutionContext, new Exception(), default);

        Assert.Same(AckStrategies.Ack, errorAckStrategy);
    }

    [Fact]
    public async Task Should_use_exchange_name_from_custom_names_provider()
    {
        await errorStrategy.HandleConsumerErrorAsync(consumerExecutionContext, new Exception(), default);

        mockBuilder.Channels[0].Received().ExchangeDeclare("CustomErrorExchangePrefixName.originalRoutingKey", "direct", true);
    }

    [Fact]
    public async Task Should_use_queue_name_from_custom_names_provider()
    {
        await errorStrategy.HandleConsumerErrorAsync(consumerExecutionContext, new Exception(), default);

        mockBuilder.Channels[0].Received().QueueDeclare("CustomEasyNetQErrorQueueName", true, false, false, null);
    }
}
