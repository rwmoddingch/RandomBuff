﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Progression.CosmeticUnlocks;

namespace RandomBuff.Core.Progression
{
    public class CosmeticUnlockID : ExtEnum<CosmeticUnlockID>
    {
        public CosmeticUnlockID(string value, bool register = false) : base(value, register)
        {
        }
        public static readonly CosmeticUnlockID FireWork = new(nameof(FireWork), true);
        public static readonly CosmeticUnlockID Test = new(nameof(Test), true);

    }

    public abstract partial class CosmeticUnlock
    {
        protected CosmeticUnlock() { }
        public abstract CosmeticUnlockID UnlockID { get; }

        public virtual void StartGame(RainWorldGame game) { }

        public virtual void Update(RainWorldGame game) {}

    }

    public abstract partial class CosmeticUnlock
    {
        public static void Register<T>() where T : CosmeticUnlock, new()
        {
            Register(typeof(T));
        }
        internal static void Register(Type type)
        {
            if (Activator.CreateInstance(type) is CosmeticUnlock unlock)
            {
                if (cosmeticUnlocks.ContainsKey(unlock.UnlockID))
                    BuffPlugin.LogError($"Cosmetic Unlocks: same UnlockID {unlock.UnlockID}.");
                else
                    cosmeticUnlocks.Add(unlock.UnlockID,type);
            }
            else
            {
                BuffPlugin.LogError($"Cosmetic Unlocks: register type {type.FullName} is not CosmeticUnlock");
            }
        }

        internal static void Init()
        {
            Register<TestCosmeticUnlock>();
        }

        internal static CosmeticUnlock CreateInstance(string name,RainWorldGame game)
        {
            var re = Activator.CreateInstance(cosmeticUnlocks[new CosmeticUnlockID(name)]) as CosmeticUnlock;
            try
            {
                re.StartGame(game);
            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e,$"CosmeticUnlock: {name} StartGame Error!");
                return null;
            }

            return re;
        }
        private static readonly Dictionary<CosmeticUnlockID, Type> cosmeticUnlocks = new ();
    }
}