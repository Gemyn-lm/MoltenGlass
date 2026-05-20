using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public struct Node
{
    public float3 Min;
    public float3 Max;
    // Sphere index if leaf count, otherwise childIndex - is leaf if sphereCount > 0
    public int Index;
    public int SphereCount;
}

[Flags]
public enum EDebugFlags
{
    BoundingBox  = 1 << 0,
    TargetSphere = 1 << 1
}

public class BVHBuilder : MonoBehaviour
{
    public float RayMaxDistance = 20.0f;
    [Range(0, 32)]
    public int TargetDepth = 0;
    [Range(0, 32)]
    public int MaxDepth = 14;
    private Ray ray = null;
    public Material HitMaterial = null;
    public EDebugFlags debugFlags = 0;
    public bool UseBVH = true;

    private Point[] spheres;
    private int nodeIndex = 0;
    private Node[] nodes = new Node[32];

    private ulong frameCount = 0L;
    private double totalTime = 0D;

    private bool hasHitThisFrame = false;
    private int sphereIndexThisFrame = -1;
    private float distToSphereThisFrame = 0.0f;
    private readonly List<int> nodeIndicesTraversed = new();
    private MeshRenderer lastSphereRenderer = null;
    private Material lastMaterial = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ray = FindFirstObjectByType<Ray>();
        PointCloud pointCloud = FindFirstObjectByType<PointCloud>();
        spheres = pointCloud.SphereList.ToArray();
        ComputeBVH();
    }

    // Update is called once per frame
    void Update()
    {
        ++frameCount;
        totalTime += Time.deltaTime;
        // ComputeBVH();
        (bool hit, int sphereIndex, float dist, _) = Search();
        if (debugFlags.HasFlag(EDebugFlags.TargetSphere))
        {
            hasHitThisFrame = hit;
            sphereIndexThisFrame = sphereIndex;
            distToSphereThisFrame = dist;
        }
    }

    private void OnDestroy()
    {
        print("Average frame time: " + (totalTime / frameCount * 1e3D).ToString("n2") + "ms");
    }

    private int AddNode(in Node node)
    {
        if (nodeIndex >= nodes.Length)
        {
            Array.Resize(ref nodes, nodes.Length * 2);
        }

        int curNodeIndex = nodeIndex;
        nodes[nodeIndex++] = node;
        return curNodeIndex;
    }

    private void ComputeBVH()
    {
        float3 min = float.MaxValue;
        float3 max = float.MinValue;

        for(int i = 0; i < spheres.Length; i++)
        {
            Point sphere = spheres[i];
            float3 sphereCenter = sphere.transform.position;
            float3 minSphere = sphereCenter - sphere.Radius;
            float3 maxSphere = sphereCenter + sphere.Radius;
            min.x = minSphere.x < min.x ? minSphere.x : min.x;
            min.y = minSphere.y < min.y ? minSphere.y : min.y;
            min.z = minSphere.z < min.z ? minSphere.z : min.z;
            max.x = maxSphere.x > max.x ? maxSphere.x : max.x;
            max.y = maxSphere.y > max.y ? maxSphere.y : max.y;
            max.z = maxSphere.z > max.z ? maxSphere.z : max.z;
        }
        AddNode(new Node() { Min = min, Max = max, SphereCount = -1, Index = -1 });
        if(UseBVH)
        {
            Split(0, 0, spheres.Length);
        }
        else
        {
            nodes[0].Index = 0;
            nodes[0].SphereCount = spheres.Length;
        }
    }
    private void Split(in int parentIndex, in int startIndex, in int sphereCount, in int depth = 0)
    {
    
        ref Node parent = ref nodes[parentIndex];
        float parentCost = NodeCost(parent.Max - parent.Min, sphereCount);

        (int splitAxis, float splitPos, float cost) = ChooseSplit(parent, startIndex, sphereCount);

        if (cost < parentCost && depth < MaxDepth)
        {
            float3 minLeft = float.MaxValue;
            float3 maxLeft = float.MinValue;

            float3 minRight = float.MaxValue;
            float3 maxRight = float.MinValue;
            int numOnLeft = 0;

            for (int i = startIndex; i < startIndex + sphereCount; i++)
            {
                Point sphere = spheres[i];

                float c = sphere.transform.position[splitAxis];
                float3 sphereCenter = sphere.transform.position;
                float3 sphereMin = sphereCenter - sphere.Radius;
                float3 sphereMax = sphereCenter + sphere.Radius;

                if (c < splitPos)
                {
                    if (sphereMin.x < minLeft.x) minLeft.x = sphereMin.x;
                    if (sphereMin.y < minLeft.y) minLeft.y = sphereMin.y;
                    if (sphereMin.z < minLeft.z) minLeft.z = sphereMin.z;
                    if (sphereMax.x > maxLeft.x) maxLeft.x = sphereMax.x;
                    if (sphereMax.y > maxLeft.y) maxLeft.y = sphereMax.y;
                    if (sphereMax.z > maxLeft.z) maxLeft.z = sphereMax.z;

                    Point swap = spheres[startIndex + numOnLeft];
                    spheres[startIndex + numOnLeft] = sphere;
                    spheres[i] = swap;
                    numOnLeft++;
                }
                else
                {
                    if (sphereMin.x < minRight.x) minRight.x = sphereMin.x;
                    if (sphereMin.y < minRight.y) minRight.y = sphereMin.y;
                    if (sphereMin.z < minRight.z) minRight.z = sphereMin.z;
                    if (sphereMax.x > maxRight.x) maxRight.x = sphereMax.x;
                    if (sphereMax.y > maxRight.y) maxRight.y = sphereMax.y;
                    if (sphereMax.z > maxRight.z) maxRight.z = sphereMax.z;
                }
            }

            int numOnRight = sphereCount - numOnLeft;
            int sphereStartLeft = startIndex + 0;
            int sphereStartRight = startIndex + numOnLeft;

            // Split parent into two children
            Node childLeft = new(){ Min = minLeft, Max = maxLeft, Index = sphereStartLeft, SphereCount = 0 };
            Node childRight = new() { Min = minRight, Max = maxRight, Index = sphereStartRight, SphereCount = 0 };
            int childIndexLeft = AddNode(childLeft);
            int childIndexRight = AddNode(childRight);

            // Update parent
            parent.Index = childIndexLeft;
            nodes[parentIndex] = parent;

            // Recursively split children
            Split(childIndexLeft, startIndex, numOnLeft, depth + 1);
            Split(childIndexRight, startIndex + numOnLeft, numOnRight, depth + 1);
        }
        else
        {
            // Parent is actually leaf, assign all triangles to it
            parent.Index = startIndex;
            parent.SphereCount = sphereCount;
            nodes[parentIndex] = parent;
        }
    
    }


    private float NodeCost(in float3 size, in int sphereCount)
    {
        if (sphereCount == 0)
            return 0.0f;
        return (size.x * size.y + size.x * size.z + size.y * size.z) * sphereCount;
    }

    private (int axis, float pos, float cost) ChooseSplit(Node node, int start, int count)
    {
        if (count <= 1) return (0, 0, float.PositiveInfinity);

        float3 size = node.Max - node.Min;

        int largestAxisIndex = size.x > size.y && size.x > size.z ? 0 : size.y > size.z ? 1 : 2;
        float pos = largestAxisIndex switch
        {
            0 => node.Min.x + size.x * 0.5f,
            1 => node.Min.y + size.y * 0.5f,
            _ => node.Min.z + size.z * 0.5f
        };

        return (largestAxisIndex, pos, EvaluateSplit(largestAxisIndex, pos, start, count));
    }

    float EvaluateSplit(int splitAxis, float splitPos, int start, int count)
    {
        int numOnLeft = 0;
        int numOnRight = 0;

        float3 minLeft = float.MaxValue;
        float3 maxLeft = float.MinValue;
        float3 minRight = float.MaxValue;
        float3 maxRight = float.MinValue;

        int end = start + count;

        for (int i = start; i < end; i++)
        {
            ref Point sphere = ref spheres[i];
            float c = sphere.transform.position[splitAxis];
            float3 radius3 = sphere.Radius;
            float3 sphereCenter = sphere.transform.position;
            float3 minSphere = sphereCenter - radius3;
            float3 maxSphere = sphereCenter + radius3;

            if (c < splitPos)
            {
                if (minSphere.x < minLeft.x) minLeft.x = minSphere.x;
                if (minSphere.y < minLeft.y) minLeft.y = minSphere.y;
                if (minSphere.z < minLeft.z) minLeft.z = minSphere.z;
                if (maxSphere.x > maxLeft.x) maxLeft.x = maxSphere.x;
                if (maxSphere.y > maxLeft.y) maxLeft.y = maxSphere.y;
                if (maxSphere.z > maxLeft.z) maxLeft.z = maxSphere.z;

                numOnLeft++;
            }
            else
            {
                if (minSphere.x < minRight.x) minRight.x = minSphere.x;
                if (minSphere.y < minRight.y) minRight.y = minSphere.y;
                if (minSphere.z < minRight.z) minRight.z = minSphere.z;
                if (maxSphere.x > maxRight.x) maxRight.x = maxSphere.x;
                if (maxSphere.y > maxRight.y) maxRight.y = maxSphere.y;
                if (maxSphere.z > maxRight.z) maxRight.z = maxSphere.z;
                numOnRight++;
            }
        }


        float costA = NodeCost(maxLeft - minLeft, numOnLeft);
        float costB = NodeCost(maxRight - minRight, numOnRight);
        return costA + costB;
    }

    private (bool hit, int sphereIndex, float dist, Vector3 pos) Search()
    {
        Stack<Node> stack = new();
        stack.Push(nodes[0]);

        float minDst = float.MaxValue;
        int hitSphereIndex = -1;
        Vector3 hitPoint = Vector3.zero;
        nodeIndicesTraversed.Clear();

        while (stack.Count > 0)
        {
            Node node = stack.Pop();

            if (node.SphereCount > 0)
            {
                for (int i = 0; i < node.SphereCount; i++)
                {
                    int sphereIndex = node.Index + i;
                    Point sphere = spheres[sphereIndex];
                    (bool hit, float dist) = RaySphere(ray, sphere);
                    if (hit)
                    {
                        if (dist < minDst)
                        {
                            minDst = dist;
                            hitSphereIndex = sphereIndex;
                            hitPoint = ray.Origin + ray.Direction * minDst;
                        }
                    }
                }
            }
            else
            {
                Node childA = nodes[node.Index];
                Node childB = nodes[node.Index + 1];

                float dstA = RayBoundingBox(ray, childA).dist;
                float dstB = RayBoundingBox(ray, childB).dist;

                if (dstA > dstB)
                {
                    if (dstA < minDst)
                    {
                        stack.Push(childA);
                        if (debugFlags.HasFlag(EDebugFlags.BoundingBox))
                        {
                            nodeIndicesTraversed.Add(node.Index);
                        }
                    }
                    if (dstB < minDst)
                    {
                        stack.Push(childB);
                        if (debugFlags.HasFlag(EDebugFlags.BoundingBox))
                        {
                            nodeIndicesTraversed.Add(node.Index + 1);
                        }
                    }
                }
                else
                {
                    if (dstB < minDst)
                    {
                        stack.Push(childB);
                        if (debugFlags.HasFlag(EDebugFlags.BoundingBox))
                        {
                            nodeIndicesTraversed.Add(node.Index + 1);
                        }
                    }
                    if (dstA < minDst)
                    {
                        stack.Push(childA);
                        if (debugFlags.HasFlag(EDebugFlags.BoundingBox))
                        {
                            nodeIndicesTraversed.Add(node.Index);
                        }
                    }
                }
            }
        }

        return (hitSphereIndex != -1, hitSphereIndex, minDst, hitPoint);
    }

    private static (bool hit, float dist) RayBoundingBox(in Ray ray, in Node node)
    {
        float3 t1 = (node.Min - ray.Origin) * ray.InverseDirection;
        float3 t2 = (node.Max - ray.Origin) * ray.InverseDirection;
        float3 tMin = Vector3.Min(t1, t2);
        float3 tMax = Vector3.Max(t1, t2);
        float tMinEnd = Mathf.Max(Mathf.Max(tMin.x, tMin.y), tMin.z);
        float tMaxEnd = Mathf.Min(Mathf.Min(tMax.x, tMax.y), tMax.z);

        bool hit = tMaxEnd >= tMinEnd && tMaxEnd > 0.0f;
        float dist = hit ? (tMinEnd > 0.0f ? tMinEnd : tMaxEnd) : float.MaxValue;
        return (hit, dist);
    }

    private static (bool hit, float dist) RaySphere(in Ray ray, in Point sphere)
    {
        float dist = float.MaxValue;

        float3 offsetRayOrigin = ray.Origin - (float3)sphere.transform.position;
        // From the equation: sqrLength(rayOrigin + rayDir * dst) = radius^2
        // Solving for dst results in a quadratic equation with coefficients:
        float a = Vector3.Dot(ray.Direction, ray.Direction); // a = 1 (assuming unit vector)
        float b = 2.0f * Vector3.Dot(offsetRayOrigin, ray.Direction);
        float c = Vector3.Dot(offsetRayOrigin, offsetRayOrigin) - sphere.Radius * sphere.Radius;
        // Quadratic discriminant
        float discriminant = b * b - 4.0f * a * c;

        bool hit = discriminant >= 0.0f;
        // No solution when d < 0 (ray misses sphere)
        if (hit)
        {
            float s = Mathf.Sqrt(discriminant);
            // Distance to nearest intersection point (from quadratic formula)
            float dstNear = Mathf.Max(0.0f, (-b - s) / (2.0f * a));
            float dstFar = (-b + s) / (2.0f * a);

            // Ignore intersections that occur behind the ray
            if (dstFar >= 0.0f)
            {
                bool isInside = dstNear == 0.0f;
                dist = isInside ? dstFar : dstNear;
            }
        }
        return (hit, dist);
    }

    static private void DrawNode(in Node node, in int depth = 0, in int targetDepth = 0)
    {
        if (depth > targetDepth)
            return;

        Color color = Color.HSVToRGB(depth / 6.0f % 1.0f, 1.0f, 1.0f);
        if (depth < targetDepth)
            color.a = 0.15f;
        Gizmos.color = color;
        float3 nodeSize = node.Max - node.Min;
        float3 nodePos = node.Min + nodeSize / 2.0f;
        Gizmos.DrawWireCube(nodePos, nodeSize);
    }

    private void DrawNodes()
    {
        if (debugFlags.HasFlag(EDebugFlags.BoundingBox) == false)
            return;

        Stack<Node> stack = new();
        Stack<int> depthStack = new();
        stack.Push(nodes[0]);
        depthStack.Push(0);

        while (stack.Count > 0)
        {
            Node node = stack.Pop();
            int depth = depthStack.Pop();
            DrawNode(node, depth, TargetDepth);

            if (node.SphereCount <= 0)
            {
                if (nodeIndicesTraversed.Contains(node.Index))
                {
                    stack.Push(nodes[node.Index]);
                    depthStack.Push(depth + 1);
                }
                if (nodeIndicesTraversed.Contains(node.Index + 1))
                {
                    stack.Push(nodes[node.Index + 1]);
                    depthStack.Push(depth + 1);
                }
            }
        }
    }

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

    private void DrawSphere()
    {
        if (debugFlags.HasFlag(EDebugFlags.TargetSphere) == false || hasHitThisFrame == false || sphereIndexThisFrame < 0)
        {
            ClearLastRenderer();
            return;
        }

        UpdateClosestSphere(spheres[sphereIndexThisFrame]);
    }

    private void OnDrawGizmos()
    {
        DrawNodes();
        if (ray != null)
            Debug.DrawRay(ray.Origin, ray.Direction * RayMaxDistance, Color.aliceBlue);
        DrawSphere();
    }
}
