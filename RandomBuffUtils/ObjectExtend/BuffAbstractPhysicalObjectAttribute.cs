using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RandomBuffUtils.ObjectExtend
{

    [AttributeUsage(AttributeTargets.Class)]

    public class BuffAbstractPhysicalObjectAttribute : System.Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]

    public class BuffAbstractPhysicalObjectPropertyAttribute : System.Attribute
    {
    }

}
