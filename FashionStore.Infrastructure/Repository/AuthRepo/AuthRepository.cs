namespace FashionStore.Infrastructure.Repositories.AuthRepo
{
    public class AuthRepository
    {
        private readonly ILogger<AuthRepository> _logger;

        public AuthRepository(ILogger<AuthRepository> logger)
        {
            _logger = logger;
            _logger.LogDebug("Auth repository initialized.");
        }


    }
}
