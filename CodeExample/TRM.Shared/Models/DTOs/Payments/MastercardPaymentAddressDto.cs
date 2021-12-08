using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TRM.Shared.Models.DTOs.Payments
{
    public class MastercardPaymentAddressDto
    {
        public string city { get; set; }
        public string postcodeZip { get; set; }
        public string street { get; set; }
        public string street2 { get; set; }
        public string country { get; set; }
    }
}
