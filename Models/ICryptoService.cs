using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoWallet.Models.Common;

namespace CryptoWallet.Models
{
    public interface ICryptoService
    {
        string CurrencyName { get; }
        Task<decimal> GetBalanceAsync(string address);
        Task<string> SendAsync(string toAddress, decimal amount);
        Task<List<Transaction>> GetTransactionHistoryAsync(string address);
    }
}