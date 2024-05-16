using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LensSet
{
    public Lens[] lenses;

    public List<Ray> FetchRays(Ray input, int numBounces)
    {
        if (lenses[0].Intersects(input, out float t))
        {

        }
        else
        {
            return new List<Ray>();
        }

        return null;
    }
}

[System.Serializable]
public class Lens
{
    public float curvature;
    public float ior = 1.0f;
    public float iorOut = 1.0f;
    public float iorIn = 1.0f;
    public bool concave;
    public Vector3 defaultOffset = new Vector3(64, 64, 64);

    public bool Intersects(Ray ray, out Vector3 I)
    {
        bool intersects = Intersects(ray, out float t);
        I = ray.origin + t * ray.direction;
        return intersects;
    }

    public bool Intersects(Ray ray, out float t)
    {
        if (!concave)
        {
            Math.SphereEntry(GetPosition(), curvature, ray.origin, ray.direction, out t);
            if (t < 0.0f) return false;
            return true;
        }
        else
        {
            Math.SphereExit(GetPosition(), curvature, ray.origin, ray.direction, out t);
            if (t < 0.0f) return false;
            return true;
        }
    }

    public Vector3 GetPosition()
    {
        if (!concave) return new Vector3(0, 0, 1) * (curvature) + defaultOffset;
        return new Vector3(0, 0, 1) * -curvature + defaultOffset;
    }

    public Vector3 GetNormal(Vector3 intersection)
    {
        Vector3 normal = ((intersection - GetPosition()) / curvature).normalized;
        if (concave) normal = -normal;
        return normal;
    }
}