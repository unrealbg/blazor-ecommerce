using System.Text.Json;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;
using Orders.Domain.Orders;

namespace Orders.Application.Orders.Checkout;

internal static class OrderLinePricingSnapshotFactory
{
    public static Result<IReadOnlyCollection<OrderLineData>> Create(
        IReadOnlyCollection<CartCheckoutLineSnapshot> cartLines,
        CartPricingResult pricingResult)
    {
        var pricedLines = pricingResult.Lines.ToDictionary(line => line.VariantId);
        var lineTotals = cartLines
            .Select(line => new CartLineAllocation(
                line,
                pricedLines.TryGetValue(line.VariantId, out var pricedLine) ? pricedLine : null))
            .ToList();

        var missingLine = lineTotals.FirstOrDefault(item => item.PricedLine is null);
        if (missingLine is not null)
        {
            return Result<IReadOnlyCollection<OrderLineData>>.Failure(new Error(
                "orders.pricing.line_missing",
                "A priced cart line could not be resolved."));
        }

        var lineAmounts = lineTotals.ToDictionary(
            item => item.Line.VariantId,
            item => item.PricedLine!.LineTotalAmount);

        var cartDiscountAllocations = AllocateCartDiscounts(
            pricingResult,
            lineAmounts,
            pricingResult.CartDiscountTotalAmount);

        var orderLines = new List<OrderLineData>(cartLines.Count);
        foreach (var allocation in lineTotals)
        {
            var pricedLine = allocation.PricedLine!;
            var allocatedCartDiscounts = cartDiscountAllocations.TryGetValue(
                allocation.Line.VariantId,
                out var appliedCartDiscounts)
                ? appliedCartDiscounts
                : [];

            var allocatedCartDiscountAmount = Money.Round(
                allocatedCartDiscounts.Sum(discount => discount.Amount));
            var totalDiscountAmount = Money.Round(
                pricedLine.DiscountTotalAmount + allocatedCartDiscountAmount);
            var finalLineTotalAmount = Math.Max(
                0m,
                Money.Round(pricedLine.LineTotalAmount - allocatedCartDiscountAmount));
            var finalUnitAmount = allocation.Line.Quantity == 0
                ? 0m
                : Money.Round(finalLineTotalAmount / allocation.Line.Quantity);

            var finalUnitPriceResult = Money.Create(pricedLine.Currency, finalUnitAmount);
            if (finalUnitPriceResult.IsFailure)
            {
                return Result<IReadOnlyCollection<OrderLineData>>.Failure(finalUnitPriceResult.Error);
            }

            var appliedDiscounts = pricedLine.AppliedDiscounts
                .Concat(allocatedCartDiscounts)
                .ToArray();

            orderLines.Add(new OrderLineData(
                allocation.Line.ProductId,
                allocation.Line.VariantId,
                allocation.Line.Sku,
                allocation.Line.ProductName,
                allocation.Line.VariantName,
                allocation.Line.SelectedOptionsJson,
                finalUnitPriceResult.Value,
                allocation.Line.Quantity,
                pricedLine.BaseUnitPriceAmount,
                pricedLine.CompareAtUnitPriceAmount,
                totalDiscountAmount,
                SerializeLineDiscounts(appliedDiscounts)));
        }

        return Result<IReadOnlyCollection<OrderLineData>>.Success(orderLines);
    }

    private static IReadOnlyDictionary<Guid, List<PricingDiscountApplication>> AllocateCartDiscounts(
        CartPricingResult pricingResult,
        IReadOnlyDictionary<Guid, decimal> lineTotals,
        decimal expectedTotal)
    {
        var cartApplications = pricingResult.AppliedDiscounts
            .Where(application =>
                application.TargetLineVariantId is null &&
                string.Equals(application.ScopeType, "Cart", StringComparison.Ordinal))
            .ToArray();

        if (expectedTotal <= 0m || lineTotals.Count == 0)
        {
            return new Dictionary<Guid, List<PricingDiscountApplication>>();
        }

        if (cartApplications.Length == 0)
        {
            cartApplications =
            [
                new PricingDiscountApplication(
                    pricingResult.AppliedCouponCode is null ? "Promotion" : "Coupon",
                    Guid.Empty,
                    Guid.Empty,
                    null,
                    "Cart",
                    null,
                    pricingResult.AppliedCouponCode is null
                        ? "Allocated cart discount"
                        : $"Coupon {pricingResult.AppliedCouponCode}",
                    expectedTotal,
                    pricingResult.Currency,
                    pricingResult.AppliedCouponCode),
            ];
        }

        var result = lineTotals.Keys.ToDictionary(variantId => variantId, _ => new List<PricingDiscountApplication>());
        foreach (var application in cartApplications)
        {
            var allocated = AllocateAmountAcrossLines(application.Amount, lineTotals);
            foreach (var (variantId, amount) in allocated)
            {
                if (amount <= 0m)
                {
                    continue;
                }

                result[variantId].Add(application with
                {
                    TargetLineVariantId = variantId,
                    Amount = amount,
                });
            }
        }

        var actualTotal = Money.Round(result.Values.SelectMany(value => value).Sum(discount => discount.Amount));
        if (actualTotal != Money.Round(expectedTotal))
        {
            var delta = Money.Round(expectedTotal - actualTotal);
            if (delta != 0m)
            {
                var lastVariantId = lineTotals.Keys.Last();
                if (result[lastVariantId].Count > 0)
                {
                    var lastDiscount = result[lastVariantId][^1];
                    result[lastVariantId][^1] = lastDiscount with
                    {
                        Amount = Money.Round(lastDiscount.Amount + delta),
                    };
                }
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<Guid, decimal> AllocateAmountAcrossLines(
        decimal totalAmount,
        IReadOnlyDictionary<Guid, decimal> lineTotals)
    {
        var basisTotal = Money.Round(lineTotals.Values.Sum());
        if (totalAmount <= 0m || basisTotal <= 0m)
        {
            return lineTotals.Keys.ToDictionary(variantId => variantId, _ => 0m);
        }

        var allocations = new Dictionary<Guid, decimal>(lineTotals.Count);
        decimal allocated = 0m;
        var items = lineTotals.ToArray();
        for (var index = 0; index < items.Length; index++)
        {
            var (variantId, lineTotal) = items[index];
            decimal amount;
            if (index == items.Length - 1)
            {
                amount = Money.Round(totalAmount - allocated);
            }
            else
            {
                amount = Money.Round(totalAmount * (lineTotal / basisTotal));
                allocated = Money.Round(allocated + amount);
            }

            allocations[variantId] = Math.Max(0m, amount);
        }

        return allocations;
    }

    private static string? SerializeLineDiscounts(IReadOnlyCollection<PricingDiscountApplication> discounts)
    {
        return discounts.Count == 0 ? null : JsonSerializer.Serialize(discounts);
    }

    private sealed record CartLineAllocation(
        CartCheckoutLineSnapshot Line,
        CartPricingLineResult? PricedLine);
}
