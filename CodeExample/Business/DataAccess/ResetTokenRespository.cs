using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Data.Dynamic;
using TRM.Web.Models.DDS;

namespace TRM.Web.Business.DataAccess
{
    public class ResetTokenRespository : IResetTokenRepository
    {
        
        protected void CopyDataIntoDto(ResetToken objectToSave, ResetToken dto)
        {
            dto.CreatedDate = objectToSave.CreatedDate;
            dto.Token = objectToSave.Token;
            dto.UserId = objectToSave.UserId;
        }

        public ResetTokenRespository()
            : this(
                DynamicDataStoreFactory.Instance.GetStore(typeof(ResetToken)) ??
                DynamicDataStoreFactory.Instance.CreateStore(typeof(ResetToken)))
        {
        }

        protected ResetTokenRespository(DynamicDataStore store)
        {
            Store = store;
        }

        protected DynamicDataStore Store { get; private set; }

        public virtual void Save(ResetToken objectToSave)
        {
            ResetToken record = null;

            if (objectToSave.Id != Guid.Empty)
            {
                record = (from r in Find() where r.Id == objectToSave.Id select r).FirstOrDefault();
            }
          
            if (record != null)
            {
                if (record.Id != objectToSave.Id)
                {
                    throw new InvalidOperationException(string.Format(
                        "The destination Id [{0}] and the source Id [{1}] must be the same.",
                        record.Id,
                        objectToSave.Id));
                }
                CopyDataIntoDto(objectToSave, record);
                if (record.Id != objectToSave.Id)
                {
                    throw new InvalidOperationException(string.Format(
                        "CopyDataIntoDto should not change the object's Identity; destination Id [{0}], source Id [{1}].",
                        record.Id,
                        objectToSave.Id));
                }
            }
            else
            {
                if (objectToSave.Id == Guid.Empty)
                {
                    objectToSave.Id = Guid.NewGuid();
                }
                record = objectToSave;
            }
            Store.Save(record);
        }

        public IOrderedQueryable<ResetToken> Find()
        {
            return Store.Items<ResetToken>();
        }


        //public virtual IEnumerable<ResetToken> FindAll()
        //{
        //    return Store.LoadAll<ResetToken>().ToList();
        //}

        //public virtual IEnumerable<ResetToken> FindIdsByDate(DateTime date)
        //{
        //    var responses = from r in Store.Items<ResetToken>()
        //        where r.CreatedDate < date
        //        select r;
         
        //    return responses;
        //}

        
        //public int DeleteByDate(DateTime date)
        //{
        //    var deleted = 0;

        //    var tokens = FindIdsByDate(date);
          
        //    foreach (var token in tokens)
        //    {
        //        Store.Delete(token);
        //        deleted += 1;
        //    }
        //    return deleted;
        //}


        
        public virtual void Delete(ResetToken objectToDelete)
        {
            if (objectToDelete.Id == Guid.Empty)
            {
                return;
            }
            Store.Delete(objectToDelete);
        }


        public virtual void DeleteAll()
        {
            Store.DeleteAll();
        }

        public virtual void Dispose()
        {
            if (Store == null)
            {
                return;
            }
            Store.Dispose();
            Store = null;
        }
        
        //public ResetToken GetPasswordResetTokenOrNull(string token)
        //{
        //    var firstResult = 
                
                
        //        Find(a => a.Token == token)
        //        .OrderByDescending(b => b.CreatedDate).FirstOrDefault();

        //    return firstResult;
        //}
    }
}