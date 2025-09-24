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
    private const string TestTopic = "integration-test-topic"; // Use a consistent test topic
    private readonly KafkaProducer? _producer;
    private readonly ILogger<ConfluentKafkaProducerTests> _logger;

    public ConfluentKafkaProducerTests(ITestOutputHelper output)
    {
        ILoggerFactory factory = LoggerFactory.Create(b =>
            b.AddXUnit(output)
             .SetMinimumLevel(LogLevel.Debug));

        _logger = factory.CreateLogger<ConfluentKafkaProducerTests>();

        if (!IsKafkaAvailable())
        {
            _logger.LogWarning("Kafka is not available at {Host}:{Port}, skipping tests.", KafkaHost, KafkaPort);
            return;
        }

        _logger.LogInformation("Kafka is available at {Host}:{Port}, proceeding with tests.", KafkaHost, KafkaPort);

        ILogger<KafkaProducer> producerLogger = factory.CreateLogger<KafkaProducer>();
        _producer = new KafkaProducer(producerLogger);
        _logger.LogInformation("Kafka producer initialized.");
    }

    [Fact]
    public async Task ShouldProduceMessageToKafkaAsync()
    {
        // Arrange
        Assert.NotNull(_producer);

        var key = $"test-key-{Guid.NewGuid():N}";
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
        ProduceResult result = await _producer.SendAsync(TestTopic, key, payload, headers, default);

        // Assert
        Assert.Equal(TestTopic, result.Topic);
        Assert.True(result.Offset >= 0);
        Assert.True(result.Partition >= 0);

        _logger.LogInformation("Message produced successfully to topic: {Topic}, partition: {Partition}, offset: {Offset}",
            result.Topic, result.Partition, result.Offset);

        // Verify message exists by creating a dedicated consumer
        await VerifyMessageExistsAsync(key, payload, headers, result);
    }

    private Task VerifyMessageExistsAsync(string expectedKey, byte[] expectedPayload,
        Dictionary<string, string> expectedHeaders, ProduceResult produceResult)
    {
        _logger.LogInformation("Verifying message exists at offset {Offset}", produceResult.Offset);

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = $"{KafkaHost}:{KafkaPort}",
            GroupId = $"verify-group-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 6000,
            HeartbeatIntervalMs = 2000
        };

        using var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();

        // Assign to specific partition
        var topicPartition = new TopicPartition(TestTopic, new Partition(produceResult.Partition));
        consumer.Assign(new[] { topicPartition });

        // Wait for assignment to complete by polling once
        _logger.LogInformation("Waiting for partition assignment to complete...");
        try
        {
            // Poll once with a short timeout to establish connection
            consumer.Consume(TimeSpan.FromMilliseconds(100));
        }
        catch (ConsumeException)
        {
            // Expected if no messages available, assignment should still be complete
        }

        // Now seek to our message offset
        var targetOffset = new TopicPartitionOffset(topicPartition, new Offset(produceResult.Offset));

        _logger.LogInformation("Seeking to partition {Partition}, offset {Offset}",
            produceResult.Partition, produceResult.Offset);

        consumer.Seek(targetOffset);

        // Consume the specific message
        var consumeResult = consumer.Consume(TimeSpan.FromSeconds(10));

        Assert.NotNull(consumeResult);
        Assert.NotNull(consumeResult.Message);
        Assert.Equal(expectedKey, consumeResult.Message.Key);
        Assert.Equal(expectedPayload, consumeResult.Message.Value);
        Assert.Equal(produceResult.Offset, consumeResult.Offset.Value);

        _logger.LogInformation("Message verified successfully with key: {Key}", consumeResult.Message.Key);

        // Verify headers
        foreach (var expectedHeader in expectedHeaders)
        {
            var kafkaHeader = consumeResult.Message.Headers
                .FirstOrDefault(h => h.Key == expectedHeader.Key);

            Assert.NotNull(kafkaHeader);

            string headerValue = Encoding.UTF8.GetString(kafkaHeader.GetValueBytes());
            Assert.Equal(expectedHeader.Value, headerValue);

            _logger.LogInformation("Header verified - {Key}: {Value}", expectedHeader.Key, headerValue);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ShouldHandleKafkaUnavailableGracefully()
    {
        // Arrange
        _logger.LogInformation("Testing error handling for unavailable Kafka service");

        var config = new ProducerConfig
        {
            BootstrapServers = "localhost:9999", // Non-existent service
            MessageTimeoutMs = 5000,
            RequestTimeoutMs = 3000,
            MessageSendMaxRetries = 1
        };

        using var kafkaProducer = new ProducerBuilder<string, byte[]>(config).Build();

        var message = new Message<string, byte[]>
        {
            Key = "error-test-key",
            Value = Encoding.UTF8.GetBytes("{\"test\":\"error-handling\"}"),
            Headers = new Headers
            {
                { "traceparent", Encoding.UTF8.GetBytes("00-test-trace-01") }
            }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ProduceException<string, byte[]>>(
            async () => await kafkaProducer.ProduceAsync(TestTopic, message, CancellationToken.None));

        Assert.NotNull(exception);
        Assert.True(exception.Error.IsError);

        _logger.LogInformation("Caught expected exception: {ErrorCode} - {ErrorReason}",
            exception.Error.Code, exception.Error.Reason);
    }

    [Fact]
    public async Task ShouldHandleCancellationTokenGracefully()
    {
        // Skip if Kafka is not available
        if (_producer == null)
        {
            _logger.LogInformation("Skipping cancellation test - Kafka not available");
            return;
        }

        // Arrange
        var key = "cancellation-test-key";
        var payload = Encoding.UTF8.GetBytes("{\"test\":\"cancellation\"}");
        var headers = new Dictionary<string, string>
        {
            ["traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
            ["correlation-id"] = "cancellation-test",
            ["idempotency-key"] = "cancellation-test-key"
            // schema-version will be auto-injected by HeaderBinder
        };

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await _producer.SendAsync(TestTopic, key, payload, headers, cancellationTokenSource.Token));

        Assert.NotNull(exception);
        _logger.LogInformation("Cancellation handled correctly: {ExceptionType}", exception.GetType().Name);
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
        _logger?.LogInformation("Test cleanup completed");
    }
}
