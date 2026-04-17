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
            sphere.transform.localPosition = UnityEngine.Random.insideUnitSphere * 50f;
            sphere.color = new Color(0, 0, 0, 1);
            sphere.transform.localScale = new Vector3(2, 2, 2);
        }
    }

    
    void Update()
    {
        
    }
}
