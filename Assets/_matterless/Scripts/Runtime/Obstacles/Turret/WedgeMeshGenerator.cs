using UnityEngine;
using System.Linq;

[RequireComponent(typeof(MeshFilter))]
public class WedgeMeshGenerator : MonoBehaviour
{
    /// <summary>
    /// Credit to Knight666 (knight666.com) & Rickz0r 
    /// https://gamedev.stackexchange.com/questions/31170/drawing-a-dynamic-indicator-for-a-field-of-view
    /// Code modified for modular use by ThomFoxx
    /// </summary>
    [SerializeField] private int _quality = 6;
    private Mesh _arcMesh;
    

    private float _angleOfFire;
    private float _minDist;
    private float _maxDist;
    
    [SerializeField] private Vector3[] _verts;
    [SerializeField] private int[] _tris;
    private Vector2[] _uv;

    public void MeshSetup(float minDistance, float maxDistance, float angle)
    {
        
        _arcMesh = new Mesh();
        _angleOfFire = angle;
        _arcMesh.name = "Primary Arc";
        _minDist = minDistance;
        _maxDist = maxDistance;
        
        _arcMesh.vertices = new Vector3[4 * _quality];
        _arcMesh.triangles = new int[3 * 2 * _quality];

        Vector3[] normals = new Vector3[4 * _quality];
        
        for (int i = 0; i < normals.Length; i++)
            normals[i] = new Vector3(0, 1, 0);

        
        _arcMesh.normals = normals;

        GenerateMesh();

        GetComponent<MeshFilter>().mesh = _arcMesh;
    }

    void GenerateMesh()
    {
        float angle_lookat = 0;

        float angle_start = angle_lookat - _angleOfFire;
        float angle_end = angle_lookat + _angleOfFire;
        float angle_delta = (angle_end - angle_start) / _quality;

        float angle_curr = angle_start;
        float angle_next = angle_start + angle_delta;

        Vector3 pos_curr_min = Vector3.zero;
        Vector3 pos_curr_max = Vector3.zero;

        Vector3 pos_next_min = Vector3.zero;
        Vector3 pos_next_max = Vector3.zero;

        Vector3[] vertices = new Vector3[4 * _quality];
        int[] triangles = new int[3 * 2 * _quality];

        for (int i = 0; i < _quality; i++)
        {
            Vector3 sphere_curr = new Vector3(
                Mathf.Sin(Mathf.Deg2Rad * (angle_curr)), 
                0f,
                Mathf.Cos(Mathf.Deg2Rad * (angle_curr)));

            Vector3 sphere_next = new Vector3(
                Mathf.Sin(Mathf.Deg2Rad * (angle_next)), 
                0f,
                Mathf.Cos(Mathf.Deg2Rad * (angle_next)));

            pos_curr_min = transform.position + sphere_curr * _minDist;
            pos_curr_max = transform.position + sphere_curr * _maxDist;

            pos_next_min = transform.position + sphere_next * _minDist;
            pos_next_max = transform.position + sphere_next * _maxDist;

            int a = 4 * i;
            int b = 4 * i + 1;
            int c = 4 * i + 2;
            int d = 4 * i + 3;

            vertices[a] = pos_curr_min;
            vertices[b] = pos_curr_max;
            vertices[c] = pos_next_max;
            vertices[d] = pos_next_min;            

            angle_curr += angle_delta;
            angle_next += angle_delta;

        }        
        
        _verts = vertices.Distinct().ToArray();
        _tris = new int[3 * 2 * _quality];

        CalculateTriangles();

        _uv = new Vector2[_verts.Length];
        for (int i = 0; i < _verts.Length; i++)
        {
            _uv[i]=new Vector2(_verts[i].x, _verts[i].z);
        }

        _arcMesh.Clear();        
        _arcMesh.vertices = _verts;
        _arcMesh.triangles = _tris;
        _arcMesh.uv = _uv;
        _arcMesh.RecalculateNormals();
        _arcMesh.RecalculateUVDistributionMetrics();
        //_arcMesh.OptimizeReorderVertexBuffer();
    }

    private void CalculateTriangles()
    {
        if (_minDist >0)
        {
            //takes the initial odd numbering due to Distinct Function
            _tris[0] = 0;
            _tris[1] = 1;
            _tris[2] = 3;

            _tris[3] = 1;
            _tris[4] = 2;
            _tris[5] = 3;

            _tris[6] = 2;
            _tris[7] = 5;
            _tris[8] = 3;

            _tris[9] = 4;
            _tris[10] = 5;
            _tris[11] = 2;

            for (int i = 4; i < _verts.Length - 3; i += 2)
            {
                _tris[i * 3] = i;
                _tris[i * 3 + 1] = i + 2;
                _tris[i * 3 + 2] = i + 3;
            }
            for (int i = 5; i < _verts.Length - 2; i += 2)
            {
                _tris[i * 3] = i;
                _tris[i * 3 + 1] = i - 1;
                _tris[i * 3 + 2] = i + 2;
            }
        }
        else
        {
            //still developing
        }
    }
}