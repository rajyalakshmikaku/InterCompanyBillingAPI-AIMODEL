
using System.Security.Cryptography;
using System.Text;
namespace InterCompanyBillingSulution_Core
{


    public class PasswordEncryptionService
    {
        private readonly byte[] _key;

        public PasswordEncryptionService(IConfiguration configuration)
        {
            var key = configuration["PasswordEncryption:Key"];

            if (string.IsNullOrWhiteSpace(key))
                throw new Exception("Password encryption key is missing.");

            _key = SHA256.HashData(
                Encoding.UTF8.GetBytes(key)
            );
        }

        public string Encrypt(string plainText)
        {
            using var aes = Aes.Create();

            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            byte[] cipherBytes = encryptor.TransformFinalBlock(
                plainBytes,
                0,
                plainBytes.Length
            );

            // IV + encrypted data
            byte[] result = aes.IV
                .Concat(cipherBytes)
                .ToArray();

            return Convert.ToBase64String(result);
        }

        public string Decrypt(string encryptedText)
        {
            byte[] data = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();

            aes.Key = _key;

            byte[] iv = data.Take(16).ToArray();
            byte[] cipherBytes = data.Skip(16).ToArray();

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();

            byte[] plainBytes = decryptor.TransformFinalBlock(
                cipherBytes,
                0,
                cipherBytes.Length
            );

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}