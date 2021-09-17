using System;

namespace Hephaestus.Commerce.CustomerServices
{
    public interface IAmContactSearchService
    {
        IAmCustomerContact FindContact(Guid contactId);
    }
}