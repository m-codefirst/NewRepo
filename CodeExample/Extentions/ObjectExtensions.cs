using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace TRM.Web.Extentions
{
    public static class ObjectExtensions
    {
        #region Private Fields
        private static readonly Type[] WriteTypes = new[] {
        typeof(string), typeof(DateTime), typeof(Enum),
        typeof(decimal), typeof(Guid),
    };
        #endregion Private Fields
        #region .ToXml
        /// <summary>
        /// Converts an anonymous type to an XElement.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>Returns the object as it's XML representation in an XElement.</returns>
        public static XElement ToXml(this object input)
        {
            return input.ToXml(null);
        }

        /// <summary>
        /// Converts an anonymous type to an XElement.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="element">The element name.</param>
        /// <returns>Returns the object as it's XML representation in an XElement.</returns>
        public static XElement ToXml(this object input, string element)
        {
            return _ToXml(input, element);
        }

        private static XElement _ToXml(object input, string element, int? arrayIndex = null, string arrayName = null)
        {
            if (input == null)
                return null;

            if (String.IsNullOrEmpty(element))
            {
                string name = input.GetType().Name;
                element = name.Contains("AnonymousType")
                    ? "Object"
                    : arrayIndex != null
                        ? arrayName + "_" + arrayIndex
                        : name;
            }

            element = XmlConvert.EncodeName(element);
            var ret = new XElement(element);

            if (input != null)
            {
                var type = input.GetType();
                var props = type.GetProperties();

                var elements = props.Select(p => {
                    var pType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                    var name = XmlConvert.EncodeName(p.Name);
                    var val = pType.IsArray ? "array" : p.GetValue(input, null);
                    var value = pType.IsArray
                        ? GetArrayElement(p, (Array)p.GetValue(input, null))
                        : pType.IsSimpleType() || pType.IsEnum
                            ? new XElement(name, val)
                            : val.ToXml(name);
                    return value;
                })
                .Where(v => v != null);

                ret.Add(elements);
            }

            return ret;
        }

        #region helpers
        /// <summary>
        /// Gets the array element.
        /// </summary>
        /// <param name="info">The property info.</param>
        /// <param name="input">The input object.</param>
        /// <returns>Returns an XElement with the array collection as child elements.</returns>
        private static XElement GetArrayElement(PropertyInfo info, Array input)
        {
            var name = XmlConvert.EncodeName(info.Name);

            XElement rootElement = new XElement(name);

            var arrayCount = input == null ? 0 : input.GetLength(0);

            for (int i = 0; i < arrayCount; i++)
            {
                var val = input.GetValue(i);
                XElement childElement = val.GetType().IsSimpleType() ? new XElement(name + "_" + i, val) : _ToXml(val, null, i, name);

                rootElement.Add(childElement);
            }

            return rootElement;
        }

        #region .IsSimpleType
        public static bool IsSimpleType(this Type type)
        {
            return type.IsPrimitive || WriteTypes.Contains(type);
        }
        #endregion .IsSimpleType

        #endregion helpers
        #endregion .ToXml
    }
}