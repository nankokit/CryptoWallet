# CryptoWallet

A secure, multi-currency cryptocurrency wallet built with C# and .NET, supporting Ethereum and ERC-20 tokens. Manage your wallets, check balances, send transactions, and view transaction history with a robust backend and secure key management.

## Features
- Multi-Currency Support: Manage Ethereum, Bitcoin, and ERC-20 token wallets.
- Secure Storage: Private keys are encrypted with AES and seed phrases are hashed with BCrypt.
- Database Persistence: SQLite database for storing user and wallet data.
- Blockchain Integration: Connects to Infura for Ethereum/ERC-20 and Etherscan for transaction history.

Extensible Design: Modular architecture with an ICryptoService interface for adding new cryptocurrencies.

## Prerequisites
- .NET 8 SDK
- SQLite (included via NuGet)
- Infura API key (for Ethereum/ERC-20)
- Etherscan API key (for transaction history)

## Setup
1. Clone the Repository:
```
git clone https://github.com/yourusername/CryptoWallet.git
cd CryptoWallet
```
2. Install Dependencies: Restore NuGet packages: `dotnet restore`
3. Configure API Keys: Create an `appsettings.json` file in the project root:
```
  {
  "InfuraApiKey": "https://sepolia.infura.io/v3/YOUR_INFURA_KEY",
  "EtherscanApiKey": "YOUR_ETHERSCAN_KEY" 
  }
```
4. Run the Application: `dotnet run`
