using Unity.Mathematics;
using UnityEngine;



public class Ray : MonoBehaviour
{
    public Vector3 Origin = Vector3.zero;
    public Vector3 Direction = Vector3.zero;
    public Vector3 InverseDirection = Vector3.zero;

    public float Speed = 1.0f;

    private void UpdateData()
    {
        Origin = transform.position;
        Direction = transform.forward;
        InverseDirection = 1.0f / (float3)Direction;
    }    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateData();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateData();
    }
}
