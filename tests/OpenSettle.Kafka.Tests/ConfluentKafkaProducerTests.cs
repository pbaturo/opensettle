using System.Text;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using OpenSettle.Kafka.Producer;
using Xunit;
using Xunit.Abstractions;

namespace OpenSettle.Kafka.Tests;

[Trait("Category", "Integration")]
public class ConfluentKafkaProducerTests : IDisposable
{
    private const string KafkaHost = "localhost";
    private const int KafkaPort = 9092;  // Updated to match docker-compose.yml
    private readonly string _testTopic = "test-topic";
    private readonly IConsumer<string, byte[]>? _consumer;
    private readonly KafkaProducer? _producer;

    public ConfluentKafkaProducerTests(ITestOutputHelper output)
    {
        ILoggerFactory factory = LoggerFactory.Create(b =>
        b.AddXUnit(output)
         .SetMinimumLevel(LogLevel.Debug));

        ILogger<KafkaProducer> logger = factory.CreateLogger<KafkaProducer>();
        _producer = new KafkaProducer(logger);

        logger.LogInformation("Starting Kafka producer tests...");
        if (!IsKafkaAvailable())
        {
            logger.LogWarning("Kafka is not available at {Host}:{Port}, skipping tests.", KafkaHost, KafkaPort);
            return;
        }
        logger.LogInformation("Kafka is available at {Host}:{Port}, proceeding with tests.", KafkaHost, KafkaPort);
        _producer = new KafkaProducer(logger);
        logger.LogInformation("Kafka producer initialized.");
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = $"{KafkaHost}:{KafkaPort}",
            GroupId = "test-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
        _consumer.Subscribe(_testTopic);
    }

    [Fact]
    public async Task ShouldProduceMessageToKafkaAsync()
    {
        _ = _producer.Should().NotBeNull("because Kafka should be available");
        _ = _consumer.Should().NotBeNull("because Kafka should be available");

        // Arrange
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
        ProduceResult result = await _producer!.SendAsync(_testTopic, key, payload, headers, default);

        // Assert
        _ = result.Topic.Should().Be(_testTopic);
        _ = result.Offset.Should().BeGreaterOrEqualTo(0);

        // Verify message was received
        ConsumeResult<string, byte[]> consumeResult = _consumer!.Consume(TimeSpan.FromSeconds(5));
        _ = consumeResult.Should().NotBeNull();
        _ = consumeResult.Message.Key.Should().Be(key);
        _ = consumeResult.Message.Value.Should().BeEquivalentTo(payload);

        // Verify headers
        foreach (KeyValuePair<string, string> header in headers)
        {
            var headerValue = consumeResult.Message.Headers
                .First(h => h.Key == header.Key)
                .GetValueBytes();
            _ = Encoding.UTF8.GetString(headerValue).Should().Be(header.Value);
        }
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
        _consumer?.Dispose();
    }
}
