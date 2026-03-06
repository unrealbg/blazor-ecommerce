namespace Inventory.Domain.Stock;

public enum StockMovementType
{
    ManualAdjustment = 1,
    ReservationCreated = 2,
    ReservationReleased = 3,
    ReservationConsumed = 4,
    OrderCompleted = 5,
    OrderCancelled = 6,
    Restock = 7,
    ReservationExpired = 8,
    StockRestored = 9,
}
