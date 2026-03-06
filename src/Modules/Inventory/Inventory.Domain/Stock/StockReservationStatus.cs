namespace Inventory.Domain.Stock;

public enum StockReservationStatus
{
    Active = 1,
    Consumed = 2,
    Released = 3,
    Expired = 4,
}
