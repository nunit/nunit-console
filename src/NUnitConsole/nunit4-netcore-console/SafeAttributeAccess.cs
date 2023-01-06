// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Globalization;
using System.Xml;

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    sealed class ExtensionAttribute : Attribute { }
}

namespace NUnit.ConsoleRunner
{
    /// <summary>
    /// SafeAttributeAccess provides an extension method for accessing XML attributes.
    /// </summary>
    public static class SafeAttributeAccess
    {
        /// <summary>
        /// Gets the value of the given attribute.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static string GetAttribute(this XmlNode result, string name)
        {
            XmlAttribute attr = result.Attributes[name];

            return attr == null ? null : attr.Value;
        }

        /// <summary>
        /// Gets the value of the given attribute as a double.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public static double GetAttribute(this XmlNode result, string name, double defaultValue)
        {
            XmlAttribute attr = result.Attributes[name];

            return attr == null
                ? defaultValue
                : double.Parse(attr.Value, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the value of the given attribute as a DateTime.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public static DateTime GetAttribute(this XmlNode result, string name, DateTime defaultValue)
        {
            string dateStr = GetAttribute(result, name);
            if (dateStr == null)
                return defaultValue;

            DateTime date;
            if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces, out date))
                return defaultValue;

            return date;
        }
    }
}
