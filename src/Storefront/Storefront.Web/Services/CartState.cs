namespace Storefront.Web.Services;

public sealed class CartState
{
    public event Action? Changed;

    public int ItemCount { get; private set; }

    public void SetItemCount(int itemCount)
    {
        ItemCount = Math.Max(0, itemCount);
        Changed?.Invoke();
    }

    public void Increase(int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        ItemCount += quantity;
        Changed?.Invoke();
    }
}
