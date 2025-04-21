namespace CryptoWallet.Models.Common
{
    public class Wallet
    {
        public string Address { get; set; }
        public string PrivateKey { get; set; }
        public ICryptoService Service { get; set; }
    }
}