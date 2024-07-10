using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static RandomBuffUtils.FutileExtend.FMesh;

namespace RandomBuffUtils.FutileExtend
{
    public class FMesh : FSprite
    {

        /// <summary>
        /// FMesh构造函数
        /// </summary>
        /// <param name="meshName">模型名称</param>
        /// <param name="imageName">模型对应贴图</param>
        /// <param name="sortFacts">是否对模型面进行深度排序（保证前后覆盖效果）</param>
        /// <param name="customNormals">是否启用法线</param>
        /// <param name="customColor">是否启用顶点色</param>
        public FMesh(string meshName, string imageName, bool sortFacts = true, bool customNormals = true, bool customColor = false) :
            this(MeshManager.GetMeshByName(meshName),imageName, sortFacts, customNormals, customColor)
        {
        }

        /// <summary>
        /// FMesh构造函数
        /// </summary>
        /// <param name="mesh">模型资源</param>
        /// <param name="imageName">模型对应贴图</param>
        /// <param name="sortFacts">是否对模型面进行深度排序（保证前后覆盖效果）</param>
        /// <param name="customNormals">是否启用法线</param>
        /// <param name="customColor">是否启用顶点色</param>
        public FMesh(Mesh3DAsset mesh, string imageName, bool sortFacts = true, bool customNormals = true,
            bool customColor = false)  : base()
        {
            _sortFacts = sortFacts;
            _mesh = mesh ?? throw new FutileException("MeshAsset is Null!");

            _customNormals = customNormals;
            _customColor = customColor;
            
            ReImportVert();
            if (customColor)
            {
                _verticeColors = new Color[_vertices.Length];
                for (int k = 0; k < _verticeColors.Length; k++)
                {
                    _verticeColors[k] = _alphaColor;
                }
            }

            Init(MeshManager.Mesh, Futile.atlasManager.GetElementWithName(imageName), mesh.facets.Length);
            _isAlphaDirty = true;
            UpdateLocalVertices();

        }


        public override void PopulateRenderLayer()
        {
            if (_isOnStage && _firstFacetIndex != -1)
            {
                var meshLayer = _renderLayer as FMeshRenderLayer;
                _isMeshDirty = false;
                UpdateVertices();
                int startVert = _firstFacetIndex * 3;


                var sortFacet = _mesh.facets;
                if(_sortFacts)
                    sortFacet = _mesh.facets.OrderBy(i =>
                        -(_vertices[i.vertices.a].z + _vertices[i.vertices.b].z + _vertices[i.vertices.c].z)).ToArray();

                for (int i=0;i<sortFacet.Length;i++)
                {
                    var curIndex = i * 3;
                    _concatenatedMatrix.ApplyVector3FromLocalVector2(ref _renderLayer.vertices[curIndex + startVert], 
                        new Vector2(_vertices[sortFacet[i].vertices.a].x, _vertices[sortFacet[i].vertices.a].y), (_vertices[sortFacet[i].vertices.a].z + _maxMeshZ) );
                    _concatenatedMatrix.ApplyVector3FromLocalVector2(ref _renderLayer.vertices[curIndex + startVert + 1],
                        new Vector2(_vertices[sortFacet[i].vertices.b].x, _vertices[sortFacet[i].vertices.b].y), (_vertices[sortFacet[i].vertices.b].z + _maxMeshZ) );
                    _concatenatedMatrix.ApplyVector3FromLocalVector2(ref _renderLayer.vertices[curIndex + startVert + 2], 
                        new Vector2(_vertices[sortFacet[i].vertices.c].x, _vertices[sortFacet[i].vertices.c].y), (_vertices[sortFacet[i].vertices.c].z + _maxMeshZ) );

                    _renderLayer.uvs[curIndex + startVert] = _mesh.uvs[sortFacet[i].uvs.a];
                    _renderLayer.uvs[curIndex + startVert + 1] = _mesh.uvs[sortFacet[i].uvs.b];
                    _renderLayer.uvs[curIndex + startVert + 2] = _mesh.uvs[sortFacet[i].uvs.c];

                    if (_customNormals)
                    {    
                        meshLayer._normals[curIndex + startVert] = Rotate(_mesh.normals[sortFacet[i].normals.a]);
                        meshLayer._normals[curIndex + startVert + 1] = Rotate(_mesh.normals[sortFacet[i].normals.b]);
                        meshLayer._normals[curIndex + startVert + 2] = Rotate(_mesh.normals[sortFacet[i].normals.c]);


                    }

                    if (_customColor)
                    {
                        _renderLayer.colors[curIndex + startVert] = _verticeColors[sortFacet[i].vertices.a];
                        _renderLayer.colors[curIndex + startVert + 1] = _verticeColors[sortFacet[i].vertices.b];
                        _renderLayer.colors[curIndex + startVert + 2] = _verticeColors[sortFacet[i].vertices.c];
                    }
                    else
                    {
                        for (int j = 0; j < 3; j++)
                            _renderLayer.colors[curIndex + startVert + j] = _alphaColor;
                    }
                }

                meshLayer._didNormalsChange = true;
                _renderLayer.HandleVertsChange();

            }

        }


        

        void ReImportVert()
        {
            _vertices = new Vector3[_mesh.vertices.Length];
            _animatedVertices = new Vector3[_mesh.vertices.Length];
            Array.Copy(_mesh.vertices, _animatedVertices, _mesh.vertices.Length);
        }
        private void UpdateVertices()
        {
            _maxMeshZ = float.MaxValue;
            for (int i = 0; i < _vertices.Length; i++)
            {

                var v = Vector3.Scale(_animatedVertices[i], _scale3d);
  
                v = RotateRound(v, Vector3.up, _rotation3d.x);
                v = RotateRound(v, Vector3.forward, _rotation3d.y);
                v = RotateRound(v, Vector3.right, _rotation3d.z);
                _vertices[i] = v;
                _maxMeshZ = Mathf.Min(_maxMeshZ,v.z,0);
            }

            _maxMeshZ = -_maxMeshZ;
        }


        private Vector3 Rotate(Vector3 v)
        {
            v = RotateRound(v, Vector3.up, _rotation3d.x);
            v = RotateRound(v, Vector3.forward, _rotation3d.y);
            v = RotateRound(v, Vector3.right, _rotation3d.z);
            return v;
        }

        public void ResetVertices()
        {
            ReImportVert();
            _isMatrixDirty = true;
        }

        Vector3 RotateRound(Vector3 position, Vector3 axis, float angle)
        {
            return Quaternion.AngleAxis(angle, axis) * (position);
        }

        /// <summary>
        /// 模型三维旋转
        /// </summary>
        public Vector3 rotation3D { 

            get => _rotation3d; 

            set
            {
                if (value == _rotation3d) return;
                _isMeshDirty = true;
                
                _rotation3d = value;
            }
        }

        /// <summary>
        /// 模型三维旋转（四元数版）
        /// </summary>
        public Quaternion quaternion
        {
            get => Quaternion.Euler(_rotation3d);
            set => rotation3D = value.eulerAngles;
        }


        /// <summary>
        /// 模型颜色
        /// </summary>
        public override Color color { 

            get => base.color;

            set
            {
                if (base.color == value) return;
                base.color = value;
                if(_customColor)
                {
                    for(int i=0;i<_verticeColors.Length;i++)
                    {
                        _verticeColors[i] = value;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 是否启用了顶点色
        /// </summary>
        public bool customColor => _customColor;


        /// <summary>
        /// 是否启用法线
        /// </summary>
        public bool customNormals
        {
            get => _customNormals;
            set {
                if(_customNormals == value) return;
                _customNormals = value;
                _isMeshDirty = true;
            }
        }


        /// <summary>
        /// 模型资源
        /// </summary>
        public Mesh3DAsset Mesh
        {
            get => _mesh;
            set 
            {
                if(_mesh == value) return;
                _mesh = value;
                if (_numberOfFacetsNeeded != _mesh.facets.Length)
                {
                    _numberOfFacetsNeeded = _mesh.facets.Length;
                    if (_isOnStage)
                    {
                        _stage.HandleFacetsChanged();
                    }
                }
            }
        }


        /// <summary>
        /// 模型三维缩放
        /// </summary>
        public Vector3 Scale3D
        {
            get => _scale3d;
            set
            {
                if(_scale3d == value) return;
                _scale3d = value;
                _isMeshDirty = true;
            }
        }

        /// <summary>
        /// 模型面排序选项
        /// </summary>
        public bool SortFacts
        {
            get => _sortFacts;
            set
            {
                if(value ==  _sortFacts) return;
                _sortFacts = value;
                _isMeshDirty = true;
            }
        }


        /// <summary>
        /// 移动单一顶点
        /// </summary>
        /// <param name="index"></param>
        /// <param name="newPos"></param>
        public void MoveVertice(int index, Vector3 newPos)
        {
            _isMeshDirty = true;
            _animatedVertices[index] = newPos;
        }

        /// <summary>
        /// 设置顶点颜色，请确保构造函数时启用了customColor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="color"></param>
        public void SetVerticeColor(int index, Color color)
        {
            _verticeColors[index] = color;
        }

        private bool _customColor;
        private Vector3 _rotation3d;
        private Mesh3DAsset _mesh;
        private Vector3[] _vertices;
        private Vector3[] _animatedVertices;
        private Vector3 _scale3d = Vector3.one;
        private Color[] _verticeColors;
        private bool _customNormals;
        private bool _sortFacts;

        private float _maxMeshZ = 0;

        public class Mesh3DAsset
        {
            public readonly Vector3[] vertices;

            public readonly Vector2[] uvs;

            public readonly TriangleFacet[] facets;

            public readonly Vector3[] normals;

            public Mesh3DAsset(TriangleFacet[] facets)
            {
                this.facets = new TriangleFacet[facets.Length];
                Array.Copy(facets,this.facets,facets.Length);
                int i = 0;
                foreach(var facet in facets)
                    i = Mathf.Max(i, facet.vertices.a + 1, facet.vertices.b + 1, facet.vertices.c + 1);

                vertices = new Vector3[i];
                uvs = new Vector2[i];
                normals = new Vector3[i];
            }


            public Mesh3DAsset(Vector3[] vertices, Vector2[] uvs, TriangleFacet[] facets, Vector3[] normals)
            {
                this.vertices = vertices;
                this.uvs = uvs;
                this.facets = facets;
                this.normals = normals;
                if (normals.Length == 0)
                {
                    this.normals = new Vector3[vertices.Length];
                    BuildDefaultNormals();
                }

            }

            /// <summary>
            /// 根据顶点位置信息构造默认法线
            /// </summary>
            public void BuildDefaultNormals()
            {
                
                for (int i = 0; i < normals.Length; i++)
                    normals[i] = Vector3.zero;
                foreach (var face in facets)
                {
                    var v1 = vertices[face.vertices.a];
                    var v2 = vertices[face.vertices.b];
                    var v3 = vertices[face.vertices.c];
                    var normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
                    normals[face.vertices.a] += normal;
                    normals[face.vertices.b] += normal;
                    normals[face.vertices.c] += normal;
                }
                for (int i = 0; i < normals.Length; i++)
                    normals[i].Normalize();
            }

            public struct TriangleFacet
            {
                public TriangleArray vertices;
                public TriangleArray normals;
                public TriangleArray uvs;



                public TriangleFacet(TriangleArray vertices, TriangleArray uvs, TriangleArray normals)
                {
                    this.vertices = vertices;
                    this.uvs = uvs;
                    this.normals = normals;

                }


                public struct TriangleArray
                {
                    public int a;
                    public int b;
                    public int c;

                    public TriangleArray(int a, int b, int c)
                    {
                        this.a = a; this.b = b; this.c = c;
                    }

                    public TriangleArray(int[] s,int offset = 0)
                    {
                        a = s[offset + 0];
                        b = s[offset + 1];
                        c = s[offset + 2];
                    }
                }

            }


        }

    }

    internal class FMeshRenderLayer : FFacetRenderLayer
    {

        public Vector3[] _normals = Array.Empty<Vector3>();

        public bool _didNormalsChange = false;

        public FMeshRenderLayer(FStage stage, FFacetType facetType, FAtlas atlas, FShader shader) : base(stage, facetType, atlas, shader)
        {
        }

        public override void FillUnusedFacetsWithZeroes()
        {
            _lowestZeroIndex = Math.Max(_nextAvailableFacetIndex, Math.Min(_maxFacetCount, _lowestZeroIndex));
            for (int i = _nextAvailableFacetIndex; i < _lowestZeroIndex; i++)
            {
                int num = i * 3;
                _vertices[num].Set(50f, 0f, 1000000f);
                _vertices[num + 1].Set(50f, 0f, 1000000f);
                _vertices[num + 2].Set(50f, 0f, 1000000f);
            }
            _lowestZeroIndex = _nextAvailableFacetIndex;
        }

        public override void ShrinkMaxFacetLimit(int deltaDecrease)
        {
            if (deltaDecrease <= 0)
            {
                return;
            }
            _maxFacetCount = Math.Max(_facetType.initialAmount, _maxFacetCount - deltaDecrease);
            Array.Resize(ref _vertices, _maxFacetCount * 3);
            Array.Resize(ref _uvs, _maxFacetCount * 3);
            Array.Resize(ref _colors, _maxFacetCount * 3);
            Array.Resize(ref _triangles, _maxFacetCount * 3);
            Array.Resize(ref _normals, _maxFacetCount * 3);
            _didNormalsChange = true;
            _didVertCountChange = true;
            _didVertsChange = true;
            _didUVsChange = true;
            _didColorsChange = true;
            _isMeshDirty = true;
            _doesMeshNeedClear = true;
        }

        public override void ExpandMaxFacetLimit(int deltaIncrease)
        {
            if (deltaIncrease <= 0)
            {
                return;
            }
            int maxFacetCount = _maxFacetCount;
            _maxFacetCount += deltaIncrease;
            Array.Resize(ref _vertices, _maxFacetCount * 3);
            Array.Resize(ref _uvs, _maxFacetCount * 3);
            Array.Resize(ref _colors, _maxFacetCount * 3);
            Array.Resize(ref _triangles, _maxFacetCount * 3);
            Array.Resize(ref _normals, _maxFacetCount * 3);

            for (int i = maxFacetCount; i < _maxFacetCount; i++)
            {
                int num = i * 3;
                _triangles[num] = num;
                _triangles[num + 1] = num + 1;
                _triangles[num + 2] = num + 2;
            }
            _didNormalsChange = true;
            _didVertCountChange = true;
            _didVertsChange = true;
            _didUVsChange = true;
            _didColorsChange = true;
            _isMeshDirty = true;
        }
    }
}
