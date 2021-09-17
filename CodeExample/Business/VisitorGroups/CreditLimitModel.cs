using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EPiServer.Personalization.VisitorGroups;

namespace TRM.Web.Business.VisitorGroups
{
    public class CreditLimitModel : CriterionModelBase
    {
        #region Editable Properties

        [DojoWidget]
        public bool HasCreditLimit { get; set; }

        #endregion

        public override ICriterionModel Copy()
        {
            return base.ShallowCopy();
        }
    }
}
