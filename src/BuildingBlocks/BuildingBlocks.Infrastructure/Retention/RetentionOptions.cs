namespace BuildingBlocks.Infrastructure.Retention;

public sealed class RetentionOptions
{
    public const string SectionName = "Retention";

    public int ProcessedOutboxDays { get; set; } = 30;

    public int WebhookPayloadDays { get; set; } = 90;

    public int AuditLogDays { get; set; } = 3650;

    public int ExpiredReservationDays { get; set; } = 30;

    public int ResolvedReportDays { get; set; } = 365;

    public int AbandonedCartDays { get; set; } = 60;

    public int SweepIntervalMinutes { get; set; } = 60;

    public TimeSpan SweepInterval => TimeSpan.FromMinutes(Math.Max(5, SweepIntervalMinutes));
}