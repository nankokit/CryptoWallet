using System.Data.SQLite;
using System.IO;
using System.Collections.Generic;
using CryptoWallet.Models.Common;
using CryptoWallet.Models;
using CryptoWallet.Models.Ethereum;
using CryptoWallet.Models.Bitcoin;
using CryptoWallet.Models.ERC20;
using System;

namespace CryptoWallet
{
    public static class DatabaseService
    {
        private static readonly string DbPath = Path.Combine(Directory.GetCurrentDirectory(), "wallet.db");

        public static void InitializeDatabase()
        {
            if (!File.Exists(DbPath))
            {
                SQLiteConnection.CreateFile(DbPath);
            }

            using var connection = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            connection.Open();

            string userSql = @"CREATE TABLE IF NOT EXISTS Users (
                               UserId TEXT PRIMARY KEY,
                               SeedHash TEXT NOT NULL
                             )";
            using var userCommand = new SQLiteCommand(userSql, connection);
            userCommand.ExecuteNonQuery();

            string walletSql = @"CREATE TABLE IF NOT EXISTS Wallets (
                                 WalletId INTEGER PRIMARY KEY AUTOINCREMENT,
                                 UserId TEXT NOT NULL,
                                 Address TEXT NOT NULL,
                                 EncryptedPrivateKey TEXT NOT NULL,
                                 CurrencyName TEXT NOT NULL,
                                 ContractAddress TEXT,
                                 FOREIGN KEY(UserId) REFERENCES Users(UserId)
                               )";
            using var walletCommand = new SQLiteCommand(walletSql, connection);
            walletCommand.ExecuteNonQuery();
        }

        public static void SaveUser(string userId, string seedPhrase)
        {
            string seedHash = BCrypt.Net.BCrypt.HashPassword(seedPhrase);
            using var connection = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            connection.Open();
            string sql = "INSERT OR REPLACE INTO Users (UserId, SeedHash) VALUES (@userId, @seedHash)";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@seedHash", seedHash);
            command.ExecuteNonQuery();
        }

        public static bool VerifyUser(string userId, string seedPhrase)
        {
            using var connection = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            connection.Open();
            string sql = "SELECT SeedHash FROM Users WHERE UserId = @userId";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@userId", userId);
            string storedHash = (string)command.ExecuteScalar();

            return storedHash != null && BCrypt.Net.BCrypt.Verify(seedPhrase, storedHash);
        }

        public static void SaveWallet(string userId, Wallet wallet, string seedPhrase)
        {
            using var connection = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            connection.Open();
            string sql = @"INSERT INTO Wallets (UserId, Address, EncryptedPrivateKey, CurrencyName, ContractAddress) 
                           VALUES (@userId, @address, @encryptedPrivateKey, @currencyName, @contractAddress)";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@address", wallet.Address);
            command.Parameters.AddWithValue("@encryptedPrivateKey", Encryptor.Encrypt(wallet.PrivateKey, seedPhrase));
            command.Parameters.AddWithValue("@currencyName", wallet.Service.CurrencyName);
            command.Parameters.AddWithValue("@contractAddress", wallet.ContractAddress ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
        }

        public static List<Wallet> LoadWallets(string userId, string seedPhrase)
        {
            var wallets = new List<Wallet>();
            using var connection = new SQLiteConnection($"Data Source={DbPath};Version=3;");
            connection.Open();
            string sql = "SELECT Address, EncryptedPrivateKey, CurrencyName, ContractAddress FROM Wallets WHERE UserId = @userId";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@userId", userId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                string address = reader.GetString(0);
                string encryptedPrivateKey = reader.GetString(1);
                string currencyName = reader.GetString(2);
                string contractAddress = reader.IsDBNull(3) ? null : reader.GetString(3);

                string privateKey;
                try
                {
                    privateKey = Encryptor.Decrypt(encryptedPrivateKey, seedPhrase);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error decrypting private key: {ex.Message}");
                    continue;
                }

                ICryptoService service;
                if (currencyName == "Bitcoin")
                {
                    service = new BitcoinService();
                }
                else if (contractAddress != null)
                {
                    service = new ERC20TokenService(privateKey, contractAddress);
                }
                else
                {
                    service = new EthereumService(privateKey);
                }

                wallets.Add(new Wallet
                {
                    Address = address,
                    PrivateKey = privateKey,
                    ContractAddress = contractAddress,
                    Service = service
                });
            }

            return wallets;
        }
    }
}