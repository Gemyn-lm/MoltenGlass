using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SphereMovement : MonoBehaviour
{
    public float period; // In seconds
    public float distance;

    public enum Axis
    {
        X,
        Y,
        Z
    }
    
    public Axis axis;

    private void Awake()
    {
        period = Random.value * 3;
        distance = Random.value * 8;
        axis = (Axis)Random.Range(0, 3);
    }

    void Update()
    {
        switch (axis)
        {
            case Axis.X:
                transform.position += new Vector3(Mathf.Sin(Time.time * period) * distance * Time.deltaTime, 0, 0);
                break;
            case Axis.Y:
                transform.position += new Vector3(0, Mathf.Sin(Time.time * period) * distance * Time.deltaTime, 0);
                break;
            case Axis.Z:
                transform.position += new Vector3(0, 0, Mathf.Sin(Time.time * period) * distance * Time.deltaTime);
                break;
            default:
                break;
        }
    }
}
