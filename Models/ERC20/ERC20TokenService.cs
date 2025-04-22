using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Threading.Tasks;
using CryptoWallet.Models.Common;
using System.Text.Json;
using System.IO;
using CryptoWallet.Models.Ethereum;

namespace CryptoWallet.Models.ERC20
{
    public class ERC20TokenService : ICryptoService
    {
        private readonly Web3 _web3;
        private readonly string _privateKey;
        private readonly string _rpcUrl;
        private readonly string _etherscanApiKey;
        private readonly HttpClient _httpClient;
        private readonly string _contractAddress;
        private readonly Contract _contract;
        private readonly int _decimals;

        public string CurrencyName { get; }

        private readonly string _erc20Abi = @"[
            {""constant"":true,""inputs"":[{""name"":""_owner"",""type"":""address""}],""name"":""balanceOf"",""outputs"":[{""name"":""balance"",""type"":""uint256""}],""type"":""function""},
            {""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_value"",""type"":""uint256""}],""name"":""transfer"",""outputs"":[{""name"":""success"",""type"":""bool""}],""type"":""function""},
            {""constant"":true,""inputs"":[],""name"":""decimals"",""outputs"":[{""name"":"""",""type"":""uint8""}],""type"":""function""},
            {""constant"":true,""inputs"":[],""name"":""symbol"",""outputs"":[{""name"":"""",""type"":""string""}],""type"":""function""}
        ]";

        public ERC20TokenService(string privateKey, string contractAddress)
        {
            try
            {
                _privateKey = privateKey;
                _contractAddress = contractAddress;
                Console.WriteLine($"Initializing ERC20TokenService with private key: {privateKey.Substring(0, 4)}... and contract: {_contractAddress}");

                // Чтение конфигурации
                if (!File.Exists("appsettings.json"))
                    throw new FileNotFoundException("appsettings.json not found");
                var config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText("appsettings.json"));
                _rpcUrl = config["InfuraApiKey"] ?? throw new ArgumentException("InfuraApiKey is missing in appsettings.json");
                _etherscanApiKey = config["EtherscanApiKey"] ?? throw new ArgumentException("EtherscanApiKey is missing in appsettings.json");

                // Инициализация Web3
                var httpClientHandler = new HttpClientHandler();
                var loggingHandler = new LoggingHttpMessageHandler(httpClientHandler);
                var httpClient = new HttpClient(loggingHandler);
                var client = new Nethereum.JsonRpc.Client.RpcClient(new Uri(_rpcUrl), httpClient);
                var account = new Account(_privateKey);
                _web3 = new Web3(account, client);

                // Инициализация контракта
                _contract = _web3.Eth.GetContract(_erc20Abi, _contractAddress);

                // Получение decimals и symbol
                var decimalsFunction = _contract.GetFunction("decimals");
                _decimals = decimalsFunction.CallAsync<int>().GetAwaiter().GetResult();
                var symbolFunction = _contract.GetFunction("symbol");
                CurrencyName = symbolFunction.CallAsync<string>().GetAwaiter().GetResult();

                // Инициализация Etherscan
                _httpClient = new HttpClient(new LoggingHttpMessageHandler(new HttpClientHandler()));
                _httpClient.BaseAddress = new Uri("https://api-sepolia.etherscan.io/");

                TestConnection().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing ERC20TokenService: {ex.Message}");
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

                Console.WriteLine($"Checking {CurrencyName} balance for address: {address}");
                var balanceOfFunction = _contract.GetFunction("balanceOf");
                var balance = await balanceOfFunction.CallAsync<BigInteger>(address);
                return (decimal)balance / (decimal)Math.Pow(10, _decimals);
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

                // Проверка баланса токенов
                var balance = await GetBalanceAsync(_web3.TransactionManager.Account.Address);
                if (balance < amount)
                    throw new InvalidOperationException("Insufficient token balance");

                // Проверка баланса ETH для газа
                var ethBalance = await _web3.Eth.GetBalance.SendRequestAsync(_web3.TransactionManager.Account.Address);
                var ethBalanceInEth = Web3.Convert.FromWei(ethBalance.Value);
                if (ethBalanceInEth < 0.001m) // Примерная плата за газ
                    throw new InvalidOperationException("Insufficient ETH for gas");

                Console.WriteLine($"Sending {amount} {CurrencyName} to {toAddress}");
                var transferFunction = _contract.GetFunction("transfer");
                var amountInWei = new BigInteger(amount * (decimal)Math.Pow(10, _decimals));
                var gas = await transferFunction.EstimateGasAsync(_web3.TransactionManager.Account.Address, toAddress, amountInWei);
                var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(_web3.TransactionManager.Account.Address, gas, null, null, toAddress, amountInWei);
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

                var url = $"api?module=account&action=tokentx&contractaddress={_contractAddress}&address={address}&startblock=0&endblock=99999999&sort=desc&apikey={_etherscanApiKey}";
                Console.WriteLine($"Fetching {CurrencyName} transaction history for {address}");
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
                        To = tx.To,
                        Value = (decimal)BigInteger.Parse(tx.Value) / (decimal)Math.Pow(10, _decimals)
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
}
