using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OmegaStreamServices.Services
{
    public class EncryptionHelper : IEncryptionHelper
    {
        private readonly byte[] Key;
        private readonly byte[] IV;

        public EncryptionHelper(IConfiguration configuration)
        {
            Key = Convert.FromBase64String(configuration["EncryptionSettings:Key"]!);
            IV = Encoding.UTF8.GetBytes(configuration["EncryptionSettings:IV"]!);

            if (Key.Length != 32)
                throw new ArgumentException("Invalid key size. Key must be 32 bytes (256 bits) for AES-256.");

            if (IV.Length != 16)
                throw new ArgumentException("Invalid IV size. IV must be 16 bytes (128 bits) for AES.");
        }

        public string Encrypt(string text)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (var ms = new MemoryStream())
                using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cryptoStream))
                {
                    writer.Write(text);
                    writer.Flush();
                    cryptoStream.FlushFinalBlock();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public string Decrypt(string cipherText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (var ms = new MemoryStream(Convert.FromBase64String(cipherText)))
                using (var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cryptoStream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
