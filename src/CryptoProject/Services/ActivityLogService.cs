using CryptoProject.Entities.Enums;
using CryptoProject.Entities;

namespace CryptoProject.Services
{
    public static class ActivityLogService
    {
        public static ActivityLog CreateLogEntry(Guid? userId, string userEmail, ActivityType activityType, params object[] additionalInfo)
        {
            var logEntry = new ActivityLog
            {
                UserId = userId,
                UserEmail = userEmail,
                ActivityType = activityType,
                Timestamp = DateTime.UtcNow,
                Details = GetDetailsMessage(activityType, additionalInfo)
            };

            return logEntry;
        }

        private static string GetDetailsMessage(ActivityType activityType, object[] additionalInfo)
        {
            switch (activityType)
            {
                case ActivityType.UserRegistered:
                    return $"User with ID {additionalInfo[0]} registered.";
                case ActivityType.UserLoggedIn:
                    return "User logged in.";
                case ActivityType.UserSetPin:
                    return "User set or updated their PIN.";
                case ActivityType.UserUpdatedProfile:
                    return "User updated their profile information.";
                case ActivityType.WalletCreated:
                    return $"Wallet created for user ID {additionalInfo[0]}.";
                case ActivityType.WalletFundsAdded:
                    return $"Funds added to wallet. Amount: {additionalInfo[0]}.";
                case ActivityType.WalletFundsDeducted:
                    return $"Funds deducted from wallet. Amount: {additionalInfo[0]}.";
                case ActivityType.WalletTransferInitiated:
                    return $"Transfer initiated to user ID {additionalInfo[0]}. Amount: {additionalInfo[1]}.";
                case ActivityType.WalletTransferCompleted:
                    return $"Transfer completed to user ID {additionalInfo[0]}. Amount: {additionalInfo[1]}.";
                case ActivityType.WalletTransferReverted:
                    return $"Transfer reverted for transaction ID {additionalInfo[0]}.";
                case ActivityType.AdminFundsAdjusted:
                    return $"Admin adjusted funds. New balance: {additionalInfo[0]}.";
                case ActivityType.AdminViewedUser:
                    return $"Admin viewed details for user ID {additionalInfo[0]}.";
                case ActivityType.AdminViewedTransaction:
                    return $"Admin viewed transaction ID {additionalInfo[0]}.";
                default:
                    return "Activity occurred.";
            }
        }
    }

}
