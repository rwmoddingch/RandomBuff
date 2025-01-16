using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuffUtils.CreatureExtend
{
    public static class BuffCreatureExtend
    {
        internal static List<BuffCreatureRegister> managedRegisters = new List<BuffCreatureRegister>();
        internal static List<BuffCreatureRegister> enabledRegisters = new List<BuffCreatureRegister>();
        internal static List<BuffCreatureRegister> registersToRemove = new List<BuffCreatureRegister>();

        internal static Dictionary<string, BuffCreatureRegister> typeValue2RegisterMapping = new Dictionary<string, BuffCreatureRegister>();

        public static void Register(BuffCreatureRegister buffCreatureRegister)
        {
            if(typeValue2RegisterMapping.ContainsKey(buffCreatureRegister.typeValue))
            {
                BuffUtils.LogWarning("BuffCreatureExtend", $"Type value of {buffCreatureRegister.typeValue} already registed.");
                return;
            }

            managedRegisters.Add(buffCreatureRegister);
            typeValue2RegisterMapping.Add(buffCreatureRegister.typeValue, buffCreatureRegister);
        }

        internal static void ActuallyDisableRegisters()
        {
            foreach(var register in registersToRemove)
            {
                register.OnRegisterDisable();
                register.currentlyEnabled = false;
                enabledRegisters.Remove(register);
            }
            registersToRemove.Clear();
        }
    }

    public static class BuffCreatureRegisterEnabler
    {
        /// <summary>
        /// 启用注册的生物类型
        /// </summary>
        /// <param name="buffCreatureRegister"></param>
        public static void Enable(this BuffCreatureRegister buffCreatureRegister)
        {
            if (!BuffCreatureExtend.managedRegisters.Contains(buffCreatureRegister))
            {
                BuffUtils.LogWarning("BuffCreatureExtend", $"You may forget registering {buffCreatureRegister.typeValue}");
                return;
            }

            if (BuffCreatureExtend.enabledRegisters.Contains(buffCreatureRegister))
            {
                if(BuffCreatureExtend.registersToRemove.Contains(buffCreatureRegister))
                    BuffCreatureExtend.registersToRemove.Remove(buffCreatureRegister);

                return;
            }
            
            BuffCreatureExtend.enabledRegisters.Add(buffCreatureRegister);
            buffCreatureRegister.OnRegisterEnable();
            buffCreatureRegister.currentlyEnabled = true;
        }

        /// <summary>
        /// 禁用注册的生物类型，会在轮回结束时生效
        /// </summary>
        /// <param name="buffCreatureRegister"></param>
        public static void Disable(this BuffCreatureRegister buffCreatureRegister)
        {
            if (!BuffCreatureExtend.managedRegisters.Contains(buffCreatureRegister))
            {
                BuffUtils.LogWarning("BuffCreatureExtend", $"You may forget registering {buffCreatureRegister.typeValue}");
                return;
            }

            if (!BuffCreatureExtend.enabledRegisters.Contains(buffCreatureRegister))
                return;

            if (BuffCreatureExtend.registersToRemove.Contains(buffCreatureRegister))
                return;

            BuffCreatureExtend.registersToRemove.Add(buffCreatureRegister);
        }
    }
}
