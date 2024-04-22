using RWCustom;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace RandomBuff.Render.UI.Component
{
    public class RandomBuffFlag
    {
        public static int step = 120;
        public static float verDecay = 2.5f;

        List<Vert> verts = new List<Vert>();
        List<Spring> horizontalSpring = new List<Spring>();
        List<Spring> verticalSpring = new List<Spring>();
        List<Spring> crossSpring45 = new List<Spring>();
        List<Spring> crossSpring135 = new List<Spring>();
        List<Spring> hFlexionSpring = new List<Spring>();
        List<Spring> vFlexionSpring = new List<Spring>();
        List<IForce> forces = new List<IForce>();

        float cK = 150f;
        float hK = 400f;
        float vK = 300f;
        float hFK = 80f;
        float vFK = 150f;

        public float timeF => 1f / step;

        internal int gridX;
        internal int gridY;

        internal float cellX = 0.25f;
        internal float cellY = 0.25f;

        public Vector2 rect;
        public readonly float scaleX;
        public readonly float scaleY;

        float timeStacker;
        public RandomBuffFlag(IntVector2 grid, Vector2 rect, float cK = 150f, float hK = 400f, float vK = 300f, float hFK =40f, float vFK = 100f)
        {
            gridX = grid.x;
            gridY = grid.y;
            this.rect = rect;

            scaleX = rect.x / (cellX * gridX);
            scaleY = rect.y / (cellY * gridY);

            this.cK = cK;
            this.hK = hK;
            this.vK = vK;
            this.hFK = hFK;
            this.vFK = vFK;

            InitFlag();
        }

        void InitFlag()
        {
            for (int y = 0; y < gridY; y++)
            {
                for (int x = 0; x < gridX; x++)
                {
                    var vert = new Vert()
                    {
                        pos = new Vector3(x * cellX, -y * cellY, 0),
                        isStatic = y == 0
                    };
                    verts.Add(vert);
                }
            }

            #region InitSprings
            for (int y = 0; y < gridY; y++)//horizontal
            {
                for (int x = 0; x < gridX - 1; x++)
                {
                    float origLength = (GetVert(x, y).pos - GetVert(x + 1, y).pos).magnitude;
                    horizontalSpring.Add(new Spring(GetVertIndex(x, y), GetVertIndex(x + 1, y), origLength, hK));
                }
            }

            for (int x = 0; x < gridX; x++)//vertical
            {
                for (int y = 0; y < gridY - 1; y++)
                {
                    float origLength = (GetVert(x, y).pos - GetVert(x, y + 1).pos).magnitude;
                    verticalSpring.Add(new Spring(GetVertIndex(x, y), GetVertIndex(x, y + 1), origLength, vK));
                }
            }

            for (int x = 1; x < gridX; x++)
            {
                for (int y = 0; y < gridY - 1; y++)
                {
                    float origLength = (GetVert(x, y).pos - GetVert(x - 1, y + 1).pos).magnitude;
                    crossSpring45.Add(new Spring(GetVertIndex(x, y), GetVertIndex(x - 1, y + 1), origLength, cK));
                }
            }

            for (int x = 0; x < gridX - 1; x++)
            {
                for (int y = 0; y < gridY - 1; y++)
                {
                    float origLength = (GetVert(x, y).pos - GetVert(x + 1, y + 1).pos).magnitude;
                    crossSpring135.Add(new Spring(GetVertIndex(x, y), GetVertIndex(x + 1, y + 1), origLength, cK));
                }
            }

            for (int x = 0; x < gridX; x++)
            {
                for (int y = 0; y < gridY - 2; y++)
                {
                    float origLength = (GetVert(x, y).pos - GetVert(x, y + 2).pos).magnitude;
                    vFlexionSpring.Add(new Spring(GetVertIndex(x, y), GetVertIndex(x, y + 2), origLength, vFK));
                }
            }

            for (int y = 0; y < gridY; y++)
            {
                for (int x = 0; x < gridX - 2; x++)
                {
                    float origLength = (GetVert(x, y).pos - GetVert(x + 2, y).pos).magnitude;
                    hFlexionSpring.Add(new Spring(GetVertIndex(x, y), GetVertIndex(x + 2, y), origLength, hFK));
                }
            }
            #endregion

            foreach (var vert in verts)//初始旗帜收起
                vert.pos = new Vector3(vert.pos.x, vert.pos.y * 1.7f, vert.pos.z);

            forces.Add(new BaseForce() { force = Vector3.down * 9.8f });//重力
            forces.Add(new BaseWind(gridX * cellX / 2f, 3f));//风力
        }

        public void Update()
        {
            ApplySpringForce(horizontalSpring);
            ApplySpringForce(verticalSpring);
            ApplySpringForce(crossSpring45);
            ApplySpringForce(crossSpring135);
            ApplySpringForce(vFlexionSpring);
            ApplySpringForce(hFlexionSpring);

            UpdateVert();
        }

        void UpdateVert()
        {
            for (int i = 0; i < verts.Count; i++)
            {
                verts[i].pos += verts[i].ver * timeF;
                verts[i].ver -= verts[i].ver * timeF * verDecay;

                if (!verts[i].isStatic)
                {
                    foreach (var force in forces)
                    {
                        verts[i].ver += force.GetForce(verts[i].pos) * timeF;
                    }
                }
            }
        }

        void ApplySpringForce(List<Spring> springs)
        {
            foreach (var spring in springs)
            {
                Vert a = verts[spring.vertA];
                Vert b = verts[spring.vertB];

                float currentL = (a.pos - b.pos).magnitude;
                Vector3 dir = (a.pos - b.pos).normalized;//b -> a

                float acc = (currentL - spring.origLength) * spring.k * timeF;
                if (!a.isStatic) a.ver += acc * -dir;
                if (!b.isStatic) b.ver += acc * dir;
            }
        }

        internal int GetVertIndex(int x, int y)
        {
            return x + gridX * y;
        }

        internal Vert GetVert(int x, int y)
        {
            return verts[GetVertIndex(x, y)];
        }


        public class Vert
        {
            public Vector3 pos;
            public Vector3 ver;

            public bool isStatic;
        }

        public struct Spring
        {
            public int vertA;
            public int vertB;

            public float origLength;

            public float k;

            public Spring(int vertA, int vertB, float origLength, float k)
            {
                this.vertA = vertA;
                this.vertB = vertB;
                this.origLength = origLength;
                this.k = k;
            }
        }

        interface IForce
        {
            Vector3 Force { get; }
            Vector3 GetForce(Vector3 pos);
        }

        public struct BaseForce : IForce
        {
            public Vector3 force;
            public Vector3 Force { get => force; }
            public void UpdateForce()
            {

            }
            public Vector3 GetForce(Vector3 pos)
            {
                return Force;
            }
        }

        public struct BaseWind : IForce
        {
            float midX;
            float strength;
            float timeFactor;
            Vector3 force;
            public Vector3 Force { get => force; }

            public BaseWind(float midX, float strength, float timeFactor = 0.5f)
            {
                this.midX = midX;
                this.strength = strength;
                this.timeFactor = timeFactor;
            }

            public Vector3 GetForce(Vector3 pos)
            {
                //return (Mathf.Sin(pos.y + -Mathf.Abs(pos.x - midX + 0.5f * Mathf.Sin(Time.time * 0.25f * timeFactor)) + pos.z + Time.time * 0.75f * timeFactor) * 5f) * Vector3.forward * Mathf.Sin(Time.time * 1.5f * timeFactor) * strength;

                float cof = -pos.y * 1.5f;
                cof += -Time.time;
                cof += +Mathf.Pow(Mathf.Abs(pos.x - midX + Mathf.Cos(Time.time * 0.5f) * 0.2f) * 0.35f, 2f);
                return Mathf.Sin(cof) * Vector3.forward * strength;
            }
        }
    }

    public class FlagRenderer
    {
        protected RandomBuffFlag flag;
        public FContainer container;

        protected Vector2[] allPos;
        protected Vector2[] allLastPos;

        protected Vector3[] normals;
        protected Vector3[] lastNormals;
        protected float[] lights;

        protected Vector2[] uvs;

        protected Vector2 flagCenter;
        public Vector2 pos;


        public FlagRenderer(RandomBuffFlag flag)
        {
            this.flag = flag;
            container = new FContainer();

            flagCenter = new Vector2(flag.gridX * flag.cellX / 2f, -flag.gridY * flag.cellY / 2f);

            allPos = new Vector2[flag.gridX * flag.gridY];
            allLastPos = new Vector2[flag.gridX * flag.gridY];

            normals = new Vector3[flag.gridX * flag.gridY];
            lastNormals = new Vector3[flag.gridX * flag.gridY];

            uvs = new Vector2[flag.gridX * flag.gridY];

            for(int x = 0; x < flag.gridX; x++)
            {
                for(int y = 0; y < flag.gridY; y++)
                {
                    allPos[flag.GetVertIndex(x, y)] = Get2DPos(flag.GetVert(x, y).pos,x , y);
                    allLastPos[flag.GetVertIndex(x, y)] = allPos[flag.GetVertIndex(x, y)];
                    uvs[flag.GetVertIndex(x, y)] = new Vector2((float)x / (flag.gridX - 1), 1f - (float)y / (flag.gridY - 1));
                }
            }

            CaculateNormal();
            for (int x = 0; x < flag.gridX; x++)
            {
                for (int y = 0; y < flag.gridY; y++)
                {
                    lastNormals[flag.GetVertIndex(x, y)] = normals[flag.GetVertIndex(x, y)];
                }
            }
        }
        protected virtual Vector2 Get2DPos(Vector3 pos, int x, int y)
        {
            Vector2 orig = new Vector2(pos.x * flag.scaleX, pos.y * flag.scaleY);
            Vector2 dir = orig - flagCenter;
            return orig + dir * pos.z / 100f + this.pos;
        }
 
        public virtual void Update()
        {
            for (int x = 0; x < flag.gridX; x++)
            {
                for (int y = 0; y < flag.gridY; y++)
                {
                    allLastPos[flag.GetVertIndex(x, y)] = allPos[flag.GetVertIndex(x, y)];
                    allPos[flag.GetVertIndex(x, y)] = Get2DPos(flag.GetVert(x, y).pos, x, y);
                }
            }
            CaculateNormal();
        }

        protected virtual void CaculateNormal()
        {
            for(int x = 0; x < flag.gridX; x++)
            {
                for(int y = 0; y < flag.gridY; y++)
                {
                    lastNormals[flag.GetVertIndex(x, y)] = normals[flag.GetVertIndex(x, y)];
                }
            }

            for (int y = 0; y < flag.gridY - 1; y++)
            {
                for (int x = 0; x < flag.gridX - 1; x++)
                {
                    Vector3 a = (flag.GetVert(x + 1, y).pos - flag.GetVert(x, y).pos).normalized;
                    Vector3 b = (flag.GetVert(x, y + 1).pos - flag.GetVert(x, y).pos).normalized;
                    Vector3 norm = Vector3.Cross(a, b);
                    normals[flag.GetVertIndex(x, y)] = norm;
                }
                int x1 = flag.gridX - 1;
                Vector3 a1 = (flag.GetVert(x1, y + 1).pos - flag.GetVert(x1, y).pos).normalized;
                Vector3 b1 = (flag.GetVert(x1 - 1, y).pos - flag.GetVert(x1, y).pos).normalized;
                Vector3 norm1 = Vector3.Cross(a1, b1);
                normals[flag.GetVertIndex(x1, y)] = norm1;
            }

            int y1 = flag.gridY - 1;
            for (int x = 0; x < flag.gridX - 1; x++)
            {
                Vector3 a = (flag.GetVert(x, y1 - 1).pos - flag.GetVert(x, y1).pos).normalized;
                Vector3 b = (flag.GetVert(x + 1, y1).pos - flag.GetVert(x, y1).pos).normalized;
                Vector3 norm = Vector3.Cross(a, b);
                normals[flag.GetVertIndex(x, y1)] = norm;
            }
            int x2 = flag.gridX - 1;
            Vector3 a2 = (flag.GetVert(x2 - 1, y1).pos - flag.GetVert(x2, y1).pos).normalized;
            Vector3 b2 = (flag.GetVert(x2, y1 - 1).pos - flag.GetVert(x2, y1).pos).normalized;
            Vector3 norm2 = Vector3.Cross(a2, b2);
            normals[flag.GetVertIndex(x2, y1)] = norm2;
        }

        public virtual void GrafUpdate(float timeStacker)
        {
        }
    }

    public class TestFlagRenderer : FlagRenderer
    {
        TriangleMesh triangleMesh;
        TriangleMesh innerMesh;

        Vector3 lightDir;

        Color darkCol = new Color(0.2f, 0.2f, 0.2f);
        Color lightCol = new Color(0.8f, 0.8f, 0.8f);

        FlagColorModules.MetalColorModule goldenColor;
        FlagColorModules.NormalColorModule blackSilkColor;
        FlagColorModules.NormalColorModule edgeSilkColor;

        int innerGridX, innerGridY;
        int[] innerVerticeMapper;

        public TestFlagRenderer(RandomBuffFlag flag, int innerEdge) : base(flag)
        {
            lightDir = new Vector3(-1f, -1f, -1f).normalized;

            goldenColor = FlagColorModules.silver;
            blackSilkColor = new FlagColorModules.NormalColorModule(Color.black, Helper.GetRGBColor(5, 6, 7), Helper.GetRGBColor(35, 35, 44));
            edgeSilkColor = new FlagColorModules.NormalColorModule(Helper.GetRGBColor(75, 38, 188), Helper.GetRGBColor(42, 22, 75), Helper.GetRGBColor(122, 132, 221));

            #region processOuterMesh
            List<TriangleMesh.Triangle> triangles = new List<TriangleMesh.Triangle>();

            for (int x = 0; x < flag.gridX - 1; x++)
            {
                int a = flag.GetVertIndex(x, 0);
                int b = flag.GetVertIndex(x + 1, 0);
                int c = flag.GetVertIndex(x + 1, 1);
                triangles.Add(new TriangleMesh.Triangle(a, b, c));//顺时针
            }

            for (int y = 1; y < flag.gridY - 1; y++)
            {
                for (int x = 0; x < flag.gridX - 1; x++)
                {
                    int a = flag.GetVertIndex(x, y);
                    int b = flag.GetVertIndex(x + 1, y);
                    int c = flag.GetVertIndex(x, y - 1);
                    int d = flag.GetVertIndex(x + 1, y + 1);
                    triangles.Add(new TriangleMesh.Triangle(a, c, b));
                    triangles.Add(new TriangleMesh.Triangle(a, b, d));
                }
            }

            for (int x = 0; x < flag.gridX - 1; x++)
            {
                int a = flag.GetVertIndex(x, flag.gridY - 1);
                int b = flag.GetVertIndex(x + 1, flag.gridY - 1);
                int c = flag.GetVertIndex(x, flag.gridY - 2);
                triangles.Add(new TriangleMesh.Triangle(a, c, b));
            }
            triangleMesh = new TriangleMesh("Futile_White", triangles.ToArray(), true);
            container.AddChild(triangleMesh);
            #endregion

            #region processInnerMesh
            innerGridX = flag.gridX - innerEdge * 2;
            innerGridY = flag.gridY - innerEdge;

            List<TriangleMesh.Triangle> innerTriangles = new List<TriangleMesh.Triangle>();
            for (int x = 0; x < innerGridX - 1; x++)
            {
                int a = GetInnerVertIndex(x, 0);
                int b = GetInnerVertIndex(x + 1, 0);
                int c = GetInnerVertIndex(x + 1, 1);
                innerTriangles.Add(new TriangleMesh.Triangle(a, b, c));//顺时针
            }
            for (int y = 1; y < innerGridY - 1; y++)
            {
                for (int x = 0; x < innerGridX - 1; x++)
                {
                    int a = GetInnerVertIndex(x, y);
                    int b = GetInnerVertIndex(x + 1, y);
                    int c = GetInnerVertIndex(x, y - 1);
                    int d = GetInnerVertIndex(x + 1, y + 1);
                    innerTriangles.Add(new TriangleMesh.Triangle(a, c, b));
                    innerTriangles.Add(new TriangleMesh.Triangle(a, b, d));
                }
            }
            for (int x = 0; x < innerGridX - 1; x++)
            {
                int a = GetInnerVertIndex(x, innerGridY - 1);
                int b = GetInnerVertIndex(x + 1, innerGridY - 1);
                int c = GetInnerVertIndex(x, innerGridY - 2);
                innerTriangles.Add(new TriangleMesh.Triangle(a, c, b));
            }
            innerVerticeMapper = new int[innerGridX * innerGridY];
            for(int x = 0; x < innerGridX; x++)
            {
                for(int y = 0; y < innerGridY; y++)
                {
                    innerVerticeMapper[GetInnerVertIndex(x, y)] = flag.GetVertIndex(x + innerEdge, y);
                }
            }

            innerMesh = new TriangleMesh("Futile_White", innerTriangles.ToArray(), true);
            container.AddChild(innerMesh);

            
            #endregion
        }

        int GetInnerVertIndex(int x, int y)
        {
            return x + innerGridX * y;
        }

        public override void GrafUpdate(float timeStacker)
        {
            for(int x = 0; x < flag.gridX; x++)
            {
                for(int y = 0;y < flag.gridY; y++)
                {
                    int index = flag.GetVertIndex(x, y);
                    Vector2 pos = Vector2.Lerp(allLastPos[index], allPos[index], timeStacker);
                    Vector3 normal = Vector3.Lerp(lastNormals[index], normals[index], timeStacker);
                    float light = Mathf.Clamp01(Vector3.Dot(normal, lightDir));

                    triangleMesh.vertices[index] = pos;
                    triangleMesh.verticeColors[index] = goldenColor.GetColor(light);
                }
            }

            for(int x = 0; x < innerGridX; x++)
            {
                for(int y = 0; y < innerGridY; y++)
                {
                    int innerIndex = GetInnerVertIndex(x, y);
                    int index = innerVerticeMapper[innerIndex];
                    Vector2 pos = Vector2.Lerp(allLastPos[index], allPos[index], timeStacker);
                    Vector3 normal = Vector3.Lerp(lastNormals[index], normals[index], timeStacker);
                    float light = Mathf.Clamp01(Vector3.Dot(normal, lightDir));

                    innerMesh.vertices[innerIndex] = pos;
                    innerMesh.verticeColors[innerIndex] = blackSilkColor.GetColor(light);
                }
            }

            triangleMesh.Refresh();
            innerMesh.Refresh();
        }

       
    }

    public class RandomBuffFlagRenderer : FlagRenderer
    {
        FlagType flagType;

        TriangleMesh triangleMesh;
        TriangleMesh innerMesh;

        int innerGridX, innerGridY;
        int[] innerVerticeMapper;

        FlagColorModules.ColorModule outerModule;
        FlagColorModules.ColorModule innerModule;

        public Vector3 lightDir;

        public bool customAlpha;
        public float lastAlpha;
        public float alpha;
        float setAlpha;
        public bool Show { get => setAlpha == 1f; set => setAlpha = value ? 1f : 0f; }
        public bool NeedRenderUpdate => setAlpha != alpha;

        public RandomBuffFlagRenderer(RandomBuffFlag flag, FlagType flagType, FlagColorType flagColorType) : base(flag)
        {
            this.flagType = flagType;

            lightDir = new Vector3(-1f, -1f, -1f).normalized;
            //innerModule = new FlagColorModules.NormalColorModule(Color.black, Helper.GetRGBColor(5, 6, 7), Helper.GetRGBColor(35, 35, 44));

            triangleMesh = new TriangleMesh("Futile_White", GetTriangles(flag.gridX, flag.gridY, flag.GetVertIndex).ToArray(), true);
            container.AddChild(triangleMesh);

            innerGridX = flag.gridX - 2;
            innerGridY = flag.gridY - 1;

            innerMesh = new TriangleMesh("Futile_White", GetTriangles(innerGridX, innerGridY, GetInnerVertIndex).ToArray(), true);
            container.AddChild(innerMesh);
            innerVerticeMapper = new int[innerGridX * innerGridY];
            for (int x = 0; x < innerGridX; x++)
            {
                for (int y = 0; y < innerGridY; y++)
                {
                    innerVerticeMapper[GetInnerVertIndex(x, y)] = flag.GetVertIndex(x + 1, y);
                }
            }
            SetupColorModule(flagColorType);
        }

        void SetupColorModule(FlagColorType flagColorType)
        {
            switch(flagColorType)
            {
                case FlagColorType.Silver:
                    outerModule = FlagColorModules.silver;
                    innerModule = FlagColorModules.blackSilk;
                    break;
                case FlagColorType.Golden:
                    outerModule = FlagColorModules.golden;
                    innerModule = FlagColorModules.blackSilk;
                    break;
                case FlagColorType.Grey:
                    outerModule = FlagColorModules.whiteSilk;
                    innerModule = FlagColorModules.blackSilk;
                    break;
            }
        }

        public override void Update()
        {
            base.Update();

            lastAlpha = alpha;
            if(alpha != setAlpha && !customAlpha)
            {
                alpha = Mathf.Lerp(alpha, setAlpha, 0.15f);
                if(Mathf.Approximately(alpha, setAlpha))
                    alpha = setAlpha;
            }
        }

        protected override Vector2 Get2DPos(Vector3 pos, int x, int y)
        {
            switch (flagType)
            {
                case FlagType.InnerTriangle:
                    Vector2 orig = new Vector2(pos.x * flag.scaleX, pos.y * flag.scaleY * Mathf.Lerp(0.8f, 1f, Mathf.Abs(x - flag.gridX / 2f) / (flag.gridX / 2f)));
                    Vector2 dir = orig - flagCenter;
                    return orig + dir * pos.z / 100f + this.pos;

                case FlagType.OuterTriangle:
                    orig = new Vector2(pos.x * flag.scaleX, pos.y * flag.scaleY * Mathf.Lerp(1f, 0.8f, Mathf.Abs(x - flag.gridX / 2f) / (flag.gridX / 2f)));
                    dir = orig - flagCenter;
                    return orig + dir * pos.z / 100f + this.pos;

                case FlagType.Square:
                default:
                    return base.Get2DPos(pos, x, y);
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            float smoothAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            for (int x = 0; x < flag.gridX; x++)
            {
                for (int y = 0; y < flag.gridY; y++)
                {
                    int index = flag.GetVertIndex(x, y);
                    Vector2 pos = Vector2.Lerp(allLastPos[index], allPos[index], timeStacker);
                    Vector3 normal = Vector3.Lerp(lastNormals[index], normals[index], timeStacker);
                    float light = Mathf.Clamp01(Vector3.Dot(normal, lightDir));

                    triangleMesh.vertices[index] = pos;
                    triangleMesh.verticeColors[index] = outerModule.GetColor(light);
                    triangleMesh.verticeColors[index].a = smoothAlpha;
                }
            }

            for (int x = 0; x < innerGridX; x++)
            {
                for (int y = 0; y < innerGridY; y++)
                {
                    int innerIndex = GetInnerVertIndex(x, y);
                    int index = innerVerticeMapper[innerIndex];
                    Vector2 pos = Vector2.Lerp(allLastPos[index], allPos[index], timeStacker);
                    Vector3 normal = Vector3.Lerp(lastNormals[index], normals[index], timeStacker);
                    float light = Mathf.Clamp01(Vector3.Dot(normal, lightDir));

                    innerMesh.vertices[innerIndex] = pos;
                    innerMesh.verticeColors[innerIndex] = innerModule.GetColor(light);
                    innerMesh.verticeColors[innerIndex].a = smoothAlpha;
                }
            }

            triangleMesh.Refresh();
            innerMesh.Refresh();
        }

        List<TriangleMesh.Triangle> GetTriangles(int gridX, int gridY, Func<int, int, int> GetVertIndex)
        {
            List<TriangleMesh.Triangle> triangles = new List<TriangleMesh.Triangle>();

            for (int x = 0; x < gridX - 1; x++)
            {
                int a = GetVertIndex(x, 0);
                int b = GetVertIndex(x + 1, 0);
                int c = GetVertIndex(x + 1, 1);
                triangles.Add(new TriangleMesh.Triangle(a, b, c));//顺时针
            }

            for (int y = 1; y < gridY - 1; y++)
            {
                for (int x = 0; x < gridX - 1; x++)
                {
                    int a = GetVertIndex(x, y);
                    int b = GetVertIndex(x + 1, y);
                    int c = GetVertIndex(x, y - 1);
                    int d = GetVertIndex(x + 1, y + 1);
                    triangles.Add(new TriangleMesh.Triangle(a, c, b));
                    triangles.Add(new TriangleMesh.Triangle(a, b, d));
                }
            }

            for (int x = 0; x < gridX - 1; x++)
            {
                int a = GetVertIndex(x, gridY - 1);
                int b = GetVertIndex(x + 1, gridY - 1);
                int c = GetVertIndex(x, gridY - 2);
                triangles.Add(new TriangleMesh.Triangle(a, c, b));
            }
            return triangles;
        }

        int GetInnerVertIndex(int x, int y)
        {
            return x + innerGridX * y;
        }

        public enum FlagType
        {
            Square,
            OuterTriangle,
            InnerTriangle
        }
    
        public enum FlagColorType
        {
            Silver,
            Golden,
            Grey,
            Custom
        }
    }

    public static class FlagColorModules
    {
        public static readonly MetalColorModule golden;
        public static readonly MetalColorModule silver;

        public static readonly NormalColorModule blackSilk;
        public static readonly NormalColorModule whiteSilk;

        static FlagColorModules()
        {
            golden = new MetalColorModule(Helper.GetRGBColor(51, 38, 36), Helper.GetRGBColor(151, 71, 10), Helper.GetRGBColor(255, 251, 131), Helper.GetRGBColor(240, 240, 255));
            silver = new MetalColorModule(Helper.GetRGBColor(44, 42, 54), Helper.GetRGBColor(94, 100, 110), Helper.GetRGBColor(157, 182, 212), Helper.GetRGBColor(219, 235, 255));
            blackSilk = new NormalColorModule(Color.black, Helper.GetRGBColor(5, 6, 7), Helper.GetRGBColor(35, 35, 44));
            whiteSilk = new NormalColorModule(Helper.GetRGBColor(84, 87, 101), Helper.GetRGBColor(163, 170, 178), Helper.GetRGBColor(197, 204, 212));
        }

        public class ColorModule
        {
            public virtual Color GetColor(float light)
            {
                return Color.black;
            }
        }

        public class NormalColorModule : ColorModule
        {
            static float highLightT = 0.5f;

            public Color baseCol;
            public Color darkCol;
            public Color lightCol;

            public NormalColorModule(Color baseCol, Color darkCol, Color lightCol)
            {
                this.baseCol = baseCol;
                this.darkCol = darkCol;
                this.lightCol = lightCol;
            }

            public override Color GetColor(float light)
            {

                if (light > highLightT)
                    return Color.Lerp(baseCol, lightCol, (light - highLightT) / (1f - highLightT));
                else
                    return Color.Lerp(darkCol, baseCol, (light) / (highLightT - 0f));
            }
        }

        public class MetalColorModule : ColorModule
        {
            static float metalDarkT = 0.33f;
            static float metalMediumT = 0.66f;

            public Color metalDark;
            public Color metalMedium;
            public Color metalHighlight;
            public Color metalShine;

            public MetalColorModule(Color metalDark, Color metalMedium, Color metalHighlight, Color metalShine)
            {
                this.metalDark = metalDark;
                this.metalMedium = metalMedium;
                this.metalHighlight = metalHighlight;
                this.metalShine = metalShine;
            }

            public override Color GetColor(float light)
            {
                if (light < metalDarkT)
                    return Color.Lerp(metalDark, metalMedium, (light - 0f) / (metalDarkT - 0f));
                else if (light < metalMediumT)
                    return Color.Lerp(metalMedium, metalHighlight, (light - metalDarkT) / (metalMediumT - metalDarkT));
                else
                    return Color.Lerp(metalHighlight, metalShine, (light - metalMediumT) / (1f - metalMediumT));
            }
        }
    }
}
