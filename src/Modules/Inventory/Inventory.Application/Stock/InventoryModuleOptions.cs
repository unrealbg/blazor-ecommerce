namespace Inventory.Application.Stock;

public sealed class InventoryModuleOptions
{
    public const string SectionName = "Inventory";

    public int ReservationTtlMinutes { get; set; } = 30;

    public int ExpirationSweepSeconds { get; set; } = 60;

    public bool RefreshReservationOnCartMutation { get; set; } = true;

    public int RetryOnConcurrencyCount { get; set; } = 3;

    public bool ExposeExactStockPublicly { get; set; }

    public TimeSpan ReservationTtl => TimeSpan.FromMinutes(Math.Max(1, ReservationTtlMinutes));

    public TimeSpan ExpirationSweepInterval => TimeSpan.FromSeconds(Math.Max(10, ExpirationSweepSeconds));
}
