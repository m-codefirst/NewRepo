using System.Collections.Generic;

namespace TRM.Web.Services.AutoInvest
{
    public interface IAutoInvestmentSerializationHelper
    {
        string SerializeInvestments(Dictionary<string, decimal> investments);
        decimal GetTotalAmount(Dictionary<string, decimal> investments);
        Dictionary<string, decimal> DeserializeInvestments(string investments, decimal totalAmount);
    }
}