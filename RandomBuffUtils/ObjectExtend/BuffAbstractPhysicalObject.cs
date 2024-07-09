using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RandomBuffUtils.ObjectExtend
{
    /// <summary>
    /// 在继承于AbstractPhysicObject及其子类的类型前加该Attribute，
    /// 可以自动构造序列化及反序列化函数。（即可以进行轮回间保存）
    /// 
    /// 若该类型同时在Fisobs注册会导致未定义行为。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]

    public class BuffAbstractPhysicalObjectAttribute : System.Attribute
    {
    }

    /// <summary>
    /// 仅在标有BuffAbstractPhysicalObject特性的类型内有效
    /// 在需要进行存储的数据前加上该Attribute可以自动进行数据保存读取
    ///  
    /// 若该类型同时在Fisobs注册会导致未定义行为。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]

    public class BuffAbstractPhysicalObjectPropertyAttribute : System.Attribute
    {
    }

    /// <summary>
    /// 若不继承基类的构造函数请在自己的BuffAbstractPhysicalObject使用该接口
    /// </summary>
    public interface IBuffAbstractPhysicalObjectInitialization
    {
        public AbstractPhysicalObject Initialize(World world, AbstractPhysicalObject.AbstractObjectType type,
            WorldCoordinate pos, EntityID Id, string[] unrecognizedAttributes);
    }

}
