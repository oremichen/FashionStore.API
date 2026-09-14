namespace FashionStore.Domain.Abstractions.Payments;

public interface IPaymentGatewayFactory
{
    IPaymentGateway Get(string providerKey);

    IEnumerable<string> ActiveProviders { get; }

    bool IsSupported(string providerKey);
}
