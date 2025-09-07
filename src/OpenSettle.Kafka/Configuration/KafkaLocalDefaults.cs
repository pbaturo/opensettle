using Confluent.Kafka;

namespace OpenSettle.Kafka.Configuration;

/// <summary>
/// Hardcoded, local-only defaults for running against Redpanda in Docker.
/// Centralize all literals here to be moved to Options/appsettings later.
/// </summary>
public static class KafkaLocalDefaults
{
    /// <summary>
    /// Gets the producer configuration for local development with Redpanda.
    /// </summary>
    /// <returns>A configured ProducerConfig instance with local defaults.</returns>
    public static ProducerConfig GetProducerConfig()
    {
        ProducerConfig producerConfig = new()
        {
            BootstrapServers = Common.BootstrapServers,
            ClientId = Common.ClientId,
            EnableIdempotence = Producer.EnableIdempotence,
            Acks = Producer.AcksLevel,
            MessageTimeoutMs = Producer.MessageTimeoutMs,
            CompressionType = Producer.Compression,
            LingerMs = Producer.LingerMs,
            SocketKeepaliveEnable = true,
            EnableDeliveryReports = true
        };
        return producerConfig;
    }

    /// <summary>
    /// Common configuration values shared across producers and consumers.
    /// </summary>
    public static class Common
    {
        /// <summary>Host apps on your machine connect to Redpanda via localhost:9092.</summary>
        public const string BootstrapServers = "localhost:9092";

        /// <summary>Identifies the client in broker logs/metrics.</summary>
        public const string ClientId = "opensettle-dev";

        /// <summary>Default message schema version to stamp into headers.</summary>
        public const string SchemaVersion = "v1";
    }

    /// <summary>
    /// Configuration values specific to Kafka producers.
    /// </summary>
    public static class Producer
    {
        /// <summary>Enable idempotent producer to avoid duplicates on retries.</summary>
        public const bool EnableIdempotence = true;

        /// <summary>Durability level; map to Confluent config (e.g., Acks=All).</summary>
        public const Acks AcksLevel = Acks.All;

        /// <summary>Compression for payloads; tweak later if needed.</summary>
        public const CompressionType Compression = CompressionType.None;

        /// <summary>Batch linger (ms) before sending; 0 = send immediately.</summary>
        public const int LingerMs = 0;

        /// <summary>Fail a send if not acknowledged within this timeout (ms).</summary>
        public const int MessageTimeoutMs = 30_000;
    }

    /// <summary>
    /// Configuration values specific to Kafka consumers.
    /// </summary>
    public static class Consumer
    {
        /// <summary>Consumer group id (offsets are tracked per group).</summary>
        public const string GroupId = "opensettle-worker-dev";

        /// <summary>Manual commits after success (at-least-once).</summary>
        public const bool EnableAutoCommit = false;

        /// <summary>Where to start if no committed offsets exist.</summary>
        public const string AutoOffsetReset = "Earliest";

        /// <summary>Max time (ms) between polls before considered unresponsive.</summary>
        public const int MaxPollIntervalMs = 300_000;

        /// <summary>Heartbeat/session timeout (ms).</summary>
        public const int SessionTimeoutMs = 10_000;
    }

    /// <summary>
    /// Kafka topic names used by the application.
    /// </summary>
    public static class Topics
    {
        /// <summary>Primary event stream for new payments.</summary>
        public const string PaymentsCreated = "payments.created";

        /// <summary>Dead-letter topic for poison messages from PaymentsCreated.</summary>
        public const string PaymentsCreatedDlq = "payments.created.dlq";
    }

    /// <summary>
    /// Administrative configuration for topic management and maintenance.
    /// </summary>
    public static class Admin
    {
        /// <summary>Create topics on startup in local dev (idempotent).</summary>
        public const bool EnsureTopicsOnStart = true;

        /// <summary>Partition count for payments.created.</summary>
        public const int PaymentsCreatedPartitions = 3;

        /// <summary>Retention for payments.created in hours (7 days).</summary>
        public const int PaymentsCreatedRetentionHours = 168;

        /// <summary>Partition count for DLQ.</summary>
        public const int DlqPartitions = 3;

        /// <summary>Retention for DLQ in hours (30 days).</summary>
        public const int DlqRetentionHours = 720;
    }
}
