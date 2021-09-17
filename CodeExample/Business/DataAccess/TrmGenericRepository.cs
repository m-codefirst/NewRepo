using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Data.Dynamic;
using Hephaestus.CMS.DataAccess;
using Hephaestus.CMS.Models;

namespace TRM.Web.Business.DataAccess
{
    public abstract class TrmGenericRepository<T> : IRepository<T> where T : DdsPocoBase
    {
        protected TrmGenericRepository()
            : this(
                DynamicDataStoreFactory.Instance.GetStore(typeof(T)) ??
                DynamicDataStoreFactory.Instance.CreateStore(typeof(T)))
        {
        }

        protected TrmGenericRepository(DynamicDataStore store)
        {
            Store = store;
        }

        protected DynamicDataStore Store { get; private set; }

        public virtual void Save(T objectToSave)
        {
            var record = Find(item => item.Id == objectToSave.Id).FirstOrDefault();
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

        public T Find(Guid id)
        {
            return Store.Items<T>().FirstOrDefault(o => o.Id == id);
        }

        public virtual IEnumerable<T> FindAll()
        {
            return Store.LoadAll<T>();
        }

        public virtual IEnumerable<T> Find(Func<T, bool> @where)
        {
            return Store.Items<T>().Where(@where);
        }

        public virtual void Delete(T objectToDelete)
        {
            if (objectToDelete.Id == Guid.Empty)
            {
                return;
            }
            Store.Delete(objectToDelete);
        }

        public virtual void Delete(Func<T, bool> @where)
        {
            foreach (var item in Find(@where))
            {
                Store.Delete(item);
            }
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

        protected abstract void CopyDataIntoDto(T objectToSave, T dto);
    }
}
