using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MonoMod.RuntimeDetour;
using UnityEngine.Assertions;

namespace RandomBuff.Core.Entry
{
    public interface IBuffEntry
    {
        public void OnEnable();
    }

    /// <summary>
    /// 在HookOn创建，可在禁用hook时候自动回收
    /// </summary>
    public sealed class BuffAutoCallBack : IDetour
    {

        private Action onApply;
        private Action onDestroy;

        private bool isApplied = false;
        private bool isValid = true;

        public bool IsValid => isValid;

        public bool IsApplied => isApplied;

        public BuffAutoCallBack(Action onApply, Action onDestroy)
        {
            this.onApply = onApply;
            this.onDestroy = onDestroy;
            Apply();
        }
        public void Dispose()
        {
            Free();
        }

        public void Apply()
        {
            if (!isValid)
                throw new ArgumentException("Try apply invalid BuffAutoCallBack");
            if (!IsApplied)
            {
                onApply();
                isApplied = true;
            }
        }

      

        public void Free()
        {
            Undo();
            isValid = false;
        }
        public void Undo()
        {
            if (isApplied)
            {
                onDestroy();
                isApplied = false;
            }
        }

   

        #region Unused


        public MethodBase GenerateTrampoline(MethodBase signature = null)
        {
            throw new NotImplementedException();
        }

        public T GenerateTrampoline<T>() where T : Delegate
        {
            throw new NotImplementedException();
        }



        #endregion

    }
}
