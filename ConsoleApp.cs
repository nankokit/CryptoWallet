using CryptoWallet.Models;
using CryptoWallet.Models.Common;
using CryptoWallet.Models.Ethereum;
using CryptoWallet.Models.Bitcoin;
using CryptoWallet.Models.ERC20;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace CryptoWallet
{
    public class ConsoleApp
    {
        private const string WalletFilePath = "wallets.json";
        private static string _currentUserId;
        private static string _seedPhrase;

        public static async Task Main(string[] args)
        {
            DatabaseService.InitializeDatabase();

            Console.WriteLine("Multi-Currency Crypto Wallet");
            Console.WriteLine("1. Register");
            Console.WriteLine("2. Login");
            Console.WriteLine("3. Recover Wallet");
            Console.Write("Select an option: ");
            string initialChoice = Console.ReadLine();

            if (initialChoice == "1")
            {
                RegisterUser();
            }
            else if (initialChoice == "2")
            {
                if (!LoginUser()) return;
            }
            else if (initialChoice == "3")
            {
                RecoverWallet();
                return;
            }
            else
            {
                Console.WriteLine("Invalid option. Exiting...");
                return;
            }

            List<Wallet> wallets;
            if (File.Exists(WalletFilePath))
            {
                wallets = LoadWallets(_seedPhrase);
                if (wallets == null)
                {
                    Console.WriteLine("Failed to unlock wallets. Exiting...");
                    return;
                }
                Console.WriteLine("Wallets loaded successfully.");
            }
            else
            {
                wallets = new List<Wallet>();
                Console.WriteLine("No wallet file found. Starting with an empty wallet list.");
            }

            bool running = true;
            while (running)
            {
                Console.WriteLine("\nMulti-Currency Crypto Wallet");
                Console.WriteLine("1. Add Wallet");
                Console.WriteLine("2. Select Wallet");
                Console.WriteLine("3. Save and Exit");
                Console.Write("Select an option: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        AddWallet(wallets);
                        break;
                    case "2":
                        await SelectWallet(wallets);
                        break;
                    case "3":
                        SaveWallets(wallets, _seedPhrase);
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
        }

        private static void RegisterUser()
        {
            Console.Write("Enter User ID (e.g., user123): ");
            _currentUserId = Console.ReadLine();
            if (string.IsNullOrEmpty(_currentUserId))
            {
                Console.WriteLine("Error: User ID cannot be empty.");
                return;
            }

            Mnemonic mnemonic = new Mnemonic(Wordlist.English, WordCount.Twelve);
            _seedPhrase = mnemonic.ToString();
            DatabaseService.SaveUser(_currentUserId, _seedPhrase);

            Console.WriteLine($"Your seed phrase: {_seedPhrase}");
            Console.WriteLine("Save it in a secure place! Do not share it with anyone.");
        }

        private static bool LoginUser()
        {
            Console.Write("Enter User ID: ");
            _currentUserId = Console.ReadLine();
            Console.Write("Enter your seed phrase: ");
            _seedPhrase = Console.ReadLine();

            if (DatabaseService.VerifyUser(_currentUserId, _seedPhrase))
            {
                Console.WriteLine("Login successful!");
                return true;
            }
            else
            {
                Console.WriteLine("Invalid User ID or Seed Phrase.");
                return false;
            }
        }

        private static void RecoverWallet()
        {
            Console.Write("Enter your seed phrase to recover wallet: ");
            string seedPhrase = Console.ReadLine();

            try
            {
                Mnemonic mnemonic = new Mnemonic(seedPhrase);
                ExtKey masterKey = mnemonic.DeriveExtKey();
                Console.WriteLine("Seed phrase is valid. Wallet recovered.");

                Wallet wallet = new Wallet(masterKey.PrivateKey);
                Console.WriteLine($"Recovered wallet address: {wallet.Address}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recovering wallet: {ex.Message}");
            }
        }

        private static void AddWallet(List<Wallet> wallets)
        {
            Console.WriteLine("\nAdd a new wallet:");
            Console.WriteLine("1. Add Ethereum Wallet");
            Console.WriteLine("2. Add ERC-20 Token Wallet");
            Console.WriteLine("3. Add Bitcoin Wallet");
            Console.WriteLine("4. Generate Wallet from Seed Phrase");
            Console.Write("Select an option: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    wallets.Add(CreateWallet("Ethereum"));
                    break;
                case "2":
                    wallets.Add(CreateWallet("ERC20"));
                    break;
                case "3":
                    wallets.Add(CreateBitcoinWallet());
                    break;
                case "4":
                    wallets.Add(CreateWalletFromSeedPhrase());
                    break;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }

        private static Wallet CreateWalletFromSeedPhrase()
        {
            try
            {
                Mnemonic mnemonic = new Mnemonic(_seedPhrase);
                ExtKey masterKey = mnemonic.DeriveExtKey();
                Key privateKey = masterKey.PrivateKey;

                return new Wallet(privateKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to generate wallet from seed phrase: {ex.Message}");
                return null;
            }
        }

        private static Wallet CreateWallet(string type)
        {
            Console.Write("Enter address (e.g., 0x15H7dd...): ");
            var address = Console.ReadLine();
            if (!address.StartsWith("0x") || address.Length != 42)
            {
                Console.WriteLine("Invalid Ethereum address format.");
                return null;
            }
            Console.Write("Enter private key (e.g., h25589...): ");
            var privateKey = Console.ReadLine();

            if (type == "ERC20")
            {
                Console.Write("Enter ERC-20 token contract address (e.g., 0x...): ");
                var contractAddress = Console.ReadLine();
                if (!contractAddress.StartsWith("0x") || contractAddress.Length != 42)
                {
                    Console.WriteLine("Invalid contract address format.");
                    return null;
                }

                try
                {
                    return new Wallet
                    {
                        Address = address,
                        PrivateKey = privateKey,
                        ContractAddress = contractAddress,
                        Service = new ERC20TokenService(privateKey, contractAddress)
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to initialize ERC-20 wallet: {ex.Message}");
                    return null;
                }
            }

            try
            {
                return new Wallet
                {
                    Address = address,
                    PrivateKey = privateKey,
                    Service = new EthereumService(privateKey)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize Ethereum wallet: {ex.Message}");
                return null;
            }
        }

        private static Wallet CreateBitcoinWallet()
        {
            Console.Write("Enter Bitcoin address: ");
            var address = Console.ReadLine();
            Console.Write("Enter Bitcoin private key: ");
            var privateKey = Console.ReadLine();

            return new Wallet
            {
                Address = address,
                PrivateKey = privateKey,
                Service = new BitcoinService()
            };
        }

        private static async Task SelectWallet(List<Wallet> wallets)
        {
            Console.WriteLine("\nSelect a wallet:");
            for (int i = 0; i < wallets.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {wallets[i].Service.CurrencyName} ({wallets[i].Address})");
            }
            Console.Write("Choice: ");
            if (int.TryParse(Console.ReadLine(), out int walletIndex) && walletIndex > 0 && walletIndex <= wallets.Count)
            {
                await WalletMenu(wallets[walletIndex - 1]);
            }
            else
            {
                Console.WriteLine("Invalid wallet selection.");
            }
        }

        private static async Task WalletMenu(Wallet wallet)
        {
            bool running = true;
            while (running)
            {
                Console.WriteLine($"\n{wallet.Service.CurrencyName} Wallet ({wallet.Address})");
                Console.WriteLine("1. Check Balance");
                Console.WriteLine("2. Send Funds");
                Console.WriteLine("3. Show Transaction History");
                Console.WriteLine("4. Back");
                Console.Write("Select an option: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        var balance = await wallet.Service.GetBalanceAsync(wallet.Address);
                        Console.WriteLine($"Balance: {balance} {wallet.Service.CurrencyName}");
                        break;

                    case "2":
                        Console.Write("Enter recipient address: ");
                        var toAddress = Console.ReadLine();
                        Console.Write("Enter amount: ");
                        if (decimal.TryParse(Console.ReadLine(), out decimal amount))
                        {
                            var txHash = await wallet.Service.SendAsync(toAddress, amount);
                            Console.WriteLine($"Transaction sent. Hash: {txHash}");
                        }
                        else
                        {
                            Console.WriteLine("Invalid amount.");
                        }
                        break;

                    case "3":
                        var history = await wallet.Service.GetTransactionHistoryAsync(wallet.Address);
                        Console.WriteLine("\nTransaction History:");
                        if (history.Count == 0)
                        {
                            Console.WriteLine("No transactions found.");
                        }
                        else
                        {
                            foreach (var tx in history)
                            {
                                Console.WriteLine($"Hash: {tx.Hash}");
                                Console.WriteLine($"  From: {tx.From}");
                                Console.WriteLine($"  To: {tx.To}");
                                Console.WriteLine($"  Value: {tx.Value} {wallet.Service.CurrencyName}");
                                Console.WriteLine("---");
                            }
                        }
                        break;

                    case "4":
                        running = false;
                        break;

                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
        }

        private static void SaveWallets(List<Wallet> wallets, string seedPhrase)
        {
            var walletData = new List<EncryptedWalletData>();
            foreach (var wallet in wallets)
            {
                walletData.Add(new EncryptedWalletData
                {
                    Address = wallet.Address,
                    EncryptedPrivateKey = Encryptor.Encrypt(wallet.PrivateKey, seedPhrase),
                    CurrencyName = wallet.Service.CurrencyName,
                    ContractAddress = wallet.ContractAddress
                });
            }

            string json = JsonSerializer.Serialize(walletData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(WalletFilePath, json);
            Console.WriteLine("Wallets saved successfully.");
        }

        private static List<Wallet> LoadWallets(string seedPhrase)
        {
            try
            {
                string json = File.ReadAllText(WalletFilePath);
                var walletData = JsonSerializer.Deserialize<List<EncryptedWalletData>>(json);
                var wallets = new List<Wallet>();

                foreach (var data in walletData)
                {
                    string privateKey = Encryptor.Decrypt(data.EncryptedPrivateKey, seedPhrase);
                    ICryptoService service;
                    if (data.CurrencyName == "Bitcoin")
                    {
                        service = new BitcoinService();
                    }
                    else if (data.ContractAddress != null)
                    {
                        service = new ERC20TokenService(privateKey, data.ContractAddress);
                    }
                    else
                    {
                        service = new EthereumService(privateKey);
                    }

                    wallets.Add(new Wallet
                    {
                        Address = data.Address,
                        PrivateKey = privateKey,
                        ContractAddress = data.ContractAddress,
                        Service = service
                    });
                }

                return wallets;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading wallets: {ex.Message}");
                return null;
            }
        }
    }

    public class EncryptedWalletData
    {
        public string Address { get; set; }
        public string EncryptedPrivateKey { get; set; }
        public string CurrencyName { get; set; }
        public string ContractAddress { get; set; }
    }
}