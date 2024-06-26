﻿using CryptoProject.Entities.Enums;

namespace CryptoProject.Models.Requests
{
    public record DebitRequest
    {
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public WalletType WalletType { get; set; }
    }
}
