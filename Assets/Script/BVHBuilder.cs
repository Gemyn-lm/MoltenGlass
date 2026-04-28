using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class BoundingBox
{
    public Vector3 Min = Vector3.positiveInfinity;
    public Vector3 Max = Vector3.negativeInfinity;
    public Vector3 Center => (Min + Max) * 0.5f;
    public Vector3 Size
    {
        get
        {
            Vector3 tempSize = Max - Min;
            return new Vector3(Mathf.Abs(tempSize.x), Mathf.Abs(tempSize.y), Mathf.Abs(tempSize.z));
        }
        set
        {
            Size = value;
        }
    }

    public void GrowToInclude(in Vector3 sphereCenter, in float radius = 1.0f)
    {
        Min = Vector3.Min(sphereCenter - radius * Vector3.one, Min);
        Max = Vector3.Max(sphereCenter + radius * Vector3.one, Max);
    }
}

public struct BVHResult
{
    public float ClosestDistance;
    public Point Sphere;
    public int Depth;
    public List<Node> TraversedNodes;
}

public class Node
{
    public BoundingBox Bounds = new();
    public int SphereCount = 0;
    public int Index = -1;
    public List<Point> spheres = new();
    public Node ChildA = null;
    public Node ChildB = null;
    public int Depth = 0;
}

[Flags]
public enum EDebugFlags
{
    BoundingBox = 1 << 0,
    TargetSphere = 1 << 1
}

public class BVHBuilder : MonoBehaviour
{
    public float RayMaxDistance = 20.0f;
    [Range(0, 32)]
    public int TargetDepth = 0;
    [Range(0, 32)]
    public int MaxDepth = 14;
    public Material HitMaterial = null;
    public EDebugFlags debugFlags = 0;
    public bool UseBVH = true;
    private List<Point> sphereList = new();
    private Node root = null;
    private Ray ray = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ray = FindFirstObjectByType<Ray>();
        PointCloud pointCloud = FindFirstObjectByType<PointCloud>();

        foreach (Point point in pointCloud.SphereList)
        {
            sphereList.Add(point);
        }
        if(UseBVH)
            ComputeBVH();
    }

    private void Split(Node parent, in int depth = 0)
    {
        if (depth >= MaxDepth)
            return;

        Vector3 size = parent.Bounds.Size;
        int splitAxis = size.x > Mathf.Max(size.y, size.z) ? 0 : size.y > size.z ? 1 : 2;
        float splitPos = parent.Bounds.Center[splitAxis];

        parent.ChildA = new();
        parent.ChildA.Depth = depth + 1;
        parent.ChildB = new();
        parent.ChildB.Depth = depth + 1;

        foreach (Point sphere in parent.spheres)
        {
            bool inA = sphere.transform.position[splitAxis] < splitPos;
            Node child = inA ? parent.ChildA : parent.ChildB;
            child.Bounds.GrowToInclude(sphere.transform.position, sphere.Radius);
            child.spheres.Add(sphere);
        }

        Split(parent.ChildA, depth + 1);
        Split(parent.ChildB, depth + 1);
    }

    void ComputeBVH()
    {
        BoundingBox bounds = new();
        foreach (Point sphere in sphereList)
        {
            bounds.GrowToInclude(sphere.transform.position, sphere.Radius);
        }

        root = new Node() { Bounds = bounds, spheres = sphereList };
        Split(root);
    }

    ulong frameCount = 0L;
    double totalTime = 0D;

    // Update is called once per frame
    void Update()
    {
        ++frameCount;
        totalTime += Time.deltaTime;
        // ComputeBVH();
    }

    private void OnDestroy()
    {
        print("Average frame time: " + totalTime / frameCount * 1e3D + "ms");
    }

    float RayBoundingBoxDst(in Ray ray, in BoundingBox box)
    {
        Vector3 tMin = (float3)(box.Min - ray.Origin) * ray.InverseDirection;
        Vector3 tMax = (float3)(box.Max - ray.Origin) * ray.InverseDirection;
        Vector3 t1 = Vector3.Min(tMin, tMax);
        Vector3 t2 = Vector3.Max(tMin, tMax);
        float tNear = Mathf.Max(Mathf.Max(t1.x, t1.y), t1.z);
        float tFar = Mathf.Min(Mathf.Min(t2.x, t2.y), t2.z);

        bool hit = tFar >= tNear && tFar > 0.0f;
        float dst = hit ? tNear > 0.0f ? tNear : 0.0f : float.PositiveInfinity;
        return dst;
    }

    float RaySphere(in Ray ray, in Point sphere)
    {
        float dst = float.PositiveInfinity;

        float3 offsetRayOrigin = ray.Origin - sphere.transform.position;
        // From the equation: sqrLength(rayOrigin + rayDir * dst) = radius^2
        // Solving for dst results in a quadratic equation with coefficients:
        float a = Vector3.Dot(ray.Direction, ray.Direction); // a = 1 (assuming unit vector)
        float b = 2.0f * Vector3.Dot(offsetRayOrigin, ray.Direction);
        float c = Vector3.Dot(offsetRayOrigin, offsetRayOrigin) - sphere.Radius * sphere.Radius;
        // Quadratic discriminant
        float discriminant = b * b - 4.0f * a * c;

        // No solution when d < 0 (ray misses sphere)
        if (discriminant >= 0.0f)
        {
            float s = Mathf.Sqrt(discriminant);
            // Distance to nearest intersection point (from quadratic formula)
            float dstNear = Mathf.Max(0.0f, (-b - s) / (2.0f * a));
            float dstFar = (-b + s) / (2.0f * a);

            // Ignore intersections that occur behind the ray
            if (dstFar >= 0.0f)
            {
                bool isInside = dstNear == 0.0f;
                dst = isInside ? dstFar : dstNear;
            }
        }

        return dst;
    }

    private BVHResult GetClosestSphereInListFromRay(in Ray ray, BVHResult state, in List<Point> spheres)
    {
        foreach (Point sphere in spheres)
        {
            float distToSphere = RaySphere(ray, sphere);
            if (distToSphere < state.ClosestDistance)
            {
                state.ClosestDistance = distToSphere;
                state.Sphere = sphere;
            }
        }
        return state;
    }

    private BVHResult RaySphereTestBVH(Node node, Ray ray, BVHResult state, int depth = 0)
    {
        state.TraversedNodes.Add(node);
        float dist = RayBoundingBoxDst(ray, node.Bounds);
        if(float.IsInfinity(dist) == false)
        {
            state.Depth = depth;
            if (node.ChildA == null && node.ChildB == null)
            {
                state = GetClosestSphereInListFromRay(ray, state, node.spheres);
            }
            else
            {
                state = RaySphereTestBVH(node.ChildA, ray, state, depth + 1);
                state = RaySphereTestBVH(node.ChildB, ray, state, depth + 1);
            }
        }

        return state;
    }

    private void DrawNodes(Node node, int depth = 0)
    {
        if (node == null || depth > TargetDepth)
            return;

        Color color = Color.HSVToRGB(depth / 6.0f % 1.0f, 1.0f, 1.0f);
        if (depth < TargetDepth)
            color.a = 0.15f;
        Gizmos.color = color;
        Gizmos.DrawWireCube(node.Bounds.Center, node.Bounds.Size);

        DrawNodes(node.ChildA, depth + 1);
        DrawNodes(node.ChildB, depth + 1);
    }

    void DrawSingleNode(Node node)
    {
        if (node == null || node.Depth > TargetDepth)
            return;

        Color color = Color.HSVToRGB(node.Depth / 6.0f % 1.0f, 1.0f, 1.0f);
        if (node.Depth < TargetDepth)
            color.a = 0.15f;
        Gizmos.color = color;
        Gizmos.DrawWireCube(node.Bounds.Center, node.Bounds.Size);
    }

    MeshRenderer lastSphereRenderer = null;
    Material lastMaterial = null;

    private void UpdateClosestSphere(Point closestSphere)
    {
        MeshRenderer sphereRenderer = closestSphere.GetComponent<MeshRenderer>();
        if (lastSphereRenderer != sphereRenderer)
        {
            if (lastMaterial != null && lastSphereRenderer != null)
                lastSphereRenderer.sharedMaterial = lastMaterial;
            lastMaterial = sphereRenderer.sharedMaterial;
            sphereRenderer.sharedMaterial = HitMaterial;
            lastSphereRenderer = sphereRenderer;
        }
    }

    private void ClearLastRenderer()
    {
        if (lastSphereRenderer != null)
        {

            lastSphereRenderer.sharedMaterial = lastMaterial;
            lastMaterial = null;
            lastSphereRenderer = null;
        }
    }

    private void DrawSphereIfDedugActive(in BVHResult result)
    {
        if (debugFlags.HasFlag(EDebugFlags.TargetSphere) == false)
            return;

        if (float.IsInfinity(result.ClosestDistance) == false && result.Sphere != null)
        {
            UpdateClosestSphere(result.Sphere);
        }
        else
        {
            ClearLastRenderer();
        }
    }

    private void OnDrawGizmos()
    {
        // DrawNodes(root);
        if (ray == null)
            return;
        Debug.DrawRay(ray.Origin, ray.Direction * RayMaxDistance, Color.aliceBlue);
        BVHResult result = new BVHResult() { ClosestDistance = float.PositiveInfinity, TraversedNodes = new() };
        if (UseBVH == true)
        {
            result = RaySphereTestBVH(root, ray, result);
            if (debugFlags.HasFlag(EDebugFlags.BoundingBox))
            {
                foreach (Node node in result.TraversedNodes)
                    DrawSingleNode(node);
            }
            TargetDepth = result.Depth;
        }
        else
        {
            result = GetClosestSphereInListFromRay(ray, result, sphereList);
        }
        DrawSphereIfDedugActive(result);
    }
}
