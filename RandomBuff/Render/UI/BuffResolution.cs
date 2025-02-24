using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI
{
    public static class BuffResolution
    {
        static Vector2 screenSize;
        static Vector2 halfScreenSize;
        public static Vector2 DesignedRes => new Vector2(1366f, 768f);
        public static Vector2 HalfDesignedRes => new Vector2(683f, 384f);
        public static Vector2 ScreenSize
        {
            get
            {
                if (screenSize != Custom.rainWorld.options.ScreenSize)
                    CaculateCof();
                return screenSize;
            }
        }
        public static Vector2 HalfScreenSize
        {
            get
            {
                if (screenSize != Custom.rainWorld.options.ScreenSize)
                    CaculateCof();
                return halfScreenSize;
            }
        }

        public static Vector2 Convert(Vector2 designedPos, ConvertType convertType)
        {
            if (screenSize != Custom.rainWorld.options.ScreenSize)
                CaculateCof();
            if (screenSize == DesignedRes)
                return designedPos;

            Vector2 result = Vector2.zero;

            if (convertType.HasFlag(ConvertType.ScaleToCenter))
            {
                Vector2 delta = designedPos - HalfDesignedRes;
                result.x = delta.x * screenSize.x / DesignedRes.x + halfScreenSize.x;
                result.y = delta.y * screenSize.y / DesignedRes.y + halfScreenSize.y;
            }
            else if (convertType.HasFlag(ConvertType.ScaleToZeroPoint))
            {
                result.x = designedPos.x / DesignedRes.x * screenSize.x;
                result.y = designedPos.y / DesignedRes.y * ScreenSize.y;
            }
            else if (convertType.HasFlag(ConvertType.KeepCenterSpan))
            {
                Vector2 delta = designedPos - HalfDesignedRes;
                result = halfScreenSize + delta;
            }
            else
            {
                if (convertType.HasFlag(ConvertType.xKeepLeftSpan))
                {
                    result.x = designedPos.x;
                }
                else if (convertType.HasFlag(ConvertType.xKeepRightSpan))
                {
                    result.x = screenSize.x - (DesignedRes.x - designedPos.x);
                }
                else if (convertType.HasFlag(ConvertType.xScaleToLeft))
                {
                    result.x = designedPos.x / DesignedRes.x * screenSize.x;
                }
                else if (convertType.HasFlag(ConvertType.xScaleToRight))
                {
                    result.x = screenSize.x - (DesignedRes.x - designedPos.x) / DesignedRes.x * screenSize.x;
                }
                else if (convertType.HasFlag(ConvertType.xScaleToCenter))
                {
                    result.x = (designedPos.x - HalfDesignedRes.x) / DesignedRes.x * screenSize.x + halfScreenSize.x;
                }
                else if (convertType.HasFlag(ConvertType.xKeepCenterSpan))
                {
                    result.x = (designedPos.x - HalfDesignedRes.x) + halfScreenSize.x;
                }
                else
                    result.x = designedPos.x;

                if (convertType.HasFlag(ConvertType.yKeepDownSpan))
                {
                    result.y = designedPos.y;
                }
                else if (convertType.HasFlag(ConvertType.yKeepUpSpan))
                {
                    result.y = screenSize.y - (DesignedRes.y - designedPos.y);
                }
                else if (convertType.HasFlag(ConvertType.yScaleToDown))
                {
                    result.y = designedPos.y / DesignedRes.y * screenSize.y;
                }
                else if (convertType.HasFlag(ConvertType.yScaleToUp))
                {
                    result.y = screenSize.y - (DesignedRes.y - designedPos.y) / DesignedRes.y * screenSize.y;
                }
                else if (convertType.HasFlag(ConvertType.yScaleToCenter))
                {
                    result.y = (designedPos.y - HalfDesignedRes.y) / DesignedRes.y * screenSize.y + halfScreenSize.y;
                }
                else if (convertType.HasFlag(ConvertType.yKeepCenterSpan))
                {
                    result.y = (designedPos.y - HalfDesignedRes.y) + halfScreenSize.y;
                }
                else
                    result.y = designedPos.y;
            }

            return result;
        }

        static void CaculateCof()
        {
            screenSize = Custom.rainWorld.options.ScreenSize;
            halfScreenSize = screenSize / 2f;
            BuffPlugin.Log($"BuffResolution : ScreenSize set : {screenSize.x},{screenSize.y}");
        }

        [Flags]
        public enum ConvertType
        {
            None = 0,
            xKeepLeftSpan = 1,
            yKeepUpSpan = 2,
            xKeepRightSpan = 4,
            yKeepDownSpan = 8,
            KeepCenterSpan = 16,
            xScaleToLeft = 32,
            yScaleToUp = 64,
            xScaleToRight = 128,
            yScaleToDown = 256,
            ScaleToCenter = 512,
            ScaleToZeroPoint = 1024,
            xScaleToCenter = 2048,
            yScaleToCenter = 4096,
            xKeepCenterSpan = 8192,
            yKeepCenterSpan = 16384,
        }
    }
}
