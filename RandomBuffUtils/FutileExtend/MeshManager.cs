using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using UnityEngine;
using static RandomBuffUtils.FutileExtend.FMesh;

namespace RandomBuffUtils.FutileExtend
{
    public static class MeshManager
    {

        internal static FFacetType Mesh;

        public static Mesh3DAsset Cube;
        public static Mesh3DAsset Plane;
        public static Mesh3DAsset Sphere;

        internal static void OnModsInit()
        {
            IL.FFacetRenderLayer.UpdateMeshProperties += FFacetRenderLayer_UpdateMeshPropertiesIL;
            On.FFacetRenderLayer.UpdateMeshProperties += FFacetRenderLayer_UpdateMeshProperties;
            Mesh = FFacetType.CreateFacetType("RandomBuff.Pkuyo.Mesh", 16, 16, 64,
                (stage, type, atlas, shader) => new FMeshRenderLayer(stage, type, atlas, shader));
            Cube = LoadMesh("Cube", "buffassets/assetbundles/futileextend/Cube.obj");
            Plane = LoadMesh("Plane", "buffassets/assetbundles/futileextend/Plane.obj");
            Sphere = LoadMesh("Sphere", "buffassets/assetbundles/futileextend/Sphere.obj");

        }

        private static void FFacetRenderLayer_UpdateMeshProperties(On.FFacetRenderLayer.orig_UpdateMeshProperties orig, FFacetRenderLayer self)
        {
            orig(self);
            if (self is FMeshRenderLayer { _didNormalsChange: true } layer)
            {
                layer._mesh.normals = layer._normals;
                layer._didNormalsChange = false;
            }
        }

        private static void FFacetRenderLayer_UpdateMeshPropertiesIL(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After, i => i.MatchCallOrCallvirt<Mesh>("set_colors"));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<FFacetRenderLayer>>((self) =>
            {
                if (self is FMeshRenderLayer layer)
                {
                    layer._didNormalsChange = false;
                    layer._mesh.normals = layer._normals;
                }
            });
        }

        public static Mesh3DAsset LoadMesh(string name, string path)
        {
            if (MeshAssets.ContainsKey(name))
                throw new FutileException($"already contains mesh named: {name}");
            

            if (!File.Exists(path))
                path = AssetManager.ResolveFilePath(path);
            if (!File.Exists(path))
                throw new FutileException($"can't find obj mesh file at: {path}");

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Mesh3DAsset.TriangleFacet> facets = new List<Mesh3DAsset.TriangleFacet>();
            string[] lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
               
                string[] chars = line.Split(' ');
                switch (chars[0])
                {
                    case "v":
                        vertices.Add(new Vector3(float.Parse(chars[1]), float.Parse(chars[2]), float.Parse(chars[3])));
                        break;
                    case "vt":
                        uvs.Add(new Vector2(float.Parse(chars[1]), float.Parse(chars[2])));
                        break;
                    case "vn":
                        normals.Add(new Vector3(float.Parse(chars[1]), float.Parse(chars[2]), float.Parse(chars[3])));
                        break;
                    case "f":
                        //if (chars.Length > 4)
                        //   throw new FutileException("Mesh3DAsset.LoadMeshFromPath only support triangle mesh!");
                        int[] vec = new int[chars.Length - 1];
                        int[] uv = new int[chars.Length - 1];
                        int[] normal = new int[chars.Length - 1];
                        for (int i = 1; i < chars.Length; i++)//三角面
                        {
                            string[] face = chars[i].Split('/');
                            vec[i - 1] = int.Parse(face[0]) - 1;
                            if (face.Length == 3)
                            {
                                normal[i - 1] = face[2] != string.Empty ? int.Parse(face[2]) - 1 : vec[i - 1];
                                uv[i - 1] = face[1] != string.Empty ? int.Parse(face[1]) - 1 : vec[i - 1];
                    
                            }
                            else if (face.Length == 2)
                            {
                                uv[i - 1] = face[1] != string.Empty ? int.Parse(face[1]) - 1 : vec[i - 1];
                                normal[i - 1] = vec[i - 1];
                            }
                            else
                            {
                                uv[i - 1] = vec[i - 1];
                                normal[i - 1] = vec[i - 1];
                            }
                        }
                        for(int i = 0;i< uv.Length-2;i++)
                            facets.Add(new Mesh3DAsset.TriangleFacet(
                                new Mesh3DAsset.TriangleFacet.TriangleArray(vec, i),
                                new Mesh3DAsset.TriangleFacet.TriangleArray(uv, i),
                                new Mesh3DAsset.TriangleFacet.TriangleArray(normal, i)));
                        break;
                }
            }
            BuffUtils.Log("FutileExtend", $"Import Mesh: name:{name}, vertices:{vertices.Count}, uvs:{uvs.Count}, normals:{normals.Count}, facets:{facets.Count}");

            var re = new Mesh3DAsset(vertices.ToArray(), uvs.ToArray(), facets.ToArray(), normals.ToArray());
            MeshAssets.Add(name,re);
            return re;
        }

        public static Mesh3DAsset GetMeshByName(string name)
        {
            if (MeshAssets.TryGetValue(name, out var mesh))
                return mesh;
            throw new FutileException($"Can't find mesh by name:{name}");
        }
        public static bool DoesContainsMesh(string name)
        {
            return MeshAssets.ContainsKey(name);
        }
        private static readonly Dictionary<string, Mesh3DAsset> MeshAssets = new();
    }
}
