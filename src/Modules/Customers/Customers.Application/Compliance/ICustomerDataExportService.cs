namespace Customers.Application.Compliance;

public interface ICustomerDataExportService
{
    Task<CustomerDataExportDto?> ExportByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}