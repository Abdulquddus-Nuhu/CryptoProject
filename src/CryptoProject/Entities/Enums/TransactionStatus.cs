namespace CryptoProject.Entities.Enums
{
    public enum TransactionStatus
    {
        Successful, // Transaction was completed successfully
        Reversed, // Transaction was successfully completed but then undone
        Failed, // Transaction could not be completed
        Pending, // Transaction has been initiated but is not yet completed
    }

}
