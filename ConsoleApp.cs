using CryptoWallet.Models;
using CryptoWallet.Models.Common;
using CryptoWallet.Models.Ethereum;
using CryptoWallet.Models.Bitcoin;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoWallet
{
    public class ConsoleApp
    {
        public static async Task Main(string[] args)
        {
            var wallets = new List<Wallet>();

            bool addingWallets = true;
            while (addingWallets)
            {
                Console.WriteLine("\nAdd a new wallet:");
                Console.WriteLine("1. Add Ethereum Wallet");
                Console.WriteLine("2. Add Bitcoin Wallet");
                Console.WriteLine("3. Finish adding wallets");
                Console.Write("Select an option: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        wallets.Add(CreateEthereumWallet());
                        break;
                    case "2":
                        wallets.Add(CreateBitcoinWallet());
                        break;
                    case "3":
                        addingWallets = false;
                        break;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }

            if (wallets.Count == 0)
            {
                Console.WriteLine("No wallets added. Exiting...");
                return;
            }

            bool running = true;
            while (running)
            {
                Console.WriteLine("\nMulti-Currency Crypto Wallet");
                Console.WriteLine("1. Select Wallet");
                Console.WriteLine("2. Exit");
                Console.Write("Select an option: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await SelectWallet(wallets);
                        break;
                    case "2":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
        }

        private static Wallet CreateEthereumWallet()
        {
            Console.Write("Enter Ethereum address (e.g., 0x51B5bb...): ");
            var address = Console.ReadLine();
            Console.Write("Enter Ethereum private key (e.g., f36609...): ");
            var privateKey = Console.ReadLine();

            return new Wallet
            {
                Address = address,
                PrivateKey = privateKey,
                Service = new EthereumService(privateKey)
            };
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
                        Console.WriteLine("Transaction History:");
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
                                Console.WriteLine($"  Value: {tx.Value} ETH");
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
    }
}