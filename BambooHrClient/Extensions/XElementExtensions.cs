using System;
using System.Xml.Linq;

namespace BambooHrClient
{
    public static class XElementExtensions
    {
        public static void AddFieldValueIfNotNull(this XElement xElement, string name, string value)
        {
            if (value == null)
                return;

            var fieldElement = new XElement("field");

            fieldElement.Add(new XAttribute("id", name));
            fieldElement.Value = value;

            xElement.Add(fieldElement);
        }
        public static void AddFieldValueIfNotNull(this XElement xElement, string name, DateTime? value)
        {
            if (value == null)
                return;

            var fieldElement = new XElement("field");

            fieldElement.Add(new XAttribute("id", name));
            fieldElement.Value = value.Value.ToString("yyyy-MM-dd");

            xElement.Add(fieldElement);
        }
        public static void AddFieldValueIfNotNull<T>(this XElement xElement, string name, T? value) where T : struct
        {
            if (value == null)
                return;

            var fieldElement = new XElement("field");

            fieldElement.Add(new XAttribute("id", name));
            fieldElement.Value = value.Value.ToString();

            xElement.Add(fieldElement);
        }
    }
}
