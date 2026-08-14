using System.Security.Cryptography;
using System.Text;
using FashionStore.Application.Abstractions.Encryption;

namespace FashionStore.Application.Utils.Encryption
{
    public class RsaEncryptionService : IRsaEncryptionService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RsaEncryptionService> _logger;

        public RsaEncryptionService(IConfiguration configuration, ILogger<RsaEncryptionService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string Encrypt(string plainText)
        {
            _logger.LogDebug("Encrypting application data with RSA.");
            if (string.IsNullOrWhiteSpace(plainText))
            {
                throw new ArgumentException("Plain text cannot be null or empty.", nameof(plainText));
            }

            var publicKeyPem = GetRequiredSetting("EncryptionSettings:RsaPublicKeyPem");

            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = rsa.Encrypt(plainBytes, RSAEncryptionPadding.OaepSHA256);

            return Convert.ToBase64String(cipherBytes);
        }

        public string Decrypt(string cipherText)
        {
            _logger.LogDebug("Decrypting application data with RSA.");
            if (string.IsNullOrWhiteSpace(cipherText))
            {
                throw new ArgumentException("Cipher text cannot be null or empty.", nameof(cipherText));
            }

            var privateKeyPem = GetRequiredSetting("EncryptionSettings:RsaPrivateKeyPem");

            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            var cipherBytes = Convert.FromBase64String(cipherText);
            var plainBytes = rsa.Decrypt(cipherBytes, RSAEncryptionPadding.OaepSHA256);

            return Encoding.UTF8.GetString(plainBytes);
        }

        private string GetRequiredSetting(string key)
        {
            return _configuration[key]
                ?? throw new InvalidOperationException($"Missing encryption setting: {key}");
        }
    }
}
