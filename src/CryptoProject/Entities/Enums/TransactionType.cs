namespace CryptoProject.Entities.Enums
{
    public enum TransactionType
    {
        Addition, // Admin adding funds to a user's wallet
        Deduction, // Admin deducting funds from a user's wallet
        Transfer, // User transferring funds to another user's wallet,
        WalletTranfer,
        WireTransfer,
        BitcoinTransfer,
    }
}
