using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuffUtils.Simple3D
{
    public class MeshHelper
    {
        public static Mesh3D CreateOctahedron(Vector3 scale)
        {
            Mesh3D.TriangleFacet[] facets = new Mesh3D.TriangleFacet[]
            {
                new Mesh3D.TriangleFacet(0,1,2),
                new Mesh3D.TriangleFacet(0,2,3),
                new Mesh3D.TriangleFacet(0,3,4),
                new Mesh3D.TriangleFacet(0,4,1),

                new Mesh3D.TriangleFacet(5,1,2),
                new Mesh3D.TriangleFacet(5,2,3),
                new Mesh3D.TriangleFacet(5,3,4),
                new Mesh3D.TriangleFacet(5,4,1),
            };
            Mesh3D result = new Mesh3D();
            result.SetFacet(facets);

            result.SetVertice(0, Vector3.up * scale.y);
            result.SetVertice(5, Vector3.down * scale.y);

            result.SetVertice(1, Vector3.left * scale.x);
            result.SetVertice(3, Vector3.right * scale.x);
            result.SetVertice(2, Vector3.forward * scale.z);
            result.SetVertice(4, Vector3.back * scale.z);
            return result;
        }

        public static Mesh3D CreatePyramid(Vector3 scale)
        {
            Mesh3D.TriangleFacet[] facets = new Mesh3D.TriangleFacet[]
            {
                new Mesh3D.TriangleFacet(0,1,2),
                new Mesh3D.TriangleFacet(0,2,3),
                new Mesh3D.TriangleFacet(0,3,4),
                new Mesh3D.TriangleFacet(0,4,1),
            };
            Mesh3D result = new Mesh3D();
            result.SetFacet(facets);

            result.SetVertice(0, Vector3.up * scale.y);

            result.SetVertice(1, Vector3.left * scale.x);
            result.SetVertice(3, Vector3.right * scale.x);
            result.SetVertice(2, Vector3.forward * scale.z);
            result.SetVertice(4, Vector3.back * scale.z);
            return result;
        }

        public static Mesh3D CreateDiamond(Vector2 scale)
        {
            Mesh3D.TriangleFacet[] facets = new Mesh3D.TriangleFacet[]
            {
                new Mesh3D.TriangleFacet(0,1,2),
                new Mesh3D.TriangleFacet(1,2,3), 
            };

            Mesh3D result = new Mesh3D();
            result.SetFacet(facets);

            result.SetVertice(0, Vector3.up * scale.y);
            result.SetVertice(1, Vector3.left * scale.x);
            result.SetVertice(2, Vector3.right * scale.x);
            result.SetVertice(3, Vector3.down * scale.y);
            return result;
        }

        public static Mesh3D Create6x6DotMatrixMesh(float width)
        {
            List<Mesh3D.TriangleFacet> facets = new List<Mesh3D.TriangleFacet>();//6x6x6
            for (int i = 0; i < 216; i += 3)
            {
                facets.Add(new Mesh3D.TriangleFacet(i, i + 1, i + 2));
            }
            Mesh3D result = new Mesh3D();
            result.SetFacet(facets.ToArray());

            for (int z = 0; z < 6; z++)
            {
                for (int y = 0; y < 6; y++)
                {
                    for (int x = 0; x < 6; x++)
                    {
                        result.SetVertice(z + y * 6 + x * 36, (new Vector3(3 - x, 3 - y, 3 - z) - Vector3.one * 0.5f) * width);
                    }
                }
            }
            return result;
        }
    }
}
