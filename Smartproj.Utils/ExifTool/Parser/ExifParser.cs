using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;


namespace Smartproj.Utils
{
    public class ExifParser
    {
        public IDictionary<string, IEnumerable<Tag>> ParseTags(JArray json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }
            var dict = new Dictionary<string, IEnumerable<Tag>>();

            foreach (JContainer jtoken in json) 
            {
                var key = jtoken["SourceFile"];
                string value = "";
                if (key != null && (value = key.ToString()) != "")
                {
                    dict.Add(value, ParseTags((JObject)jtoken));
                }
            }
            return dict;
        }
        public IDictionary<string, IEnumerable<KeyValuePair<string, IEnumerable<JProperty>>>> ExtractTags(JArray json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }
            var dict = new Dictionary<string, IEnumerable<KeyValuePair<string, IEnumerable<JProperty>>>>();

            foreach (JContainer jtoken in json)
            {
                var key = jtoken["SourceFile"];
                string value = "";
                if (key != null && (value = key.ToString()) != "")
                {
                    dict.Add(value, ExtractTags((JObject)jtoken));
                }
            }
            return dict;
        }
        public IEnumerable<Tag> ParseTags(JObject json)
        {
            var list = new List<Tag>();

            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            foreach (var prop in json.Children().Cast<JProperty>())
            {
                if (prop.Value.Type != JTokenType.Object)
                {
                    // exif tags will be represented as objects,
                    // the first property currently is a string representing the filename
                    continue;
                }

                var tag = Parse(prop);

                if (tag != null)
                {
                    list.Add(tag);
                }
            }

            return list;
        }
        public IEnumerable<KeyValuePair<string, IEnumerable<JProperty>>> ExtractTags(JObject json)
        {
            var list = new List<KeyValuePair<string, IEnumerable<JProperty>>>();

            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            foreach (var prop in json.Children().Cast<JProperty>())
            {
                if (prop.Value.Type != JTokenType.Object)
                {
                    // exif tags will be represented as objects,
                    // the first property currently is a string representing the filename
                    continue;
                }

                List<JProperty> group = default;
                string[] namePair = prop.Name.Split(':');
                if (namePair.Length > 0)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].Key == namePair[0]) { group = (List<JProperty>)list[i].Value; break; }
                    }
                    if (group == default)
                    {
                        list.Add(new KeyValuePair<string, IEnumerable<JProperty>>(namePair[0], group = new List<JProperty>()));
                    }
                    group.Add(prop);
                }
            }

            return list;
        }
        Tag Parse(JProperty tagJson)
        {
            var data = tagJson.Value;
            var nameSepIndex = tagJson.Name.LastIndexOf(':');
            var group = tagJson.Name.Substring(0, nameSepIndex);
            var name = tagJson.Name.Substring(nameSepIndex + 1);
            var numValue = data["num"]?.ToString();
            var valJson = data["val"];
            IEnumerable<string> list = null;
            var jarray = valJson as JArray;

            if (jarray != null)
            {
                list = jarray.Select(x => (string)x);
            }

            return new Tag(data["id"]?.ToString(), name, (string)data["desc"], (string)data["table"], group, valJson.ToString(), numValue, list);
        }
    }
}
