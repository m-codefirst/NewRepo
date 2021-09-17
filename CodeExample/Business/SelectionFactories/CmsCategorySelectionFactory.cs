using System.Collections.Generic;
using System.Linq;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;

namespace TRM.Web.Business.SelectionFactories
{
    public class CmsCategorySelectionFactory : ISelectionFactory
    {
        private Injected<CategoryRepository> CategoryRepository { get; set; }

        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            var allCategories = this.CategoryRepository.Service.GetRoot().GetList().Cast<Category>().ToList();

            return allCategories.Select(category => new SelectItem()
            {
                Text = category.Name,
                Value = category.ID
            });
        }
    }
}