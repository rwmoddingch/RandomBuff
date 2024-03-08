using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.SaveData.BuffConfig
{
    internal partial class TypeSerializer
    {
        public static void Register(Type type, TypeSerializer typeSerializer)
        {
            throw new NotImplementedException();
        }

        public static TypeSerializer GetSerializer(Type type)
        {
            throw new NotImplementedException();
        }

        public string Serialize(object value)
        {
            throw new NotImplementedException();
        }

        public object Deserialize(string value)
        {
            throw new NotImplementedException();
        }
    }
}
