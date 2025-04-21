using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoWallet.Models.Common; 

namespace CryptoWallet.Models.Bitcoin
{
    public class BitcoinService : ICryptoService
    {
        public string CurrencyName => "Bitcoin";

        public Task<decimal> GetBalanceAsync(string address)
        {
            return Task.FromResult(0m); 
        }

        public Task<string> SendAsync(string toAddress, decimal amount)
        {
            return Task.FromResult("Not implemented");
        }

        public Task<List<Transaction>> GetTransactionHistoryAsync(string address)
        {
            return Task.FromResult(new List<Transaction>());
        }
    }
}