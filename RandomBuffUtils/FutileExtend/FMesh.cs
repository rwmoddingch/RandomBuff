using System;
using System.Linq;
using UnityEngine;

namespace RandomBuffUtils.FutileExtend
{
    public class FMesh : FSprite
    {
        public FMesh(string meshName, string imageName, bool customColor, bool customNormals = false) :
            this(MeshManager.GetMeshByName(meshName),imageName, customColor, customNormals)
        {
        }
        public FMesh(Mesh3DAsset mesh, string imageName, bool customColor, bool customNormals = false)  : base()
        {
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

            Init(FFacetType.Triangle, Futile.atlasManager.GetElementWithName(imageName), mesh.facets.Length);
            _isAlphaDirty = true;
            UpdateLocalVertices();

        }

        public override void PopulateRenderLayer()
        {
            if (_isOnStage && _firstFacetIndex != -1)
            {
                _isMeshDirty = false;
                UpdateVertices();
                int startVert = _firstFacetIndex * 3;

                if (_customNormals &&_renderLayer._mesh.normals.Length < _mesh.facets.Length * 3 + startVert)
                {
                    var tmp = new Vector3[_renderLayer._mesh.vertices.Length];
                    Array.Copy(_renderLayer._mesh.normals, tmp, _renderLayer._mesh.normals.Length);
                    _renderLayer._mesh.normals = tmp;
                }
                var sortFacet = _mesh.facets.OrderBy((i)  => _vertices[i.vertices.a].z + _vertices[i.vertices.b].z + _vertices[i.vertices.c].z).ToArray();

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
                        _renderLayer._mesh.normals[curIndex + startVert] = _mesh.normals[sortFacet[i].normals.a];
                        _renderLayer._mesh.normals[curIndex + startVert + 1] = _mesh.normals[sortFacet[i].normals.b];
                        _renderLayer._mesh.normals[curIndex + startVert + 2] = _mesh.normals[sortFacet[i].normals.c];
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
            }
            _renderLayer.HandleVertsChange();
        }


        void ReImportVert()
        {
            _vertices = new Vector3[_mesh.vertices.Length];
            _animatedVertices = new Vector3[_mesh.vertices.Length];
            Array.Copy(_mesh.vertices, _animatedVertices, _mesh.vertices.Length);
        }
        private void UpdateVertices()
        {
            _maxMeshZ = float.MinValue;
            for (int i = 0; i < _vertices.Length; i++)
            {

                var v = Vector3.Scale(_animatedVertices[i], _scale3d);
  
                v = RotateRound(v, Vector3.up, _rotation3d.x);
                v = RotateRound(v, Vector3.forward, _rotation3d.y);
                v = RotateRound(v, Vector3.right, _rotation3d.z);
                _vertices[i] = v;
                _maxMeshZ = Mathf.Max(_maxMeshZ,Mathf.Abs(v.z));
            }

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


        public Vector3 rotation3D { 

            get => _rotation3d; 

            set
            {
                if (value == _rotation3d) return;
                _isMeshDirty = true;
                _rotation3d = value;
            }
        }

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

        public bool customNormals
        {
            get => _customNormals;
            set {
                if(_customNormals == value) return;
                _customNormals = value;
                _isMeshDirty = true;
            }
        }

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

        public void MoveVertice(int index, Vector3 newPos)
        {
            _isMeshDirty = true;
            _animatedVertices[index] = newPos;
        }

        public void SetColor(int index, Color color)
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
            }

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
                    this.normals = normals;
                    this.uvs = uvs;
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
                        a = s[0];
                        b = s[1];
                        c = s[2];
                    }
                }

            }


        }

    }
}
