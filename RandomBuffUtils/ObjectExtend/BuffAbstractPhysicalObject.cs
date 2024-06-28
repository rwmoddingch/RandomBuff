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

    /// <summary>
    /// 若不继承基类的构造函数请在自己的abstractObject使用该接口
    /// </summary>
    public interface IBuffAbstractPhysicalObjectInitialization
    {
        public AbstractPhysicalObject Initialize(World world, AbstractPhysicalObject.AbstractObjectType type,
            WorldCoordinate pos, EntityID Id, string[] unrecognizedAttributes);
    }

}
