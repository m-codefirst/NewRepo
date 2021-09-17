using System;
using System.Collections.Generic;
using System.Linq;
using Hephaestus.CMS.DataAccess;
using TRM.Web.Models.DDS;

namespace TRM.Web.Business.DataAccess
{
    public interface IResetTokenRepository
    {
        void Save(ResetToken objectToSave);

        IOrderedQueryable<ResetToken> Find();
        void Delete(ResetToken objectToDelete);
        
        void DeleteAll();
       }
}
