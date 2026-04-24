using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using Random = UnityEngine.Random;

public class CullPoint : MonoBehaviour
{
    [System.Serializable]
    public struct Point
    {
        public Vector2 pos;
        public float size;
        public Transform tr;
        public SpriteRenderer sr;
        public Vector3 density;
    }
    
    public Gradient gradient;

    public Point[] pointCloud;
    public int pointCount;

    public GameObject pointPrefab;

    public float searchRadius;
    [Range(0f, 3f)]
    public float densitySmooth;

    public float threshold;
    public TextMeshProUGUI text;

    public enum Algo
    {
        Simple,
        Advanced
    };
    public Algo algo;

    private void Start()
    {
        pointCloud = new Point[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            Transform tr = Instantiate(pointPrefab, transform).transform;
            pointCloud[i] = new Point();
            pointCloud[i].pos = GetRandomPositionOnTorus(7, 1.8f);
            pointCloud[i].tr = tr;
            pointCloud[i].sr = pointCloud[i].tr.GetComponent<SpriteRenderer>();
            pointCloud[i].density = Vector3.zero;
        }
    }
    
    Vector3 GetRandomPositionOnTorus(float ringRadius, float wallRadius)
    {
        // Angle around the main ring
        float ringAngle = UnityEngine.Random.value * Mathf.PI * 2.0f;

        // Angle around the tube cross-section
        float tubeAngle = UnityEngine.Random.value * Mathf.PI * 2.0f;

        // Radial direction pointing outward from the ring centre at ringAngle
        Vector2 radialDir = new Vector3(Mathf.Sin(ringAngle), Mathf.Cos(ringAngle), 0.0f);

        // Unit circle in the tube cross-section plane (radialDir � Z-axis)
        Vector2 tubeOffset = Mathf.Cos(tubeAngle) * radialDir
                             + Mathf.Sin(tubeAngle) * Vector2.up;

        return radialDir * ringRadius + tubeOffset * wallRadius;
    }
    
    void Update()
    {
        switch (algo)
        {
            case Algo.Simple: ComputeDensity();
                break;
            case Algo.Advanced: ComputeBoundaryAdvanced();
                break;
            default:
                break;
        }
        int count = 0;
        for (int i = 0; i < pointCount; i++)
        {
            pointCloud[i].tr.position = pointCloud[i].pos;
            pointCloud[i].sr.color = gradient.Evaluate(pointCloud[i].density.x);
            //pointCloud[i].sr.color = gradient.Evaluate(pointCloud[i].density.magnitude);
            if(pointCloud[i].density.x > threshold)
                count++;
        }
        
        text.text = count.ToString() + " / "  + pointCount.ToString();
    }

    void ComputeDensity()
    {
        for (int i = 0; i < pointCount; i++)
        {
            float count = 0;
            Vector2 averageCenter = Vector3.zero;
            float averageDistance = 0;
            for (int j = 0; j < pointCount; j++)
            {
                if (i == j)
                    continue;
                
                float dist = Vector2.Distance(pointCloud[i].pos,pointCloud[j].pos);
                if (dist < searchRadius)
                {
                    averageCenter += pointCloud[j].pos;
                    averageDistance += dist;
                    count++;
                }
                
                
            }
            averageCenter /= (float)count;
            averageDistance /= (float)count;
            float assimetry = Vector2.Distance(averageCenter, pointCloud[i].pos);
            
            pointCloud[i].density.x = Mathf.Pow(assimetry / averageDistance, densitySmooth);
        }
    }
    
    void ComputeBoundaryAdvanced()
    {
        for (int i = 0; i < pointCount; i++)
        {
            // --- 1. Collecter les voisins ---
            List<Vector2> neighbors = new List<Vector2>();
            for (int j = 0; j < pointCount; j++)
            {
                if (i == j) continue;
                if (Vector2.Distance(pointCloud[i].pos, pointCloud[j].pos) < searchRadius)
                    neighbors.Add(pointCloud[j].pos);
            }

            if (neighbors.Count < 2)
            {
                pointCloud[i].density.x = 1f; // isolé = bord
                continue;
            }

            // --- 2. PCA locale : estimer la "normale" 2D ---
            // En 2D, la normale est perpendiculaire à la direction principale du voisinage.
            // On calcule la matrice de covariance 2x2 du nuage local.
            Vector2 p = pointCloud[i].pos;

            Vector2 mean = Vector2.zero;
            foreach (var n in neighbors) mean += n;
            mean /= neighbors.Count;

            // Matrice de covariance : cxx, cxy, cyy
            float cxx = 0, cxy = 0, cyy = 0;
            foreach (var n in neighbors)
            {
                Vector2 d = n - mean;
                cxx += d.x * d.x;
                cxy += d.x * d.y;
                cyy += d.y * d.y;
            }
            cxx /= neighbors.Count;
            cxy /= neighbors.Count;
            cyy /= neighbors.Count;

            // Valeurs propres de la matrice 2x2 [[cxx, cxy],[cxy, cyy]]
            // λ = ((cxx+cyy) ± sqrt((cxx-cyy)²+4*cxy²)) / 2
            float trace = cxx + cyy;
            float det   = cxx * cyy - cxy * cxy;
            float disc  = Mathf.Sqrt(Mathf.Max(0f, trace * trace * 0.25f - det));
            float lambda1 = trace * 0.5f + disc; // grande valeur propre  → direction principale
            float lambda2 = trace * 0.5f - disc; // petite valeur propre  → direction normale

            // Vecteur propre associé à lambda1 = tangente principale
            Vector2 tangent;
            if (Mathf.Abs(cxy) > 1e-6f)
                tangent = new Vector2(lambda1 - cyy, cxy).normalized;
            else
                tangent = cxx >= cyy ? Vector2.right : Vector2.up;

            // La normale est perpendiculaire à la tangente
            Vector2 normal = new Vector2(-tangent.y, tangent.x);

            // --- 3. Projeter les voisins sur la tangente et trier par angle ---
            // En 2D, on projette chaque voisin dans le repère (tangent, normal) centré sur p
            List<float> angles = new List<float>(neighbors.Count);
            foreach (var n in neighbors)
            {
                Vector2 d = n - p;
                float t = Vector2.Dot(d, tangent);
                float nm = Vector2.Dot(d, normal);
                angles.Add(Mathf.Atan2(nm, t)); // angle dans [-π, π]
            }

            angles.Sort();

            // --- 4. Trouver le plus grand angle vide entre deux voisins consécutifs ---
            float maxGap = 0f;
            for (int k = 1; k < angles.Count; k++)
                maxGap = Mathf.Max(maxGap, angles[k] - angles[k - 1]);

            // Ne pas oublier le gap qui "boucle" entre le dernier et le premier angle
            float wrapGap = (angles[0] + 2f * Mathf.PI) - angles[angles.Count - 1];
            maxGap = Mathf.Max(maxGap, wrapGap);

            // --- 5. Normaliser dans [0,1] et appliquer le lissage ---
            // maxGap est dans [0, 2π]. Un bord aura un gap > π/2 (90°).
            // On normalise par 2π pour avoir une valeur comparable à l'ancienne méthode.
            float score = Mathf.Pow(maxGap / (2f * Mathf.PI), densitySmooth);
            pointCloud[i].density.x = score;
        }
    }

    void CullInterior()
    {
        
    }
}
