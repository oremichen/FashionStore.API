using FashionStore.Domain.Abstractions.Payments;
using FashionStore.Domain.Constants;

namespace FashionStore.Infrastructure.Payments;

public sealed class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyDictionary<string, Func<IPaymentGateway>> _gatewayFactories;

    public PaymentGatewayFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _gatewayFactories = new Dictionary<string, Func<IPaymentGateway>>(StringComparer.OrdinalIgnoreCase)
        {
            [PaymentProviderKeys.Paystack] = () => _serviceProvider.GetRequiredService<PaystackPaymentGateway>()
        };
    }

    public IEnumerable<string> ActiveProviders => _gatewayFactories.Keys;

    public bool IsSupported(string providerKey)
    {
        return !string.IsNullOrWhiteSpace(providerKey) &&
            _gatewayFactories.ContainsKey(providerKey);
    }

    public IPaymentGateway Get(string providerKey)
    {
        if (string.IsNullOrWhiteSpace(providerKey))
        {
            throw new ArgumentException("Payment provider key is required.", nameof(providerKey));
        }

        if (!_gatewayFactories.TryGetValue(providerKey, out var factory))
        {
            throw new ArgumentException($"Payment provider '{providerKey}' is not configured. " +
                $"Supported providers: {string.Join(", ", _gatewayFactories.Keys)}", nameof(providerKey));
        }

        return factory();
    }
}
