using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoWallet.Models.Common;
using System.Net.Http;
using System.Net.Http.Json;
using System;
using System.Numerics;

namespace CryptoWallet.Models.Ethereum
{
    public class EthereumService : ICryptoService
    {
        private readonly Web3 _web3;
        private readonly string _privateKey;
        private readonly string _rpcUrl = "https://sepolia.infura.io/v3/80529fb326034dd480fb7fd24115e822"; 
        private readonly string _etherscanApiKey = "NGZYT44U5QVWEHUX34JJQAFSTMUB75XARW"; 
        private readonly HttpClient _httpClient;

        public string CurrencyName => "Ethereum";

        public EthereumService(string privateKey)
        {
            _privateKey = privateKey;
            var account = new Account(_privateKey);
            _web3 = new Web3(account, _rpcUrl);
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api-sepolia.etherscan.io/");
        }

        public async Task<decimal> GetBalanceAsync(string address)
        {
            var balance = await _web3.Eth.GetBalance.SendRequestAsync(address);
            return Web3.Convert.FromWei(balance.Value);
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