using UnityEngine;

public class Point : MonoBehaviour
{
    public struct PointData
    {
        public Vector3 Position;
        public Vector3 MatColor;

        public static readonly int DataSize = sizeof(float) * 6;
    }
    
    public Color color;

    public PointData GetPointData()
    {
        PointData result = new PointData();

        result.Position = transform.position;
        result.MatColor = new Vector3(color.r, color.g, color.b);
        
        return result;
    }
}
