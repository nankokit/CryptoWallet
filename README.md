# CryptoWallet

A secure, multi-currency cryptocurrency wallet built with C# and .NET, supporting Ethereum and ERC-20 tokens. Manage your wallets, check balances, send transactions, and view transaction history with a robust backend and secure key management.

## Features
- Multi-Currency Support: Manage Ethereum and ERC-20 token wallets.
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

 ## Usage
1. **Register**: Create a new user with a unique ID and receive a secure seed phrase.
2. **Login**: Use your user ID and seed phrase to access your wallets.
3. **Add Wallet**: Create Ethereum, Bitcoin, or ERC-20 token wallets.
4. **Manage Wallets**: Check balances, send transactions, or view transaction history.
5. **Recover Wallet**: Restore a wallet using a seed phrase.

## Security Notes
- Store your seed phrase securely and never share it.
- The console displays sensitive data (e.g., seed phrases) during registration. Future versions will improve this.
- Use strong passwords for encryption.

## Project Structure
- **CryptoWallet/Models/Common**: Core models (`Wallet`, `Transaction`, `Encryptor`) and interfaces (`ICryptoService`).
- **CryptoWallet/Models/Ethereum**: Ethereum-specific services and logic.
- **CryptoWallet/Models/ERC20**: ERC-20 token services.
- **CryptoWallet/DatabaseService.cs**: SQLite database management.
- **CryptoWallet/ConsoleApp.cs**: Console-based UI and application logic.
