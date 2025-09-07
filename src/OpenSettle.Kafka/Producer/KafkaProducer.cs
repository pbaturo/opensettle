using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using OpenSettle.Kafka.Configuration;

namespace OpenSettle.Kafka.Producer;

/// <summary>
/// Implementation of IKafkaProducer using Confluent.Kafka library.
/// Provides message production capabilities to Kafka topics with structured logging.
/// </summary>
public sealed class KafkaProducer : IKafkaProducer
{
    private readonly ILogger<KafkaProducer> _logger;
    private readonly IProducer<string, byte[]> _kafkaProducer;

    /// <summary>
    /// Initializes a new instance of the KafkaProducer class.
    /// </summary>
    /// <param name="logger">Logger instance for structured logging of produce operations.</param>
    public KafkaProducer(ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        ProducerBuilder<string, byte[]> producerBuilder =
            new(KafkaLocalDefaults.GetProducerConfig());
        _kafkaProducer = producerBuilder.Build();
    }

    /// <summary>
    /// Sends a message to the specified Kafka topic asynchronously.
    /// </summary>
    /// <param name="topic">The Kafka topic to send the message to.</param>
    /// <param name="key">The message key used for partitioning and ordering.</param>
    /// <param name="payload">The message payload as UTF-8 encoded JSON bytes.</param>
    /// <param name="headers">
    /// Message headers that must include: traceparent, correlation-id, idempotency-key, and schema-version.
    /// </param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous send operation.
    /// The task result contains the produce result with topic, partition, and offset information.
    /// </returns>
    public async Task<ProduceResult> SendAsync(
        string topic,
        string key,
        ReadOnlyMemory<byte> payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken ct = default)
    {
        Message<string, byte[]> kafkaMsg = new()
        {
            Key = key,
            Value = payload.ToArray(),
            Headers = headers.Aggregate(new Headers(), static (acc, kv) =>
            {
                acc.Add(kv.Key, Encoding.UTF8.GetBytes(kv.Value));
                return acc;
            })
        };

        DeliveryResult<string, byte[]> deliveryResult =
            await _kafkaProducer.ProduceAsync(topic, kafkaMsg, ct);

        _logger.LogInformation(
            "Produced to {Topic} p{Partition} @ {Offset}",
            deliveryResult.Topic, deliveryResult.Partition, deliveryResult.Offset);

        // Adjust to your actual ProduceResult signature
        return new ProduceResult(deliveryResult.Topic, deliveryResult.Partition, deliveryResult.Offset);
    }
}

