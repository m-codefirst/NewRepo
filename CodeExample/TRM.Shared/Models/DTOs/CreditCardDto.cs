using System;
using System.Globalization;

namespace TRM.Shared.Models.DTOs
{
    public class CreditCardDto
    {
        public string CardType { get; set; }

        public string Token { get; set; }

        public string CardExpiry { get; set; }

        public bool UseAmex { get; set; }

        public string LastFour { get; set; }

        public bool IsExpired
        {
            get
            {
                DateTime expiredDate;
                if (DateTime.TryParseExact(this.CardExpiry, "MMyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out expiredDate))
                {
                    var currentDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    var result = DateTime.Compare(currentDate, expiredDate);
                    return result > 0;
                }

                return false;
            }
        }

        public string NameOnCard { get; set; }
    }
}
