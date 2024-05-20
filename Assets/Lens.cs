using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LensSet
{
    public Hemisphere[] lenses;

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
public class Hemisphere
{
    public float ior = 1.0f;
    public float iorOut = 1.0f;
    public float iorIn = 1.0f;

    public Vector3 center;
    public float radius;
    public Vector3 direction; // Hemisphere orientation
    public float maxAngle; // Maximum angle for the concave/convex boundary


    public bool Intersects(Ray ray, out Vector3 I)
    {
        bool intersects = Intersects(ray, out float t);
        I = ray.origin + t * ray.direction;
        return intersects;
    }

    public bool Intersects(Ray ray, out float t)
    {
        t = -1.0f;
        float entryDistance, exitDistance;
        if (IntersectRayHemisphere(ray, this, out entryDistance, out exitDistance))
        {
            if (entryDistance > 0.0f && exitDistance == Mathf.Infinity)
            {
                t = entryDistance; return true;
            }
            if (exitDistance > 0.0f && entryDistance == Mathf.Infinity)
            {
                t = exitDistance; return true;
            }
        }
        return false;
    }

    public Vector3 GetPosition()
    {
        return center;
    }

    public Vector3 GetNormal(Vector3 intersection)
    {
        Vector3 normal = ((intersection - GetPosition()) / radius).normalized;
        return normal * -direction.z;
    }


    public static bool IntersectRayHemisphere(Ray ray, Hemisphere hemisphere, out float entryDistance, out float exitDistance)
    {
        entryDistance = Mathf.Infinity;
        exitDistance = Mathf.Infinity;

        Vector3 oc = ray.origin - hemisphere.center;
        float a = Vector3.Dot(ray.direction, ray.direction);
        float b = 2.0f * Vector3.Dot(oc, ray.direction);
        float c = Vector3.Dot(oc, oc) - hemisphere.radius * hemisphere.radius;
        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
        {
            // No intersection with the sphere
            return false;
        }

        // Ray intersects the sphere
        float sqrtDiscriminant = Mathf.Sqrt(discriminant);
        float t1 = (-b - sqrtDiscriminant) / (2.0f * a);
        float t2 = (-b + sqrtDiscriminant) / (2.0f * a);

        // Calculate intersection points
        Vector3 point1 = ray.origin + t1 * ray.direction;
        Vector3 point2 = ray.origin + t2 * ray.direction;

        // Check if intersection points are within the hemisphere's angle boundary
        bool point1Valid = IsPointInHemisphere(point1, hemisphere);
        bool point2Valid = IsPointInHemisphere(point2, hemisphere);

        // Determine valid entry and exit distances
        if (point1Valid && point2Valid)
        {
            entryDistance = Mathf.Min(t1, t2);
            exitDistance = Mathf.Max(t1, t2);
            return true;
        }
        else if (point1Valid)
        {
            entryDistance = t1;
            return true;
        }
        else if (point2Valid)
        {
            entryDistance = t2;
            return true;
        }

        return false;
    }

    private static bool IsPointInHemisphere(Vector3 point, Hemisphere hemisphere)
    {
        Vector3 toPoint = point - hemisphere.center;
        float angle = Vector3.Angle(hemisphere.direction, toPoint);
        return angle <= hemisphere.maxAngle;
    }
}