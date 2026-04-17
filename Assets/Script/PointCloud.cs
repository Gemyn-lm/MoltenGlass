using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class PointCloud : MonoBehaviour
{
    public int sphereCount;
    
    [SerializeField]
    private List<Point> sphereList = new List<Point>();
    public GameObject spherePrefab;
    public Transform sphereParent;
    public bool UseTorusDistribution = false;
    public float Radius = 8.0f;
    public float InnerRadius = 2.0f;

    Vector3 GetRandomPositionOnTorus(float ringRadius, float wallRadius)
    {
        // Angle around the main ring
        float ringAngle = UnityEngine.Random.value * Mathf.PI * 2.0f;

        // Angle around the tube cross-section
        float tubeAngle = UnityEngine.Random.value * Mathf.PI * 2.0f;

        // Radial direction pointing outward from the ring centre at ringAngle
        Vector3 radialDir = new Vector3(Mathf.Sin(ringAngle), Mathf.Cos(ringAngle), 0.0f);

        // Unit circle in the tube cross-section plane (radialDir × Z-axis)
        Vector3 tubeOffset = Mathf.Cos(tubeAngle) * radialDir
                           + Mathf.Sin(tubeAngle) * Vector3.forward;

        return radialDir * ringRadius + tubeOffset * wallRadius;
    }

    void Awake()
    {
        for (int i = 0; i < sphereCount; i++)
        {
            Point sphere = Instantiate(spherePrefab, sphereParent).GetComponent<Point>();
            sphere.transform.localPosition = UseTorusDistribution ? GetRandomPositionOnTorus(Radius, InnerRadius) : UnityEngine.Random.insideUnitSphere * Radius;
            sphere.color = new Color(0, 0, 0, 1);
            sphere.transform.localScale = new Vector3(2, 2, 2);
        }
    }

    
    void Update()
    {
        
    }
}
