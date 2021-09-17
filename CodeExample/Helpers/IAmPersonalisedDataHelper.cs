using System;
using System.Collections.Generic;
using TRM.Web.Models.DDS;

namespace TRM.Web.Helpers
{
    public interface IAmPersonalisedDataHelper
    {
        List<PersonalisedData> GetPersonalisedDatasByCreated(int days);
        bool Delete(PersonalisedData personalisedData);
        void UpdateStatus(PersonalisedData personalisedData, bool statusUpdate);
        void Create(string imageId);
    }
}