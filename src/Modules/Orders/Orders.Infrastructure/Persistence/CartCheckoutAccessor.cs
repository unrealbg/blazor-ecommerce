using System.Data;
using BuildingBlocks.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Orders.Infrastructure.Persistence;

internal sealed class CartCheckoutAccessor(OrdersDbContext dbContext) : ICartCheckoutAccessor
{
    public async Task<CartCheckoutSnapshot?> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT
                               c."Id" AS cart_id,
                               c."CustomerId" AS customer_id,
                               l.product_id,
                               l.variant_id,
                               l.sku,
                               l.product_name,
                               l.variant_name,
                               l.image_url,
                               l.selected_options_json,
                               l.unit_currency,
                               l.unit_amount,
                               l.quantity
                           FROM cart.carts c
                           LEFT JOIN cart.cart_lines l ON l.cart_id = c."Id"
                           WHERE c."CustomerId" = @customerId;
                           """;

        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            if (dbContext.Database.CurrentTransaction is not null)
            {
                command.Transaction = dbContext.Database.CurrentTransaction.GetDbTransaction();
            }

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@customerId";
            parameter.Value = customerId;
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            Guid? cartId = null;
            string? normalizedCustomerId = null;
            var lines = new List<CartCheckoutLineSnapshot>();

            while (await reader.ReadAsync(cancellationToken))
            {
                cartId ??= reader.GetGuid(reader.GetOrdinal("cart_id"));
                normalizedCustomerId ??= reader.GetString(reader.GetOrdinal("customer_id"));

                if (reader.IsDBNull(reader.GetOrdinal("product_id")))
                {
                    continue;
                }

                lines.Add(new CartCheckoutLineSnapshot(
                    reader.GetGuid(reader.GetOrdinal("product_id")),
                    reader.GetGuid(reader.GetOrdinal("variant_id")),
                    reader.IsDBNull(reader.GetOrdinal("sku")) ? null : reader.GetString(reader.GetOrdinal("sku")),
                    reader.GetString(reader.GetOrdinal("product_name")),
                    reader.IsDBNull(reader.GetOrdinal("variant_name")) ? null : reader.GetString(reader.GetOrdinal("variant_name")),
                    reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
                    reader.IsDBNull(reader.GetOrdinal("selected_options_json")) ? null : reader.GetString(reader.GetOrdinal("selected_options_json")),
                    reader.GetString(reader.GetOrdinal("unit_currency")),
                    reader.GetDecimal(reader.GetOrdinal("unit_amount")),
                    reader.GetInt32(reader.GetOrdinal("quantity"))));
            }

            if (cartId is null || normalizedCustomerId is null)
            {
                return null;
            }

            return new CartCheckoutSnapshot(cartId.Value, normalizedCustomerId, lines);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task ClearCartAsync(Guid cartId, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM cart.cart_lines WHERE cart_id = {cartId}",
            cancellationToken);

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM cart.carts WHERE \"Id\" = {cartId}",
            cancellationToken);
    }
}
