﻿using RestSharp.Serializers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using RestSharp;
using RestSharp.Serializers.Xml;
using System.Xml.Serialization;
using System.Linq;

namespace BambooHrClient.Models
{
    [DeserializeAs(Name = "list")]
    public class BambooHrListField
    {
        public int FieldId { get; set; }
        public string Alias { get; set; }
        public string Manageable { get; set; }
        public string Multiple { get; set; }
        public string Name { get; set; }

        [XmlIgnore]
        public List<BambooHrListFieldOption> Options { get; set; }

        [XmlArray("Options")]
        public BambooHrListFieldOption[] FilteredOptions => Options?.Where(w  => !String.IsNullOrWhiteSpace(w.Value)).ToArray();

        /// <summary>
        /// Parameterless constructor for XML deserialization.
        /// </summary>
        public BambooHrListField()
        {

        }
    }

    public class BambooHrListFieldOptionSerializer : ISerializer
    {
        public ContentType ContentType { get; set; }
        public string DateFormat { get; set; }
        public string Namespace { get; set; }
        public string RootElement { get; set; }

        public BambooHrListFieldOptionSerializer()
        {
            ContentType = ContentType.Xml;
        }

        public string Serialize(object obj)
        {
            var list = obj as List<BambooHrListFieldOption>;
            
            if (list == null)
                return new DotNetXmlSerializer().Serialize(obj);

            var stringBuilder = new StringBuilder();

            stringBuilder.Append("<options>");

            foreach (var item in list)
            {
                if (string.IsNullOrWhiteSpace(item.Value))
                    continue;

                var xElement = new XElement("option", item.Value);

                if (item.Id > 0)
                    xElement.Add(new XAttribute("id", item.Id));

                if (!string.IsNullOrWhiteSpace(item.Archived))
                    xElement.Add(new XAttribute("archived", item.Archived));

                stringBuilder.Append(xElement);
            }

            stringBuilder.Append("</options>");

            return stringBuilder.ToString();
        }
    }

    [DeserializeAs(Name = "option")]
    public class BambooHrListFieldOption
    {
        public int Id { get; set; }
        public string Archived { get; set; }
        public string Value { get; set; }

        public DateTime? CreatedDate { get; set; }
        public DateTime? ArchivedDate { get; set; }

        /// <summary>
        /// Parameterless constructor for XML deserialization.
        /// </summary>
        public BambooHrListFieldOption()
        {

        }
    }
}
