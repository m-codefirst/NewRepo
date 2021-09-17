using System;
using System.Collections.Generic;
using System.Linq;
using Hephaestus.CMS.DataAccess;
using TRM.Web.Extentions;
using TRM.Web.Models.DDS;
using EPiServer.Framework.Localization;
using TRM.Web.Constants;
using EPiServer.Globalization;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using static TRM.Shared.Constants.StringConstants;

namespace TRM.Web.Helpers
{
	public class SecurityQuestionHelper : IAmSecurityQuestionHelper
	{
		private readonly LocalizationService _localizationService;
        private readonly IMetaFieldTypeHelper _metaFieldHelper;
        public SecurityQuestionHelper(IMetaFieldTypeHelper metaFieldHelper,
            LocalizationService localizationService)
		{
            _metaFieldHelper = metaFieldHelper;
			_localizationService = localizationService;
		}

		public Dictionary<string, string> GetSecurityQuestionList(string message = "")
		{
			var questionsDic = new Dictionary<string, string> { { "Select a question", !string.IsNullOrEmpty(message) ?
                    message 
                    : _localizationService.GetStringByCulture(StringResources.ChangeSecurityQuestionAndAnswer, "Select a question", ContentLanguage.PreferredCulture) } };

            var questionList = _metaFieldHelper.GetMetaEnumItems(CustomMetaFieldTypeNames.SecurityQuestion);
			foreach (var question in questionList)
			{
				questionsDic.TryAdd(question.Handle.ToString(), question.Name);
			}

			return questionsDic;
		}

        public int GetTwoPartQuestionId(string question)
        {
            var check = _metaFieldHelper.GetMetaEnumItems(CustomMetaFieldTypeNames.SecurityQuestion)
                .FirstOrDefault(x => x.Name.Trim().Equals(question.Trim(), StringComparison.OrdinalIgnoreCase));
            return check != null ? check.Handle : 0;
        }


        public string GetQuestionById(string id)
		{
            return _metaFieldHelper.GetMetaEnumItems(CustomMetaFieldTypeNames.SecurityQuestion)
                .FirstOrDefault(x => x.Handle.ToString() == id)
                ?.Name;
		}
	}
}