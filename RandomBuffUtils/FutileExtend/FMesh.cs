using System;
using System.Linq;
using UnityEngine;

namespace RandomBuffUtils.FutileExtend
{
    public class FMesh : FSprite
    {

        public FMesh(Mesh3DAsset mesh, string imageName, bool customColor, bool customNormals = false)  : base()
        {
            _mesh = mesh;
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
                var sortFacet = _mesh.facets.OrderBy((i)  => _vertices[i.a].z + _vertices[i.b].z + _vertices[i.c].z).ToArray();

                for (int i=0;i<sortFacet.Length;i++)
                {
                    var curIndex = i * 3;
                    _concatenatedMatrix.ApplyVector3FromLocalVector2(ref _renderLayer.vertices[curIndex + startVert], new Vector2(_vertices[sortFacet[i].a].x, _vertices[sortFacet[i].a].y), (_vertices[sortFacet[i].a].z + _maxMeshZ) );
                    _concatenatedMatrix.ApplyVector3FromLocalVector2(ref _renderLayer.vertices[curIndex + startVert + 1], new Vector2(_vertices[sortFacet[i].b].x, _vertices[sortFacet[i].b].y), (_vertices[sortFacet[i].b].z + _maxMeshZ) );
                    _concatenatedMatrix.ApplyVector3FromLocalVector2(ref _renderLayer.vertices[curIndex + startVert + 2], new Vector2(_vertices[sortFacet[i].c].x, _vertices[sortFacet[i].c].y), (_vertices[sortFacet[i].c].z + _maxMeshZ) );

                    _renderLayer.uvs[curIndex + startVert] = _mesh.uvs[sortFacet[i].a];
                    _renderLayer.uvs[curIndex + startVert + 1] = _mesh.uvs[sortFacet[i].b];
                    _renderLayer.uvs[curIndex + startVert + 2] = _mesh.uvs[sortFacet[i].c];

                    if (_customNormals)
                    {
                        _renderLayer._mesh.normals[curIndex + startVert] = _mesh.normals[sortFacet[i].a];
                        _renderLayer._mesh.normals[curIndex + startVert + 1] = _mesh.normals[sortFacet[i].b];
                        _renderLayer._mesh.normals[curIndex + startVert + 2] = _mesh.normals[sortFacet[i].c];
                    }

                    if (_customColor)
                    {
                        _renderLayer.colors[curIndex + startVert] = _verticeColors[sortFacet[i].a];
                        _renderLayer.colors[curIndex + startVert + 1] = _verticeColors[sortFacet[i].b];
                        _renderLayer.colors[curIndex + startVert + 2] = _verticeColors[sortFacet[i].c];
                    }
                    else
                    {
                        for (int j = 0; j < 3; j++)
                            _renderLayer.colors[curIndex + startVert + j] = _color;
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
                    i = Mathf.Max(i, facet.a + 1, facet.b + 1, facet.c + 1);

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
                    var v1 = vertices[face.a];
                    var v2 = vertices[face.b];
                    var v3 = vertices[face.c];
                    var normal = Vector3.Cross(v2 - v1, v3 - v1).normalized;
                    normals[face.a] += normal;
                    normals[face.b] += normal;
                    normals[face.c] += normal;
                }
                for (int i = 0; i < normals.Length; i++)
                    normals[i].Normalize();
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

                public TriangleFacet(int[] s)
                {
                    a = s[0];
                    b = s[1];
                    c = s[2];
                }
            }
        }

    }
}
