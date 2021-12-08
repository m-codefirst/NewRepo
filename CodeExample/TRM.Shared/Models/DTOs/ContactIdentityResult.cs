using Mediachase.Commerce.Customers;

namespace TRM.Shared.Models.DTOs
{
    /// <summary>
    /// An IdentityResult in which also a CustomerContact is associated.
    /// </summary>
    public class ContactIdentityResult
    {
        /// <summary>
        /// Returns a new instance of a ContactIdentityResult.
        /// </summary>
        /// <param name="result">Status on the attempt to create the identity.</param>
        /// <param name="contact">A CustomerContact entity related to the IdentityResult.</param>
        public ContactIdentityResult(IdentityResult result, CustomerContact contact)
        {
            Contact = contact;
            Result = result;
        }

        /// <summary>
        /// Gets the CustomerContact involved in the Identity action.
        /// </summary>
        public CustomerContact Contact { get; private set; }

        /// <summary>
        /// Gets the outcome of the related identity action.
        /// </summary>
        public IdentityResult Result { get; private set; }
    }
}