using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CryptoWallet.Models.Common
{
    internal class Encryptor
    {
        private const int KeySize = 32; 
        private const int Iterations = 10000; // PBKDF2

        public static string Encrypt(string plainText, string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16); 
            byte[] iv = RandomNumberGenerator.GetBytes(16);  

            byte[] key = GenerateKey(password, salt); 

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var memoryStream = new MemoryStream();
            memoryStream.Write(salt, 0, salt.Length);
            memoryStream.Write(iv, 0, iv.Length);   

            using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(plainText);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public static string Decrypt(string cipherText, string password)
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            using var memoryStream = new MemoryStream(cipherBytes);
            byte[] salt = new byte[16];
            byte[] iv = new byte[16];

            memoryStream.Read(salt, 0, salt.Length); 
            memoryStream.Read(iv, 0, iv.Length);  

            byte[] key = GenerateKey(password, salt);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var streamReader = new StreamReader(cryptoStream);
            return streamReader.ReadToEnd();
        }

        private static byte[] GenerateKey(string password, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(KeySize);
        }
    }
}
