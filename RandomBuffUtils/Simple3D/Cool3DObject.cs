using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using static RandomBuffUtils.Simple3D.Mesh3D;

namespace RandomBuffUtils.Simple3D
{
    public class Mesh3D
    {
        public List<Vector3> origVertices = new List<Vector3>();
        public List<Vector3> animatedVertice = new List<Vector3>();
        public List<TriangleFacet> facets = new List<TriangleFacet>();

        public Vector3 objectCenter = Vector3.zero;

        //围绕物体中心的局部旋转,实际计算在renderer中
        Vector3 _localRotaiton = Vector3.zero;
        public Vector3 localRotation
        {
            get => _localRotaiton;
            set
            {
                if(_localRotaiton != value)
                {
                    _localRotaiton = value;
                    _verticeDirty = true;
                }
            }
        }

        //绕原点的选择，实际计算在renderer中
        Vector3 _globalRotation = Vector3.zero;
        public Vector3 globalRotation
        {
            get => _globalRotation;
            set
            {
                if( _globalRotation != value)
                {
                    _globalRotation = value;
                    _verticeDirty = true;
                }
            }
        }

        //位置，实际计算在renderer中
        Vector3 _position;
        public Vector3 position
        {
            get => _position;
            set
            {
                if (_position != value)
                {
                    _position = value;
                    _verticeDirty = true;
                }
            }
        }

        //缩放，实际计算在renderer中
        float _scale = 1f;
        public float scale
        {
            get => _scale;
            set
            {
                if (_scale != value)
                {
                    _scale = value;
                    _verticeDirty = true;
                }
            }
        }

        protected bool _verticeDirty = true;
        public Action OnUpdateVertice;

        public Mesh3D()
        {
        }

        public void SetFacet(TriangleFacet[] facets)
        {
            for (int i = 0; i < facets.Length; i++)
            {
                int maxVertice = Mathf.Max(facets[i].a, facets[i].b, facets[i].c);

                while (animatedVertice.Count < maxVertice + 1)
                {
                    animatedVertice.Add(Vector3.zero);
                    origVertices.Add(Vector3.zero);
                }

                this.facets.Add(facets[i]);
            }
            BuffUtils.Log("Simple3D.Mesh3D","Total Vertices : " + origVertices.Count.ToString());
        }

        public void SetVertice(int index, Vector3 pos)
        {
            animatedVertice[index] = pos;
            origVertices[index] = pos;
        }

        public Vector3 GetOrigVertice(int index)
        {
            return origVertices[index];
        }

        public void SetAnimatedVertice(int index, Vector3 pos)
        {
            if (animatedVertice[index] != pos)
            {
                animatedVertice[index] = pos;
                _verticeDirty = true;
            }
        }

        public void Update()
        {
            if (_verticeDirty)
            {
                OnUpdateVertice?.Invoke();
                _verticeDirty = false;
            }
        }

        public void ForceDirty()
        {
            _verticeDirty = true;
        }

        public struct TriangleFacet
        {
            public int a;
            public int b;
            public int c;

            public TriangleFacet(int a, int b, int c)
            {
                this.a = a; this.b = b; this.c = c;
            }
        }
    }

    public class Mesh3DRenderer
    {
        public Mesh3D mesh;
        public string shader;

        public int startIndex;
        public int totalSprites;

        public Vector3 rotateCenter = Vector3.zero;


        public Vector3[] vertices;
        public Vector2[] uvs;

        public Color[] verticeColorInFront;
        public Color[] verticeColorInBack;

        public float maxZ;
        public float minZ;

        protected int firstConainerIndex;
        protected int lastConainerIndex;

        public bool autoCaculateZ = true;        

        public Mesh3DRenderer(Mesh3D mesh, int startIndex)
        {
            this.mesh = mesh;
            this.startIndex = startIndex;

            vertices = new Vector3[mesh.animatedVertice.Count];
            uvs = new Vector2[mesh.animatedVertice.Count];

            verticeColorInBack = new Color[mesh.animatedVertice.Count];
            verticeColorInFront = new Color[mesh.animatedVertice.Count];

            mesh.OnUpdateVertice += RecaculateVertices;
            SetUpRenderInfo();
        }

        public virtual void SetVerticeColor(Color color, bool inFront)
        {
            for (int i = 0; i < verticeColorInFront.Length; i++)
            {
                SetVerticeColor(i, color, inFront);
            }
        }

        public virtual void SetVerticeColor(int index, Color color, bool inFront)
        {
            if (inFront)
                verticeColorInFront[index] = color;
            else
                verticeColorInBack[index] = color;
        }

        public void SetUV(int index, Vector2 uv)
        {
            uvs[index] = uv;
        }

        public virtual void SetUpRenderInfo()
        {
        }

        public virtual void Update()
        {
            mesh.Update();
        }

        public virtual void RecaculateVertices()
        {
            if (autoCaculateZ)
            {
                minZ = float.MaxValue;
                maxZ = float.MinValue;
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 delta = mesh.animatedVertice[i] - mesh.objectCenter;
                delta *= mesh.scale;
                Vector3 v = mesh.objectCenter + delta;

                v = RotateRound(v, Vector3.forward, mesh.localRotation.z, mesh.objectCenter);
                v = RotateRound(v, Vector3.right, mesh.localRotation.x, mesh.objectCenter);
                v = RotateRound(v, Vector3.up, mesh.localRotation.y, mesh.objectCenter);

                Vector3 pos = mesh.position;
                pos = RotateRound(pos, Vector3.forward, mesh.globalRotation.z, Vector3.zero);
                pos = RotateRound(pos, Vector3.right, mesh.globalRotation.x, Vector3.zero);
                pos = RotateRound(pos, Vector3.up, mesh.globalRotation.y, Vector3.zero);
                v += pos;

                vertices[i] = v;

                if (autoCaculateZ)
                {
                    if (v.z < minZ)
                        minZ = v.z;
                    if (v.z > maxZ)
                        maxZ = v.z;
                }
            }
        }

        public virtual void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            mesh.ForceDirty();
        }

        public virtual void AddToContainer(RoomCamera.SpriteLeaser sLeaser, FContainer newContainer)
        {
            firstConainerIndex = int.MaxValue;
            lastConainerIndex = int.MinValue;

            for(int i = 0;i < totalSprites; i++)
            {
                int index = newContainer.GetChildIndex(sLeaser.sprites[i + startIndex]);
                if (index < firstConainerIndex)
                    firstConainerIndex = index;
                if(index > lastConainerIndex)
                    lastConainerIndex = index;
            }
        }

        public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 centerPos)
        {
        }

        public Vector3 RotateRound(Vector3 position, Vector3 axis, float angle, Vector3 center)
        {
            return Quaternion.AngleAxis(angle, axis) * (position - center) + center;
        }

        public Vector2 GetVerticeIn2D(int index)
        {
            return new Vector2(vertices[index].x, vertices[index].y);
        }

        public Color GetLerpedColor(int index)
        {
            return Color.Lerp(verticeColorInBack[index], verticeColorInFront[index], Mathf.InverseLerp(minZ, maxZ, vertices[index].z));
        }

        public enum Overlay
        {
            inFront,
            inMid,
            inBack
        }
    }

    public class Mesh3DFrameRenderer : Mesh3DRenderer
    {
        public List<LineRepresent> lineRepresents = new List<LineRepresent>();
        public float thickness;

        Vector2[][] drawVerticesBuffer;
        Color[][] colorBuffer;

        bool drawBufferDirty;

        public Mesh3DFrameRenderer(Mesh3D mesh, int startIndex, float thickness = 1f) : base(mesh, startIndex)
        {
            this.thickness = thickness;
        }

        public override void SetUpRenderInfo()
        {
            foreach (var facet in mesh.facets)
            {
                var lineA = new LineRepresent(facet.a, facet.b);
                var lineB = new LineRepresent(facet.b, facet.c);
                var lineC = new LineRepresent(facet.a, facet.c);

                if (!lineRepresents.Contains(lineA))
                    lineRepresents.Add(lineA);
                if (!lineRepresents.Contains(lineB))
                    lineRepresents.Add(lineB);
                if (!lineRepresents.Contains(lineC))
                    lineRepresents.Add(lineC);
            }

            totalSprites = lineRepresents.Count;

            drawVerticesBuffer = new Vector2[totalSprites][];
            colorBuffer = new Color[totalSprites][];

            for(int i = 0; i < drawVerticesBuffer.Length; i++)
            {
                drawVerticesBuffer[i] = new Vector2[4];
                colorBuffer[i] = new Color[2];
            }
        }

        public override void SetVerticeColor(Color color, bool inFront)
        {
            base.SetVerticeColor(color, inFront);
            drawBufferDirty = true;
        }

        public override void SetVerticeColor(int index, Color color, bool inFront)
        {
            base.SetVerticeColor(index, color, inFront);
            drawBufferDirty = true;
        }

        public override void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            for (int i = 0; i < totalSprites; i++)
            {
                sLeaser.sprites[i + startIndex] = new CustomFSprite("Futile_White");
                if (shader != string.Empty)
                {
                    sLeaser.sprites[i + startIndex].shader = rCam.game.rainWorld.Shaders[shader];
                }
            }
            drawBufferDirty = true;
        }

        public override void RecaculateVertices()
        {
            base.RecaculateVertices();
            drawBufferDirty = true;
        }

        void RecaculateDrawBuffer(RoomCamera.SpriteLeaser sLeaser)
        {
            for (int i = 0; i < lineRepresents.Count; i++)
            {
                var line = lineRepresents[i];

                Vector2 posA = GetVerticeIn2D(line.a)/* + drawCenter - camPos*/;
                Vector2 posB = GetVerticeIn2D(line.b)/* + drawCenter - camPos*/;

                Vector2 perpDir = Custom.PerpendicularVector(Custom.DirVec(posA, posB));
                

                Color newColA, newColB;
                newColA = GetLerpedColor(line.a);
                newColB = GetLerpedColor(line.b);

                //计算覆盖关系
                Overlay newOverlay;
                if (vertices[line.a].z >= 0 && vertices[line.b].z >= 0)
                    newOverlay = Overlay.inFront;
                else if (vertices[line.a].z <= 0 && vertices[line.b].z <= 0)
                    newOverlay = Overlay.inBack;
                else
                    newOverlay = Overlay.inMid;

                if (newOverlay != line.overlay)
                {
                    switch (newOverlay)
                    {
                        case Overlay.inFront:
                            MoveInContainerTo((sLeaser.sprites[i + startIndex] as CustomFSprite), lastConainerIndex);
                            break;
                        case Overlay.inBack:
                            MoveInContainerTo((sLeaser.sprites[i + startIndex] as CustomFSprite), firstConainerIndex);
                            break;
                        case Overlay.inMid:
                            for (int k = startIndex; k < startIndex + totalSprites; k++)
                            {
                                if (lineRepresents[k - startIndex].overlay == Overlay.inFront)
                                {
                                    (sLeaser.sprites[i + startIndex] as CustomFSprite).MoveBehindOtherNode((sLeaser.sprites[k]));
                                    break;
                                }
                            }
                            break;
                    }
                    line.overlay = newOverlay;
                }


                drawVerticesBuffer[i][0] = posA + perpDir * thickness / 2f;
                drawVerticesBuffer[i][1] = posA - perpDir * thickness / 2f;
                drawVerticesBuffer[i][2] = posB - perpDir * thickness / 2f;
                drawVerticesBuffer[i][3] = posB + perpDir * thickness / 2f;

                colorBuffer[i][0] = newColA;
                colorBuffer[i][1] = newColB;
            }
            drawBufferDirty = false;
        }

        void MoveInContainerTo(FNode node, int index)
        {
            var container = node.container;
            node.RemoveFromContainer();
            container.AddChildAtIndex(node, index);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 drawCenter)
        {
            if (drawBufferDirty)
                RecaculateDrawBuffer(sLeaser);

            for (int i = 0; i < lineRepresents.Count; i++)
            {
                for(int j = 0;j < 4; j++)
                {
                    (sLeaser.sprites[i + startIndex] as CustomFSprite).MoveVertice(j, drawVerticesBuffer[i][j] - camPos + drawCenter);
                }

                (sLeaser.sprites[i + startIndex] as CustomFSprite).verticeColors[0] = colorBuffer[i][0];
                (sLeaser.sprites[i + startIndex] as CustomFSprite).verticeColors[1] = colorBuffer[i][0];
                (sLeaser.sprites[i + startIndex] as CustomFSprite).verticeColors[2] = colorBuffer[i][1];
                (sLeaser.sprites[i + startIndex] as CustomFSprite).verticeColors[3] = colorBuffer[i][1];

            }
        }

        public struct LineRepresent
        {
            public int a;
            public int b;
            public Overlay overlay;

            public LineRepresent(int a, int b)
            {
                if (a < b)
                {
                    this.a = a; this.b = b;
                }
                else
                {
                    this.a = b; this.b = a;
                }
                overlay = Overlay.inFront;
            }

            public override bool Equals(object obj)
            {
                LineRepresent represent = (LineRepresent)obj;
                if (represent.a == a && represent.b == b)
                    return true;
                if (represent.a == b && represent.b == a)
                    return true;
                return false;
            }


        }
    }

    public class Mesh3DDotMatrixRenderer : Mesh3DRenderer
    {
        float scale;
        public Mesh3DDotMatrixRenderer(Mesh3D mesh, int startIndex, float scale = 5f) : base(mesh, startIndex)
        {
            this.scale = scale;
        }

        public override void SetUpRenderInfo()
        {
            totalSprites = mesh.animatedVertice.Count;
        }

        public override void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            for (int i = startIndex; i < startIndex + totalSprites; i++)
            {
                sLeaser.sprites[i] = new FSprite("pixel", true) { scale = this.scale, color = Color.green };
                if (!string.IsNullOrEmpty(shader))
                {
                    sLeaser.sprites[i].shader = rCam.game.rainWorld.Shaders[shader];
                }
            }
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 centerPos)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 pos = GetVerticeIn2D(i);
                pos += centerPos - camPos;

                sLeaser.sprites[i + startIndex].SetPosition(pos);
                sLeaser.sprites[i + startIndex].color = GetLerpedColor(i);
            }
        }
    }

    public class Mesh3DFacetRenderer : Mesh3DRenderer
    {
        bool usingLight = false;

        string _element;
        bool shouldUpdateElement;
        public string Element
        {
            get => _element;
            set
            {
                shouldUpdateElement = _element != value;
                _element = value;
            }
        }

        public List<FacetRepresent> facetRepresents;

        public Vector3 lightDir;
        public Mesh3DFacetRenderer(Mesh3D mesh, int startIndex, string element, bool usingLight = false) : base(mesh, startIndex)
        {
            _element = element;
            this.usingLight = usingLight;
        }

        public void LoadImage()
        {
            if (Futile.atlasManager.GetAtlasWithName(_element) == null)
            {
                string str = AssetManager.ResolveFilePath("Illustrations" + Path.DirectorySeparatorChar.ToString() + _element + ".png");
                Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                AssetManager.SafeWWWLoadTexture(ref texture, "file:///" + str, false, true);
                Futile.atlasManager.LoadAtlasFromTexture(_element, texture, false);
            }
        }

        public override void SetUpRenderInfo()
        {
            totalSprites = mesh.facets.Count;
            facetRepresents = new FacetRepresent[totalSprites].ToList();
            for (int i = 0; i < facetRepresents.Count; i++)
            {
                facetRepresents[i] = new FacetRepresent(mesh.facets[i]);
            }
        }

        public override void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            for (int i = 0; i < totalSprites; i++)
            {
                facetRepresents[i].linkedSpriteIndex = i + startIndex;
                sLeaser.sprites[i + startIndex] = new TriangleMesh(Element, new TriangleMesh.Triangle[1] { new TriangleMesh.Triangle(0, 1, 2) }, true, true);

                if (shader != string.Empty)
                    sLeaser.sprites[i + startIndex].shader = rCam.game.rainWorld.Shaders[shader];

                var facet = mesh.facets[i];
                (sLeaser.sprites[i + startIndex] as TriangleMesh).UVvertices[0] = uvs[facet.a];
                (sLeaser.sprites[i + startIndex] as TriangleMesh).UVvertices[1] = uvs[facet.b];
                (sLeaser.sprites[i + startIndex] as TriangleMesh).UVvertices[2] = uvs[facet.c];
            }
        }

        public override void Update()
        {
            base.Update();
            for (int i = 0; i < facetRepresents.Count; i++)
            {
                facetRepresents[i].CaculateNormalAndSort(vertices);
            }
            facetRepresents.Sort((x, y) => x.sortZ.CompareTo(y.sortZ));
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 centerPos)
        {
            for (int i = 0; i < facetRepresents.Count - 1; i++)
            {
                sLeaser.sprites[facetRepresents[i + 1].linkedSpriteIndex].MoveInFrontOfOtherNode(sLeaser.sprites[facetRepresents[i].linkedSpriteIndex]);
            }

            for (int i = 0; i < facetRepresents.Count; i++)
            {
                var represent = facetRepresents[i];
                bool culled = represent.normal.z < 0;
                int index = represent.linkedSpriteIndex;

                sLeaser.sprites[index].isVisible = true;
                (sLeaser.sprites[index] as TriangleMesh).MoveVertice(0, GetVerticeIn2D(represent.a) + centerPos - camPos);
                (sLeaser.sprites[index] as TriangleMesh).MoveVertice(1, GetVerticeIn2D(represent.b) + centerPos - camPos);
                (sLeaser.sprites[index] as TriangleMesh).MoveVertice(2, GetVerticeIn2D(represent.c) + centerPos - camPos);

                float light = Vector3.Dot(represent.normal, -lightDir);
                if (culled || !usingLight)
                    light = 0f;

                Color colA = Color.Lerp(GetLerpedColor(represent.a), Color.white, light);
                Color colB = Color.Lerp(GetLerpedColor(represent.b), Color.white, light);
                Color colC = Color.Lerp(GetLerpedColor(represent.c), Color.white, light);

                (sLeaser.sprites[index] as TriangleMesh).verticeColors[0] = colA;
                (sLeaser.sprites[index] as TriangleMesh).verticeColors[1] = colB;
                (sLeaser.sprites[index] as TriangleMesh).verticeColors[2] = colC;
            }
        }

        public class FacetRepresent
        {
            public int a; public int b; public int c;
            public int linkedSpriteIndex;

            public float sortZ;
            public Vector3 normal;
            public FacetRepresent(int a, int b, int c)
            {
                this.a = a; this.b = b; this.c = c;
                sortZ = 0f;
                normal = Vector3.zero;
            }

            public FacetRepresent(TriangleFacet copyFrom) : this(copyFrom.a, copyFrom.b, copyFrom.c)
            {
            }

            /// <summary>
            /// 法线方向默认从原点指向外部
            /// </summary>
            /// <param name="vertices"></param>
            public void CaculateNormalAndSort(Vector3[] vertices)
            {
                Vector3 A = vertices[b] - vertices[a];
                Vector3 B = vertices[c] - vertices[a];

                normal = Vector3.Cross(A, B).normalized;

                if (Vector3.Dot(vertices[a], normal) < 0f)
                    normal = -normal;

                sortZ = (vertices[a].z + vertices[b].z + vertices[c].z) / 3f;
            }
        }
    }

    public class SpecialMesh3DFrameRenderer : Mesh3DFrameRenderer
    {
        public SpecialMesh3DFrameRenderer(Mesh3D mesh, int startIndex) : base(mesh, startIndex, 1f)
        {
        }

        public override void Update()
        {
            base.Update();
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] += vertices[i].normalized * Mathf.Pow(Mathf.Sin(Time.time * 3f), 2f) * 10f;
            }
        }
    }
}
