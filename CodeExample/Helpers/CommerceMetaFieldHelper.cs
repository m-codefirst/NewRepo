using System;
using System.Linq;
using log4net;
using Mediachase.Commerce.Orders;
using Mediachase.Commerce.Storage;
using TRM.Web.Constants;

namespace TRM.Web.Helpers
{
    public class CommerceMetaFieldHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CommerceMetaFieldHelper));

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="metaStorageBase"></param>
        /// <param name="fieldName"></param>
        /// <param name="defaultValue">if no metafield found, return this value, and avoid null exceptions.</param>
        /// <param name="continueOnError">if true, then ignore error and do not raise exception.</param>
        /// <returns></returns>
        public static T GetMetaField<T>(MetaStorageBase metaStorageBase, string fieldName, T defaultValue, bool continueOnError = true) where T : IComparable
        {
            try
            {
                if (metaStorageBase.GetElementaryFieldValues().ContainsKey(fieldName))
                {
                    var metaField = metaStorageBase[fieldName];

                    if (metaField == null)
                    {
                        return defaultValue;
                    }

                    if (metaField.GetType() == typeof(T))
                    {
                        return (T)metaField;
                    }
                }
            }
            catch (ArgumentNullException ex)
            {
                if (continueOnError)
                {
                    //log warning & continue
                    LogError(ex, fieldName);
                }
                else
                {
                    throw;
                }
            }

            return default(T);
        }

        public static bool IsMetaFieldPopulated(MetaStorageBase metaStorageBase, string fieldName)
        {
            return metaStorageBase.GetElementaryFieldValues().ContainsKey(fieldName) && metaStorageBase[fieldName] != null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="metaStorageBase"></param>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <param name="continueOnError">we only allow to continue with the function if the bool is true. otherwise we throw the error back up the chain.</param>
        public static void SetMetaField(MetaStorageBase metaStorageBase, string fieldName, object value, bool continueOnError = true)
        {
            try
            {
                if (metaStorageBase.GetElementaryFieldValues().ContainsKey(fieldName))
                {
                    metaStorageBase[fieldName] = value;
                }
            }
            catch (ArgumentNullException ex)
            {
                if (continueOnError)
                {
                    //log warning & continue
                    LogError(ex, fieldName);
                }
                else
                {
                    throw;
                }
            }
        }

        private static void LogError(ArgumentNullException exception, string field)
        {
            // ReSharper disable once UseStringInterpolation
            Logger.Warn(string.Format("Cound not find the metafield: {0}", field), exception);
        }

        public static void SetChildren(LineItem parentItem, string contentId, int qty)
        {
            var childrenAsString = GetMetaField(parentItem, MetaFields.Children, string.Empty);

            var children = childrenAsString.Split('|').ToList();

            if (qty == 0 && children.Contains(contentId))
            {
                children.Remove(contentId);
            }
            else
            {
                children.Add(contentId);
            }

            var newChildren = string.Join("|", children);

            SetMetaField(parentItem, MetaFields.Children, newChildren);
        }
    }
}