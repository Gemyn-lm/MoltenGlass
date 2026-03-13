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
 

    void Awake()
    {
        for (int i = 0; i < sphereCount; i++)
        {
            Point sphere = Instantiate(spherePrefab, sphereParent).GetComponent<Point>();
            sphere.transform.localPosition = UnityEngine.Random.insideUnitSphere * 8f;
            sphere.color = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
            sphere.transform.localScale = new Vector3(2, 2, 2);
        }
    }

    
    void Update()
    {
        
    }
}
