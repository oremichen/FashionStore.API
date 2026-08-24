namespace FashionStore.Infrastructure.Contracts.Abstractions.Encryption
{
    public interface IRsaEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }
}
