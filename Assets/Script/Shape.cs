using UnityEngine;

public class Point : MonoBehaviour
{
    public struct PointData
    {
        public Vector3 Position;
        public Vector3 MatColor;
        public float emisive;

        public static readonly int DataSize = sizeof(float) * 7;
    }
    
    public Color color;
    public float Radius = 1.0f;

    public PointData GetPointData()
    {
        PointData result = new PointData();

        result.Position = transform.position;
        result.MatColor = new Vector3(color.r, color.g, color.b);
        result.emisive = 0f;
        
        return result;
    }
}
