using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static RandomBuffUtils.FutileExtend.FMesh;

namespace RandomBuffUtils.FutileExtend
{
    public static class MeshLoader
    {
        public static Mesh3DAsset LoadMeshFromPath(string path)
        {
            //要求uv和normal的索引和顶点索引一致

            if (!File.Exists(path))
                return null;
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
                        int[] vec = new int[3];
                        for (int i = 1; i < chars.Length; i++)//三角面
                        {
                            string[] face = chars[i].Split('/');
                            vec[i-1] = int.Parse(face[0]) - 1;
                        }
                        facets.Add(new Mesh3DAsset.TriangleFacet(vec));
                        break;
                }
            }
            return new Mesh3DAsset(vertices.ToArray(), uvs.ToArray(), facets.ToArray(), normals.ToArray());
        }
    }
}
