using UnityEditor;
using UnityEngine;

public class WeightedNormalsGenerator : AssetPostprocessor
{
    private const int m_TeexcoordChannel = 4;
    private const float m_CospatialVertexDistance = 0.001f;
    
    private class CospatialVertex {
        public Vector3 position;
        public Vector3 accumulatedNormal;
    }
    private void OnPostprocessModel(GameObject gameObject)
    {
        if (!gameObject.name.Contains("Outline") || !gameObject.name.StartsWith("MD_"))
            return;
        
        Transform[] children = gameObject.GetComponentsInChildren<Transform>();
        Transform smoothNormalsTransform = null;
        foreach (Transform child in children)
        {
            if (child.name == "SmoothNormals")
            {
                smoothNormalsTransform = child;
                break;
            }
        }
        
        if (smoothNormalsTransform == null)
        {
            Debug.LogError($"GameObject {gameObject.name} doesn't have a child with name SmoothNormals");
            return;
        }
        
        MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();

        if (meshFilters.Length <= 1)
        {
            Debug.LogError($"GameObject {gameObject.name} doesn't have more than one mesh filter");
            return;
        }

        Mesh smoothedMesh = smoothNormalsTransform.gameObject.GetComponent<MeshFilter>().sharedMesh;
        Mesh mesh = meshFilters[0].sharedMesh;
        Vector3[] normals = smoothedMesh.normals;
        mesh.SetUVs(3, normals);
        
        GameObject.DestroyImmediate(smoothedMesh, true);
        GameObject.DestroyImmediate(smoothNormalsTransform.gameObject, true);
        
    }
}
