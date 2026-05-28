using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class PointCloud : MonoBehaviour
{
    public int sphereCount = 10;

    public RayMaster rayMaster;
    
    [SerializeField]
    private List<Point> sphereList = new List<Point>();
    public List<Point> SphereList => sphereList;
    public GameObject spherePrefab;
    public Transform sphereParent;
    public bool UseTorusDistribution = false;
    public float Radius = 8.0f;
    public float InnerRadius = 2.0f;
    public float2 MinMaxSphereRadius = new float2(0.25f, 3.0f);


    Vector3 GetRandomPositionOnTorus(float ringRadius, float wallRadius)
    {
        // Angle around the main ring
        float ringAngle = UnityEngine.Random.value * Mathf.PI * 2.0f;

        // Angle around the tube cross-section
        float tubeAngle = UnityEngine.Random.value * Mathf.PI * 2.0f;

        // Radial direction pointing outward from the ring centre at ringAngle
        Vector3 radialDir = new Vector3(Mathf.Sin(ringAngle), Mathf.Cos(ringAngle), 0.0f);

        // Unit circle in the tube cross-section plane (radialDir � Z-axis)
        Vector3 tubeOffset = Mathf.Cos(tubeAngle) * radialDir
                           + Mathf.Sin(tubeAngle) * Vector3.forward;

        return radialDir * ringRadius + tubeOffset * wallRadius;
    }

    void Awake()
    {
        for (int i = 0; i < sphereCount; i++)
        {
            Point sphere = Instantiate(spherePrefab, sphereParent).GetComponent<Point>();
            sphere.name = "Sphere " + i;
            sphereList.Add(sphere);
            sphere.transform.localPosition = UseTorusDistribution ? GetRandomPositionOnTorus(Radius, InnerRadius) : UnityEngine.Random.insideUnitSphere * Radius;
            sphere.Radius = UnityEngine.Random.Range(MinMaxSphereRadius.x, MinMaxSphereRadius.y) * 0.5f;
            sphere.transform.localScale = sphere.Radius * Vector3.one * 2.0f;
            sphere.color = new Color(0, 0, 0, 1);
        }
    }

    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Vector3 pos = GetMouseWorldPosition();
            Point sphere = Instantiate(spherePrefab, sphereParent).GetComponent<Point>();
            sphere.transform.localPosition = pos;
            sphere.transform.localScale = sphere.Radius * Vector3.one * 2.0f;
            sphere.color = new Color(0, 0, 0, 1);
            sphere.Radius = 1f;
            rayMaster.AddPoint(sphere);
        }
    }
    
    Vector3 GetMouseWorldPosition()
    {
        UnityEngine.Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    
        // Plan XY avec Z = 0 : normale = Vector3.forward, point sur le plan = Vector3.zero
        Plane xyPlane = new Plane(Vector3.forward, Vector3.zero);
    
        if (xyPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
    
        return Vector3.zero; // fallback
    }
}
