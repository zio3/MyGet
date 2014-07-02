using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsTsInterop
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TsControllerAttribute : Attribute
    {
        public string OffsetUrl { get; set; }

        public TsControllerAttribute()
        { 

        }
        public TsControllerAttribute(string offsetUrl)
        {
            this.OffsetUrl = offsetUrl;
        }
    }
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TsHubAttribute : Attribute
    {
        public Type ClientInterface { get; set; }
        public TsHubAttribute(Type clientInterface = null)
        {
            ClientInterface = clientInterface;
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class TsControllerActionAttribute : Attribute
    {    
        public Type ResultType { get; set; }
 
        public TsControllerActionAttribute(Type resultType = null)
        {
            ResultType = resultType;
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class TsControllerHtmlActionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class TsControllerJsonActionAttribute : Attribute
    {
        public Type ResultType { get; set; }

        public TsControllerJsonActionAttribute(Type resultType = null)
        {
            ResultType = resultType;
        }
    }


    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class TsTypeInterfaceAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
    public sealed class TsEnumAttribute : Attribute
    {
    }

}
