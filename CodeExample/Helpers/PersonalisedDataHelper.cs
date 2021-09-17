using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Data.Dynamic;
using TRM.Web.Models.DDS;

namespace TRM.Web.Helpers
{
    public class PersonalisedDataHelper : IAmPersonalisedDataHelper
    {
        protected readonly DynamicDataStore Store;
        public PersonalisedDataHelper()
        {
            Store = typeof(PersonalisedData).GetStore();
        }

        public List<PersonalisedData> GetPersonalisedDatasByCreated(int days)
        {
            var expiryDate = DateTime.Now.AddDays(days);
            var personaliedDatas = Store.Items<PersonalisedData>().Where(x => x.Created <= expiryDate).ToList();
            return personaliedDatas;
        }

        public bool Delete(PersonalisedData personalisedData)
        {
            try
            {
                Store.Delete(personalisedData);
                return true;
            }
            catch (Exception)
            {
                UpdateStatus(personalisedData, false);
                return true;
            }
        }

        public void UpdateStatus(PersonalisedData personalisedData, bool statusUpdate)
        {
            personalisedData.Status = statusUpdate;
            Store.Save(personalisedData);
        }

        public void Create(string imageId)
        {
            var personalisedData = new PersonalisedData
            {
                Created = DateTime.Today,
                Status = false,
                ImageGuid = imageId
            };

            Store.Save(personalisedData);
        }
    }
}