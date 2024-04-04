namespace CryptoProject.Entities.Enums
{
    public enum ActivityType
    {
        UserRegistered, // When a new user signs up
        UserLoggedIn, // When a user logs in
        UserSetPin, // When a user sets or updates their PIN
        UserUpdatedProfile, // When a user updates their profile information
        WalletCreated, // When a new wallet is created for a user
        WalletFundsAdded, // When funds are added to a user's wallet
        WalletFundsDeducted, // When funds are deducted from a user's wallet
        WalletTransferInitiated, // When a user initiates a transfer to another user
        WalletTransferCompleted, // When a wallet transfer is successfully completed
        WalletTransferReverted, // When an admin reverts a wallet transfer
        AdminFundsAdjusted, // When an admin adjusts the funds in a user's wallet
        AdminViewedUser, // When an admin views user details
        AdminViewedTransaction, // When an admin views transaction details
        USDFundsDeducted, // When funds are deducted from a user's USD account
        USDFundsAdded, // When funds are added to a user's USD account
    }

}
