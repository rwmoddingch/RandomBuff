using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuffUtils.CreatureExtend
{
    //实方法部分
    public partial class BuffCreatureRegister
    {
        public readonly string typeValue;//注册type使用的字符串值
        readonly CreatureTemplate.Relationship defaultOtherRelationship;
        readonly List<RelationshipEstablishInfo> relationshipEstablishInfo = new List<RelationshipEstablishInfo>();//注册关系使用的信息
        readonly Dictionary<string, RelationshipEstablishInfo> other2EstablishInfoMapper = new Dictionary<string, RelationshipEstablishInfo>();

        public CreatureTemplate.Type Type { get; private set; } //仅在启用后其中的类型才有意义

        public CreatureTemplate Template { get; private set; }//仅在启用活其中才具有实际值

        public BuffCreatureRegister(string typeValue, CreatureTemplate.Relationship defaultOtherRelationship)
        {
            this.typeValue = typeValue;
            this.defaultOtherRelationship = defaultOtherRelationship;

            foreach (var info in AddRelationshipEstablishInfos())
            {
                relationshipEstablishInfo.Add(info);
                other2EstablishInfoMapper.Add(info.other, info);
            }
        }

        public BuffCreatureRegister(string typeValue) : this(typeValue, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f))
        {
        }

        public void OnRegisterEnable()
        {
            Type = RegisterEnumType();
            Template = CreateCreatureTemplate();

            int insertIndex = Type.Index;
            foreach (var template in StaticWorld.creatureTemplates)
            {
                if (template == null)
                    continue;

                Array.Resize(ref template.relationships, CreatureTemplate.Type.values.Count);

                for(int i = template.relationships.Length - 1; i > insertIndex; i--)
                {
                    template.relationships[i] = template.relationships[i - 1];
                }

                if (other2EstablishInfoMapper.ContainsKey(template.type.value))
                    template.relationships[insertIndex] = other2EstablishInfoMapper[template.type.value].other2this;
                else
                    template.relationships[insertIndex] = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);
            }

            Array.Resize(ref StaticWorld.creatureTemplates, CreatureTemplate.Type.values.Count);
            StaticWorld.creatureTemplates[Type.index] = Template;
        }

        public void OnRegisterDisable()
        {
            int removeIndex = Type.Index;

            foreach (var template in StaticWorld.creatureTemplates)
            {
                if (template == null || template.type == Type)
                    continue;

                for(int i = removeIndex;i < template.relationships.Length - 1; i++)
                {
                    template.relationships[i] = template.relationships[i + 1];
                }
                Array.Resize(ref template.relationships, template.relationships.Length - 1);
            }

            for(int i = removeIndex;i < StaticWorld.creatureTemplates.Length; i++)
            {
                StaticWorld.creatureTemplates[i] = StaticWorld.creatureTemplates[i + 1];
            }
            Array.Resize(ref StaticWorld.creatureTemplates, StaticWorld.creatureTemplates.Length - 1);

            Type.Unregister();
            Type = null;
            Template = null;
        }
    }

    //虚方法部分
    public partial class BuffCreatureRegister
    {
        /// <summary>
        /// 注册 CreatureTemplate.Type 调用的方法
        /// 一般不需要覆写，在该生物类型启用时调用
        /// </summary>
        /// <returns></returns>
        public virtual CreatureTemplate.Type RegisterEnumType()
        {
            return null;
        }

        /// <summary>
        /// 创建 CreatureTemplate 调用的方法
        /// 生物间关系将根据 AddRelationshipEstablishInfos 中的结果来自动应用
        /// </summary>
        /// <returns></returns>
        public virtual CreatureTemplate CreateCreatureTemplate()
        {
            return null;
        }

        /// <summary>
        /// 提供建立特殊关系所需的信息
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<RelationshipEstablishInfo> AddRelationshipEstablishInfos()
        {
            yield break;
        }

        public struct RelationshipEstablishInfo
        {
            public string other;

            public CreatureTemplate.Relationship this2other;
            public CreatureTemplate.Relationship other2this;

            public RelationshipEstablishInfo(string other, CreatureTemplate.Relationship this2other, CreatureTemplate.Relationship other2this)
            {
                this.other = other;
                this.this2other = this2other;
                this.other2this = other2this;
            }
        }
    }
}
