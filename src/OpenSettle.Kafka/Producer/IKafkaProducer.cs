namespace OpenSettle.Kafka.Producer;

public interface IKafkaProducer
{
    Task<ProduceResult> SendAsync(string topic,
        string key,                                  // for partitioning & ordering
        ReadOnlyMemory<byte> payload,                // UTF-8 JSON
        IReadOnlyDictionary<string, string> headers, // must include traceparent, correlation-id, idempotency-key, schema-version
        CancellationToken ct = default);
}

public readonly record struct ProduceResult(
    string Topic, int Partition, long Offset);

