using System.Collections.Generic;
using TRM.Shared.Constants;

namespace TRM.Shared.Models.DTOs
{
    public class IdentityResult
    {
        public IdentityResult()
        {
            Messages = new List<string>();
        }

        public Enums.eCustomerStatus Status { get; set; }
        public List<string> Messages { get; set; }
    }
}