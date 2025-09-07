namespace OpenSettle.Kafka.Producer;

/// <summary>
/// Defines a contract for producing messages to Kafka topics.
/// </summary>
public interface IKafkaProducer
{
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
    Task<ProduceResult> SendAsync(string topic,
        string key,                                  // for partitioning & ordering
        ReadOnlyMemory<byte> payload,                // UTF-8 JSON
        IReadOnlyDictionary<string, string> headers, // must include traceparent, correlation-id, idempotency-key, schema-version
        CancellationToken ct = default);
}

/// <summary>
/// Represents the result of a successful message production to Kafka.
/// </summary>
/// <param name="Topic">The topic the message was sent to.</param>
/// <param name="Partition">The partition number where the message was stored.</param>
/// <param name="Offset">The offset of the message within the partition.</param>
public readonly record struct ProduceResult(
    string Topic, int Partition, long Offset);

