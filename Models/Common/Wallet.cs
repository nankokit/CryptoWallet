using NBitcoin;
using CryptoWallet.Models;
using CryptoWallet.Models.Ethereum;

namespace CryptoWallet.Models.Common
{
    public class Wallet
    {
        public string Address { get; set; }
        public string PrivateKey { get; set; }
        public string ContractAddress { get; set; }
        public ICryptoService Service { get; set; }

        public Wallet() { }

        public Wallet(Key privateKey)
        {
            PrivateKey = privateKey.ToHex();
            var pubKey = privateKey.PubKey;
            Address = pubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main).ToString();
            Service = new EthereumService(PrivateKey);
        }
    }
}