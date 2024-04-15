using OtpNet;
using System.Text;

namespace CryptoProject.Services
{
    public class OtpGenerator
    {
        /// <summary>
        /// Generates An OTP
        /// </summary>
        /// <param name="key"></param>
        /// <param name="expireTimeMinutes"></param>
        /// <param name="digitsCount"></param>
        /// <returns></returns>
        public string Generate(string key, int expireTimeMinutes = 5, int digitsCount = 4)
        {
            var ttl = TimeSpan.FromMinutes(expireTimeMinutes);
            var otp = new Totp(Encoding.UTF8.GetBytes(key), (int)ttl.TotalSeconds, totpSize: digitsCount);
            return otp.ComputeTotp();
        }

        /// <summary>
        /// Verifies An OTP
        /// </summary>
        /// <param name="key"></param>
        /// <param name="token"></param>
        /// <param name="expireTimeMinutes"></param>
        /// <param name="digitsCount"></param>
        /// <returns></returns>
        public bool Verify(string key, string token, int expireTimeMinutes = 5, int digitsCount = 4)
        {
            var ttl = TimeSpan.FromMinutes(expireTimeMinutes);
            var otp = new Totp(Encoding.UTF8.GetBytes(key), (int)ttl.TotalSeconds, totpSize: digitsCount);
            return otp.VerifyTotp(token, out _);
        }
    }

}
