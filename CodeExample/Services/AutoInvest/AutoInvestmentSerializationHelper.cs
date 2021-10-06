using System.Collections.Generic;
using System.Linq;
using EPiServer.Find.Helpers.Text;

namespace TRM.Web.Services.AutoInvest
{
    public class AutoInvestmentSerializationHelper : IAutoInvestmentSerializationHelper
    {
        private const string ProductPriceSeparator = "-";
        private const string ProductsSeparator = "|";

        public string SerializeInvestments(Dictionary<string, decimal> investments)
        {
            var pairs = investments.Select(x => $"{x.Key}{ProductPriceSeparator}{x.Value}");
            var result = string.Join(ProductsSeparator, pairs);
            
            return result;
        }

        public decimal GetTotalAmount(Dictionary<string, decimal> investments)
        {
            return investments?.Select(x => x.Value).Sum() ?? (decimal)0;
        }

        public Dictionary<string, decimal> DeserializeInvestments(string investments, decimal totalAmount)
        {
            var result = new Dictionary<string, decimal>();

            if (investments.IsNullOrWhiteSpace())
            {
                return result;
            }

            var pairs = investments.Split(ProductsSeparator.ToCharArray()).ToList();

            //Backward compatibility
            if (pairs.Count == 1 && pairs[0].Contains(ProductPriceSeparator) == false)
            {
                result.Add(pairs[0], totalAmount);
                return result;
            }

            foreach (string pair in pairs)
            {
                var splitted = pair.Split(ProductPriceSeparator.ToCharArray()).ToList();
                var code = splitted[0];

                if (code.IsNullOrEmpty())
                {
                    continue;
                }
                
                if (decimal.TryParse(splitted.Count > 1 ? splitted[1] : "0", out decimal amount) && amount > 0)
                {
                    result.Add(code, amount);
                }
            }

            return result;
        }
    }
}