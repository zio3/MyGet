using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace XDocHtmlHelper
{
    static public class XDocumentExtender
    {
        public static string ClassName(this XElement elem)
        {
            return (string)elem.Attribute("class");
        }
        public static bool ClassHas(this XElement elem, string name)
        {
            return (((string)elem.Attribute("class")).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).Contains("name");
        }
        static public bool ClassEqual(this XElement elem, string name)
        {
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
            return (string)elem.Attribute("name");
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


    }
}
