using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoWallet.Models.Common;
using System.Net.Http;
using System.Net.Http.Json;
using System;
using System.IO;
using System.Numerics;
using System.Text.Json;

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
                var config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText("appsettings.json"));
                _rpcUrl = config["InfuraApiKey"];
                _etherscanApiKey = config["EtherscanApiKey"];
                Console.WriteLine($"Initializing with private key: {privateKey.Substring(0, 4)}..."); 
                var account = new Account(_privateKey);
                _web3 = new Web3(account, _rpcUrl);
                _httpClient = new HttpClient();
                _httpClient.BaseAddress = new Uri("https://api-sepolia.etherscan.io/");
                TestConnection().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing EthereumService: {ex.Message}");
                throw;
            }
        }
        private async Task TestConnection()
        {
            try
            {
                var blockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                Console.WriteLine($"Connected to Infura. Latest block: {blockNumber}");
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
                Console.WriteLine($"Checking balance for address: {address}");
                var balance = await _web3.Eth.GetBalance.SendRequestAsync(address);
                return Web3.Convert.FromWei(balance.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching balance: {ex.Message}");
                return 0m;
            }
        }

        public async Task<string> SendAsync(string toAddress, decimal amount)
        {
            var receipt = await _web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toAddress, amount);
            return receipt.TransactionHash;
        }

        public async Task<List<Transaction>> GetTransactionHistoryAsync(string address)
        {
            var url = $"api?module=account&action=txlist&address={address}&startblock=0&endblock=99999999&sort=desc&apikey={_etherscanApiKey}";
            var response = await _httpClient.GetFromJsonAsync<EtherscanResponse>(url);

            if (response == null || response.Status != "1")
            {
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
                    Value = Web3.Convert.FromWei(BigInteger.Parse(tx.Value)) // from Wei to ETH
                });
            }

            return transactions;
        }
    }

    // deserialize from Etherscan
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