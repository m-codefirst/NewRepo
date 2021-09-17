using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using EPiServer.Data.Dynamic;
using EPiServer.DataAbstraction;
using EPiServer.Personalization.VisitorGroups;
using EPiServer.ServiceLocation;

namespace TRM.Web.Business.VisitorGroups
{
    [EPiServerDataStore(AutomaticallyRemapStore = true)]
    public class ViewedCmsCategoriesModel : CriterionModelBase
    {
        private int _selectedCategory;

        internal Injected<CategoryRepository> CategoryRepository { get; set; }

        [Required]
        [DojoWidget(AdditionalOptions = "{ selectOnClick: true }", SelectionFactoryType = typeof(CategorySelectionFactory))]
        public int SelectedCategory
        {
            get
            {
                if (_selectedCategory == 0)
                    _selectedCategory = GetCategoryByGuid();
                return _selectedCategory;
            }
            set
            {
                _selectedCategory = value;
                CategoryGuid = GetCategoryGuid(value);
            }
        }

        [Required]
        [Range(0, 2147483647)]
        [DojoWidget(AdditionalOptions = "{constraints: {min: 0}, selectOnClick: true}", DefaultValue = 0)]
        public int NumberOfPageViews { get; set; }

        public Guid CategoryGuid { get; set; }

        public virtual Category GetRootCategory()
        {
            return CategoryRepository.Service.GetRoot();
        }

        internal virtual Guid GetCategoryGuid(int id)
        {
            if (id <= 0)
                return Guid.Empty;
            Category category = CategoryRepository.Service.Get(_selectedCategory);
            return category == null ? Guid.Empty : category.GUID;
        }

        private int GetCategoryByGuid()
        {
            if (CategoryGuid != Guid.Empty)
            {
                foreach (Category category in GetRootCategory().GetList())
                {
                    if (category.GUID == CategoryGuid)
                        return category.ID;
                }
            }
            return 0;
        }

        public override ICriterionModel Copy()
        {
            return ShallowCopy();
        }

        private class CategorySelectionFactory : ISelectionFactory
        {
            private readonly CategoryRepository _categoryRepository;

            public CategorySelectionFactory()
              : this(ServiceLocator.Current.GetInstance<CategoryRepository>())
            {
            }

            public CategorySelectionFactory(CategoryRepository categoryRepository)
            {
                _categoryRepository = categoryRepository;
            }

            public IEnumerable<SelectListItem> GetSelectListItems(Type property)
            {
                yield return new SelectListItem
                {
                    Text = string.Empty,
                    Value = string.Empty
                };
                foreach (Category category in GetCategoriesRecursive(_categoryRepository.GetRoot()))
                {
                    string str = "";
                    for (int index = 1; index < category.Indent; ++index)
                        str += "&nbsp;&nbsp;";
                    yield return new SelectListItem
                    {
                        Text = str + category.LocalizedDescription,
                        Value = category.ID.ToString()
                    };
                }
            }

            private IEnumerable<Category> GetCategoriesRecursive(Category category)
            {
                foreach (Category category1 in category.Categories)
                {
                    Category c = category1;
                    if (c.Available)
                    {
                        yield return c;
                        foreach (Category category2 in GetCategoriesRecursive(c))
                            yield return category2;
                        c = null;
                    }
                }
            }
        }
    }
}
