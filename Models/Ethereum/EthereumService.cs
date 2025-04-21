using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Threading.Tasks;
using CryptoWallet.Models.Common;
using System.Text.Json;
using System.IO;

namespace CryptoWallet.Models.Ethereum
{
    public class EthereumService : ICryptoService
    {
        private readonly Web3 _web3;
        private readonly string _privateKey;
        private readonly string _rpcUrl;
        private readonly string _etherscanApiKey;
        private readonly HttpClient _httpClient;

        public string CurrencyName => "Ethereum";

        public EthereumService(string privateKey)
        {
            try
            {
                _privateKey = privateKey;
                Console.WriteLine($"Initializing with private key: {privateKey.Substring(0, 4)}...");


                if (!File.Exists("appsettings.json"))
                    throw new FileNotFoundException("appsettings.json not found");
                var config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText("appsettings.json"));
                _rpcUrl = config["InfuraApiKey"] ?? throw new ArgumentException("InfuraApiKey is missing in appsettings.json");
                _etherscanApiKey = config["EtherscanApiKey"] ?? throw new ArgumentException("EtherscanApiKey is missing in appsettings.json");


                var httpClientHandler = new HttpClientHandler();
                var loggingHandler = new LoggingHttpMessageHandler(httpClientHandler);
                var httpClient = new HttpClient(loggingHandler);
                var client = new Nethereum.JsonRpc.Client.RpcClient(new Uri(_rpcUrl), httpClient);

                var account = new Account(_privateKey);
                _web3 = new Web3(account, client); 


                _httpClient = new HttpClient(new LoggingHttpMessageHandler(new HttpClientHandler()));
                _httpClient.BaseAddress = new Uri("https://api-sepolia.etherscan.io/");

                TestConnection().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing EthereumService: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                throw;
            }
        }

        private async Task TestConnection()
        {
            try
            {
                Console.WriteLine($"Attempting to connect to: {_rpcUrl}");
                var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                Console.WriteLine($"Connected to Infura. Latest block: {blockNumber}");
            }
            catch (Nethereum.JsonRpc.Client.RpcClientUnknownException ex)
            {
                Console.WriteLine($"Infura RPC error: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Infura connection failed: {ex.Message}");
            }
        }

        public async Task<decimal> GetBalanceAsync(string address)
        {
            try
            {
                if (!address.StartsWith("0x") || address.Length != 42)
                    throw new ArgumentException("Invalid Ethereum address");

                Console.WriteLine($"Checking balance for address: {address}");
                var balance = await _web3.Eth.GetBalance.SendRequestAsync(address);
                return Web3.Convert.FromWei(balance.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching balance: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                return 0m;
            }
        }

        public async Task<string> SendAsync(string toAddress, decimal amount)
        {
            try
            {
                if (!toAddress.StartsWith("0x") || toAddress.Length != 42)
                    throw new ArgumentException("Invalid recipient address");

                // Проверяем баланс
                var balance = await _web3.Eth.GetBalance.SendRequestAsync(_web3.TransactionManager.Account.Address);
                var balanceInEth = Web3.Convert.FromWei(balance.Value);
                if (balanceInEth < amount + 0.001m) // Примерная плата за газ
                    throw new InvalidOperationException("Insufficient funds for amount and gas");

                Console.WriteLine($"Sending {amount} ETH to {toAddress}");
                var receipt = await _web3.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(toAddress, amount);
                Console.WriteLine($"Transaction sent. Hash: {receipt.TransactionHash}");
                return receipt.TransactionHash;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending transaction: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                return "Failed";
            }
        }

        public async Task<List<Transaction>> GetTransactionHistoryAsync(string address)
        {
            try
            {
                if (!address.StartsWith("0x") || address.Length != 42)
                    throw new ArgumentException("Invalid Ethereum address");

                var url = $"api?module=account&action=txlist&address={address}&startblock=0&endblock=99999999&sort=desc&apikey={_etherscanApiKey}";
                Console.WriteLine($"Fetching transaction history for {address}");
                var response = await _httpClient.GetFromJsonAsync<EtherscanResponse>(url);

                if (response == null || response.Status != "1")
                {
                    Console.WriteLine($"Failed to fetch transactions: {response?.Message ?? "Unknown error"}");
                    return new List<Transaction>();
                }

                var transactions = new List<Transaction>();
                foreach (var tx in response.Result)
                {
                    transactions.Add(new Transaction
                    {
                        Hash = tx.Hash,
                        From = tx.From,
                        To = tx.To ?? "Contract Creation",
                        Value = Web3.Convert.FromWei(BigInteger.Parse(tx.Value))
                    });
                }
                Console.WriteLine($"Fetched {transactions.Count} transactions");
                return transactions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching transaction history: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                return new List<Transaction>();
            }
        }
    }

    internal class EtherscanResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public List<EtherscanTransaction> Result { get; set; }
    }

    internal class EtherscanTransaction
    {
        public string Hash { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Value { get; set; }
    }
}