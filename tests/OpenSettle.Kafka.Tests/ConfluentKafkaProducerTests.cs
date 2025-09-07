using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using OpenSettle.Kafka.Producer;
using Xunit;
using Xunit.Abstractions;

namespace OpenSettle.Kafka.Tests;

[Trait("Category", "Integration")]
public class ConfluentKafkaProducerTests : IDisposable
{
    private const string KafkaHost = "localhost";
    private const int KafkaPort = 9092;
    private readonly string _testTopic = "test-topic";
    private readonly IConsumer<string, byte[]>? _consumer;
    private readonly KafkaProducer? _producer;
    private readonly ILogger<ConfluentKafkaProducerTests> _logger;

    public ConfluentKafkaProducerTests(ITestOutputHelper output)
    {
        ILoggerFactory factory = LoggerFactory.Create(b =>
            b.AddXUnit(output)
             .SetMinimumLevel(LogLevel.Debug));

        _logger = factory.CreateLogger<ConfluentKafkaProducerTests>();

        _logger.LogInformation("Starting Kafka producer tests...");

        if (!IsKafkaAvailable())
        {
            _logger.LogWarning("Kafka is not available at {Host}:{Port}, skipping tests.", KafkaHost, KafkaPort);
            return;
        }

        _logger.LogInformation("Kafka is available at {Host}:{Port}, proceeding with tests.", KafkaHost, KafkaPort);

        ILogger<KafkaProducer> producerLogger = factory.CreateLogger<KafkaProducer>();
        _producer = new KafkaProducer(producerLogger);
        _logger.LogInformation("Kafka producer initialized.");

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = $"{KafkaHost}:{KafkaPort}",
            GroupId = "test-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
        _consumer.Subscribe(_testTopic);
        _logger.LogInformation("Kafka consumer initialized and subscribed to topic: {Topic}", _testTopic);
    }

    [Fact]
    public async Task ShouldProduceMessageToKafkaAsync()
    {
        // Arrange
        Assert.NotNull(_producer);
        Assert.NotNull(_consumer);

        _logger.LogInformation("Starting message production test");

        var key = "test-key";
        var payload = Encoding.UTF8.GetBytes("{\"test\":\"data\"}");
        var headers = new Dictionary<string, string>
        {
            ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
            ["correlation-id"] = "test-correlation",
            ["idempotency-key"] = "test-idempotency",
            ["schema-version"] = "1.0"
        };

        // Act
        _logger.LogInformation("Sending message with key: {Key}", key);
        ProduceResult result = await _producer.SendAsync(_testTopic, key, payload, headers, default);

        // Assert
        Assert.Equal(_testTopic, result.Topic);
        Assert.True(result.Offset >= 0);
        _logger.LogInformation("Message produced to topic: {Topic}, partition: {Partition}, offset: {Offset}",
            result.Topic, result.Partition, result.Offset);

        // Verify message was received
        _logger.LogInformation("Consuming message to verify delivery");
        ConsumeResult<string, byte[]> consumeResult = _consumer.Consume(TimeSpan.FromSeconds(5));

        Assert.NotNull(consumeResult);
        Assert.NotNull(consumeResult.Message);
        Assert.Equal(key, consumeResult.Message.Key);
        Assert.Equal(payload, consumeResult.Message.Value);

        _logger.LogInformation("Message consumed successfully with key: {Key}", consumeResult.Message.Key);

        // Verify headers
        foreach (KeyValuePair<string, string> header in headers)
        {
            IHeader? kafkaHeader = consumeResult.Message.Headers
                .FirstOrDefault(h => h.Key == header.Key);

            Assert.NotNull(kafkaHeader);

            byte[] headerValue = kafkaHeader.GetValueBytes();
            string headerStringValue = Encoding.UTF8.GetString(headerValue);

            Assert.Equal(header.Value, headerStringValue);
            _logger.LogInformation("Header verified - {Key}: {Value}", header.Key, headerStringValue);
        }

        _logger.LogInformation("All assertions passed successfully");
    }

    private static bool IsKafkaAvailable()
    {
        try
        {
            using var socket = new System.Net.Sockets.TcpClient();
            socket.Connect(KafkaHost, KafkaPort);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _logger?.LogInformation("Disposing test resources");
        _consumer?.Dispose();
        _logger?.LogInformation("Test cleanup completed");
    }
}
