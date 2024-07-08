using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;

namespace RandomBuffUtils.FutileExtend
{
    public static class UniformLighting
    {
        public static int ID_LightDir { get; private set; }
        public static int ID_SpecularColor { get; private set; }
        public static int ID_LightColor { get; private set; }
        public static int ID_Ambient { get; private set; }

        public static int ID_PointLight_Count { get; private set; }
        public static int ID_PointLight_Pos { get; private set; }
        public static int ID_PointLight_Color { get; private set; }
        public static int ID_PointLight_Range { get; private set; }
        public static int ID_PointLight_Intensity { get; private set; }

        public static Vector3 LightDir
        {
            get => Shader.GetGlobalVector(ID_LightDir);
            set => Shader.SetGlobalVector(ID_LightDir, value);
        }

        public static Color SpecularColor
        {
            get => Shader.GetGlobalColor(ID_SpecularColor);
            set => Shader.SetGlobalColor(ID_SpecularColor, value);
        }
        public static Color LightColor
        {
            get => Shader.GetGlobalColor(ID_LightColor);
            set => Shader.SetGlobalColor(ID_LightColor, value);
        }
        public static Color Ambient
        {
            get => Shader.GetGlobalColor(ID_Ambient);
            set => Shader.SetGlobalColor(ID_Ambient, value);
        }

        public static Mesh TestMesh;
        public static Texture2D TestTexture;

        static Vector4[] PointLight_Color = new Vector4[20];
        static Vector4[] PointLight_Pos = new Vector4[20];
        static float[] PointLight_Range = new float[20];
        static float[] PointLight_Intensity = new float[20];

        internal static void OnModsInit()
        {
            On.RoomCamera.ApplyPalette += RoomCamera_ApplyPalette;
            On.RoomCamera.Update += RoomCamera_Update;

            var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("buffassets/assetbundles/futileextend/futileextend"));
            Custom.rainWorld.Shaders.Add("UniformSimpleLighting", FShader.CreateShader("UniformSimpleLighting",
                bundle.LoadAsset<Shader>("UniformSimpleLighting")));

            TestMesh = bundle.LoadAsset<Mesh>("flameThrower.obj");
            TestTexture = bundle.LoadAsset<Texture2D>("flameThrowerTextureTex.png");

            ID_LightDir = Shader.PropertyToID("uniformSimpleLighting_LightDir");
            ID_SpecularColor = Shader.PropertyToID("uniformSimpleLighting_SpecularCol");
            ID_LightColor = Shader.PropertyToID("uniformSimpleLighting_LightCol");
            ID_Ambient = Shader.PropertyToID("uniformSimpleLighting_Ambient");

            ID_PointLight_Count = Shader.PropertyToID("uniformSimpleLighting_PointLight_Count");
            ID_PointLight_Color = Shader.PropertyToID("uniformSimpleLighting_PointLight_Color");
            ID_PointLight_Pos = Shader.PropertyToID("uniformSimpleLighting_PointLight_Pos");
            ID_PointLight_Range = Shader.PropertyToID("uniformSimpleLighting_PointLight_Range");
            ID_PointLight_Intensity = Shader.PropertyToID("uniformSimpleLighting_PointLight_Intensity");


            Shader.SetGlobalVector(ID_LightDir,new Vector3(1, 3,-2
                ));
            Shader.SetGlobalColor(ID_SpecularColor, Color.white);
            Shader.SetGlobalColor(ID_LightColor, Color.white);
            Shader.SetGlobalColor(ID_Ambient, new Color(0,0,0,0.02F));
            Camera.main.depthTextureMode |= DepthTextureMode.Depth;
        }

        private static void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            orig.Invoke(self);
            if (self.room == null)
                return;

            var lightSources = self.room.updateList.Where(u => u is LightSource)
                .Select(u => u as LightSource)
                .Where(l => !l.flat)
                .ToList();

            int pointLightCount = Mathf.Min(lightSources.Count, 20);
            //BuffUtils.Log("UniformLighting", $"count : {pointLightCount}");
            for(int i = 0; i < pointLightCount; i++)
            {
                PointLight_Color[i] = lightSources[i].color;
                PointLight_Intensity[i] = lightSources[i].Alpha * 10f;
                PointLight_Pos[i] = new Vector4(lightSources[i].pos.x, lightSources[i].pos.y, 0f, 0f);
                PointLight_Range[i] = lightSources[i].Rad * 10;
            }

            Shader.SetGlobalInt(ID_PointLight_Count, pointLightCount);
            Shader.SetGlobalVectorArray(ID_PointLight_Color, PointLight_Color);
            Shader.SetGlobalFloatArray(ID_PointLight_Intensity, PointLight_Intensity);
            Shader.SetGlobalVectorArray(ID_PointLight_Pos, PointLight_Pos);
            Shader.SetGlobalFloatArray(ID_PointLight_Range, PointLight_Range);
        }

        private static void RoomCamera_ApplyPalette(On.RoomCamera.orig_ApplyPalette orig, RoomCamera self)
        {
            orig(self);
            var lineColor = Color.black;
            for (int i = 0; i < self.paletteTexture.width; i++)
                lineColor =
                    lineColor.grayscale > self.paletteTexture.GetPixel(self.paletteTexture.height - 3, i).grayscale
                        ? lineColor
                        : self.paletteTexture.GetPixel(self.paletteTexture.height - 3, i);
            var col = (self.currentPalette.skyColor * 2).CloneWithNewAlpha(1);
            col = col.grayscale > lineColor.grayscale ? col : lineColor;
            SpecularColor = col;

            col = (self.currentPalette.fogColor * 2).CloneWithNewAlpha(1);
            col = col.grayscale > lineColor.grayscale ? col : lineColor;
            LightColor = col;

            Ambient = self.paletteTexture.GetPixel(3, 7).CloneWithNewAlpha(0.03f);

        }
    }
}
