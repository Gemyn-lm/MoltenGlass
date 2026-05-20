using Unity.Mathematics;
using UnityEngine;



public class Ray : MonoBehaviour
{
    public float3 Origin = float3.zero;
    public float3 Direction = float3.zero;
    public float3 InverseDirection = float3.zero;

    public float Speed = 1.0f;
    public float AngularSpeed = 1.0f;

    private void UpdateData()
    {
        Origin = transform.position;
        Direction = transform.forward;
        InverseDirection = 1.0f / Direction;
    }    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateData();
    }

    // Update is called once per frame
    void Update()
    {
        float horizontalMovement = (Input.GetKey(KeyCode.D) ? 1.0f : 0.0f) + (Input.GetKey(KeyCode.A) ? -1.0f : 0.0f);
        float verticalMovement = (Input.GetKey(KeyCode.W) ? 1.0f : 0.0f) + (Input.GetKey(KeyCode.S) ? -1.0f : 0.0f);
        Vector3 velocity = Time.deltaTime * Speed * new Vector3(horizontalMovement, verticalMovement, 0.0f);
        transform.position += velocity;
        float horizontalAngle = (Input.GetKey(KeyCode.UpArrow) ? -1.0f : 0.0f) + (Input.GetKey(KeyCode.DownArrow) ? 1.0f : 0.0f);
        float verticalAngle = (Input.GetKey(KeyCode.RightArrow) ? 1.0f : 0.0f) + (Input.GetKey(KeyCode.LeftArrow) ? -1.0f : 0.0f);
        transform.Rotate(Vector3.up, verticalAngle);
        transform.Rotate(Vector3.right, horizontalAngle);
        UpdateData();
    }
}
