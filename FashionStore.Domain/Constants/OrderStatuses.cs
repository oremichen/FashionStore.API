namespace FashionStore.Domain.Constants;

public static class OrderStatuses
{
    public const string PendingPayment = "PendingPayment";
    public const string Processing = "Processing";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public const string Returned = "Returned";
}
