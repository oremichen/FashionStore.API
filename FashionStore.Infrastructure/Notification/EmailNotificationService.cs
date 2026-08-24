namespace FashionStore.Infrastructure.Notification
{
    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly IReadOnlyList<IEmailProvider> _providers;
        private readonly ILogger<EmailNotificationService> _logger;
        private readonly INotificationRepository _notificationRepository;
        private readonly IEmailNotificationQueueService _emailNotificationQueueService;

        public EmailNotificationService(
            IEnumerable<IEmailProvider> providers,
            IConfiguration configuration,
            ILogger<EmailNotificationService> logger,
            INotificationRepository notificationRepository,
            IEmailNotificationQueueService emailNotificationQueueService)
        {
            _providers = OrderProviders(providers, configuration);
            _logger = logger;
            _notificationRepository = notificationRepository;
            _emailNotificationQueueService = emailNotificationQueueService;
        }

        public async Task QueueEmailAsync(EmailNotification notification, CancellationToken cancellationToken = default)
        {
            var queuedNotification = await _notificationRepository.CreateProcessingAsync(notification, cancellationToken);
            _emailNotificationQueueService.Enqueue(queuedNotification.Id, notification);
        }

        public async Task<ResponseResult> SendEmailAsync(EmailNotification notification)
        {
            var response = new ResponseResult();
            if (notification.To == null || notification.To.Count == 0)
            {
                _logger.LogError("Email request rejected because no recipient was provided.");
                return response.Fail("At least one recipient is required.", ResponseCodes.INVALID_ACTION);
            }

            if (_providers.Count == 0)
            {
                _logger.LogError("Email request rejected because no email providers are registered.");
                return response.Fail("No email provider is configured.", ResponseCodes.SERVICE_UNAVAILABLE);
            }

            var providerErrors = new List<string>();
            foreach (var provider in _providers)
            {
                _logger.LogInformation(
                    "Attempting email delivery through {Provider}. Subject: {Subject}. First recipient: {Recipient}.",
                    provider.Name, notification.Subject, notification.To[0]);

                try
                {
                    var result = await provider.SendAsync(notification);
                    if (result.IsSuccessful)
                    {
                        _logger.LogInformation(
                            "Email sent successfully through {Provider}. Subject: {Subject}. First recipient: {Recipient}.",
                            provider.Name, notification.Subject, notification.To[0]);
                        return response.Success($"Email sent successfully through {provider.Name}.");
                    }

                    providerErrors.Add($"{provider.Name}: {result.Error ?? "Unknown provider error."}");
                    _logger.LogWarning(
                        "Email delivery through {Provider} failed; trying the next configured provider. Error: {Error}",
                        provider.Name, result.Error);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    providerErrors.Add($"{provider.Name}: Unexpected provider error.");
                    _logger.LogError(exception,
                        "Email delivery through {Provider} threw an exception; trying the next configured provider.",
                        provider.Name);
                }
            }

            _logger.LogError(
                "All configured email providers failed. Subject: {Subject}. First recipient: {Recipient}. Provider errors: {ProviderErrors}",
                notification.Subject, notification.To[0], string.Join(" | ", providerErrors));
            return response.Fail("All configured email providers failed.", ResponseCodes.SERVICE_UNAVAILABLE);
        }

        private static IReadOnlyList<IEmailProvider> OrderProviders(
            IEnumerable<IEmailProvider> providers,
            IConfiguration configuration)
        {
            var registered = providers.ToDictionary(provider => provider.Name, StringComparer.OrdinalIgnoreCase);
            var configuredOrder = configuration.GetSection("EmailProviders:ProviderOrder").Get<string[]>() ?? [];
            var ordered = configuredOrder
                .Where(registered.ContainsKey)
                .Select(name => registered[name])
                .ToList();

            ordered.AddRange(registered.Values.Where(provider => ordered.All(
                item => !string.Equals(item.Name, provider.Name, StringComparison.OrdinalIgnoreCase))));
            return ordered;
        }
    }
}
