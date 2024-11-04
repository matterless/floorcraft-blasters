using UnityEngine;

namespace Matterless.Floorcraft
{
    public static class CirclePointGenerator
    {
        public static Vector3 GetRandomPointInCircle(float radius, Vector3 centerPoint = default)
        {
            float angle = Random.Range(0f, Mathf.PI * 2);
            float distance = Mathf.Sqrt(Random.Range(0f, 1f)) * radius;
            float x = centerPoint.x + Mathf.Cos(angle) * distance;
            float z = centerPoint.z + Mathf.Sin(angle) * distance;
            return new Vector3(x, centerPoint.y, z);
        }
        
        public static Vector3 GetRandomPointInDonut(float innerRadius, float outerRadius, Vector3 centerPoint = default)
        {
            float angle = Random.Range(0f, Mathf.PI * 2);
            float distance = Mathf.Sqrt(Random.Range(innerRadius / outerRadius, 1f)) * outerRadius;
            float x = centerPoint.x + Mathf.Cos(angle) * distance;
            float z = centerPoint.z + Mathf.Sin(angle) * distance;
            return new Vector3(x, centerPoint.y, z);
        }
        
        public static Vector3 GetPoint(float radius, float angle, Vector3 centerPoint = default)
        {
            float x = centerPoint.x + Mathf.Cos(angle) * radius;
            float z = centerPoint.z + Mathf.Sin(angle) * radius;
            return new Vector3(x, centerPoint.y, z);
        }
    
    }
}