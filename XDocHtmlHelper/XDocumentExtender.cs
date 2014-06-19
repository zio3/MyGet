using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace System.Xml.Linq
{
    static public class XDocumentExtender
    {
        public static string ClassName(this XElement elem)
        {
            if (elem.Attribute("class") == null)
                return null;
            return (string)elem.Attribute("class");
        }
        public static bool ClassHas(this XElement elem, string name)
        {
            if (elem.Attribute("class") == null)
                return false;

            return (((string)elem.Attribute("class")).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Contains(name);
        }
        static public bool ClassEqual(this XElement elem, string name)
        {
            if (elem.Attribute("class") == null)
                return false;
            return (string)elem.Attribute("class") == name;
        }

        public static string ElemName(this XElement elem)
        {
            return (string)elem.Attribute("name");
        }
        public static string Href(this XElement elem)
        {
            return (string)elem.Attribute("href");
        }
        static public string Id(this XElement elem)
        {
            return (string)elem.Attribute("id");
        }
        static public bool IdEqual(this XElement elem, string name)
        {
            return (string)elem.Attribute("id") == name;
        }

        static public string AttrStr(this XElement elem, string name)
        {
            return (string)elem.Attribute(name);
        }
        static public int? AttrInt(this XElement elem, string name)
        {
            return StrToIntoOrNull(AttrStr(elem,name));
        }

        static int? StrToIntoOrNull(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return null;

            int r = 0;
            if( int.TryParse(str,out r))
            {
                return r;
            }
            return null;
        }

        static public bool IsChecked(this XElement elem)
        {
            return elem.Attribute("checked") != null;
        }

        static public bool HasAttribute(this XElement elem, string name)
        {
            return elem.Attribute(name) != null;
        }

        static Dictionary<string, string> FormToDic(this XElement form)
        {
            var result = new Dictionary<string, string>();
            foreach (var elem in form.Descendants())
            {
                var name = (string)elem.Attribute("name");
                if (string.IsNullOrEmpty(name))
                    continue;

                switch (elem.Name.LocalName.ToLower())
                {
                    case "input":
                        InputCase(elem, result);
                        break;
                    case "textarea":
                        result[name] = elem.Value;
                        break;
                    case "select":
                        var target = elem.Elements("option").FirstOrDefault(c => c.HasAttribute("selected"));
                        if (target == null)
                            target = elem.Elements("option").FirstOrDefault();
                        if (target == null)
                            continue;
                        result[name] = target.AttrStr("value");
                        break;

                    default:
                        break;
                }
            }
            return result;
        }

        static private void InputCase(XElement input, Dictionary<string, string> dic)
        {
            var name = (string)input.Attribute("name");
            switch ((string)input.Attribute("type"))
            {
                case "checkbox":
                    if (input.HasAttribute("checked"))
                        dic[name] = input.AttrStr("value");
                    break;
                case "radio":
                    if (input.HasAttribute("checked"))
                        dic[name] = input.AttrStr("value");
                    break;

                default:
                    //Console.WriteLine(input.Attribute("type"));
                    dic[name] = input.AttrStr("value");
                    break;
            }
        }


    }
}
