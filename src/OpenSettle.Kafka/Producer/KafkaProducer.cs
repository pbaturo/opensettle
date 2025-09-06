using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using OpenSettle.Kafka.Configuration;

namespace OpenSettle.Kafka.Producer;

public sealed class KafkaProducer : IKafkaProducer
{
    private readonly ILogger<KafkaProducer> _logger;
    private readonly IProducer<string, byte[]> _kafkaProducer;

    public KafkaProducer(ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        ProducerBuilder<string, byte[]> producerBuilder =
            new(KafkaLocalDefaults.GetProducerConfig());
        _kafkaProducer = producerBuilder.Build();
    }

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

