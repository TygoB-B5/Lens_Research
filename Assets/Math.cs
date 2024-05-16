using UnityEngine;

public class Math
{
    // Created by chatgpt.
    public static void SphereEntry(Vector3 sphereCenter, float sphereRadius, Vector3 rayStart, Vector3 rayDirection, out float t)
    {
        Vector3 oc = rayStart - sphereCenter;
        float a = Vector3.Dot(rayDirection, rayDirection);
        float b = 2.0f * Vector3.Dot(oc, rayDirection);
        float c = Vector3.Dot(oc, oc) - (sphereRadius * sphereRadius);
        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
        {
            t = -1.0f;
        }
        else
        {
            t = (-b - Mathf.Sqrt(discriminant)) / (2.0f * a);
        }
    }

    // Created by chatgpt.
    public static void SphereExit(Vector3 sphereCenter, float sphereRadius, Vector3 rayStart, Vector3 rayDirection, out float t)
    {
        Vector3 oc = rayStart - sphereCenter;
        float a = Vector3.Dot(rayDirection, rayDirection);
        float b = 2.0f * Vector3.Dot(oc, rayDirection);
        float c = Vector3.Dot(oc, oc) - (sphereRadius * sphereRadius);
        float discriminant = b * b - 4 * a * c;

        if (discriminant < 0)
        {
            t = -1.0f;
        }
        else
        {
            t = (-b + Mathf.Sqrt(discriminant)) / (2.0f * a);
        }
    }


    public static Vector3 Refract(Vector3 incidentDirection, Vector3 normal, float refractiveIndex)
    {
        float cosTheta1 = -Vector3.Dot(normal, incidentDirection);
        float sinTheta1Squared = 1 - cosTheta1 * cosTheta1;

        // Check for total internal reflection
        float criticalAngle = Mathf.Asin(1 / refractiveIndex);
        if (sinTheta1Squared > 1 / (refractiveIndex * refractiveIndex))
        {
            // Total internal reflection
            return Vector3.zero;
        }

        float sinTheta2Squared = (sinTheta1Squared * refractiveIndex * refractiveIndex);

        // Calculate the refracted direction
        Vector3 refractedDirection = (refractiveIndex * incidentDirection) + ((refractiveIndex * cosTheta1 - Mathf.Sqrt(1 - sinTheta2Squared)) * normal);

        return refractedDirection.normalized;
    }


    public static void PlaneIntersect(Vector3 planePos, Vector3 planeNorm, float width, float height, Vector3 rayStart, Vector3 rayDirection, out float t)
    {
        Vector3 ray_origin = rayStart;
        Vector3 ray_direction = rayDirection;
        Vector3 plane_point = planePos;
        Vector3 plane_normal = planeNorm;

        float denominator = Vector3.Dot(ray_direction, plane_normal);

        if (Mathf.Abs(denominator) < 1e-6)
        {
            // The ray is parallel to the plane
            t = -1;
            return;
        }

        Vector3 vector_to_plane = plane_point - ray_origin;
        float te = Vector3.Dot(vector_to_plane, plane_normal) / denominator;
        t = -1;

        Vector3 intersection_point = ray_origin + ray_direction * te;
        Vector3 local_point = intersection_point - plane_point;

        // Assuming plane normal is aligned with the z-axis
        if (Mathf.Abs(plane_normal.x) > 0.9f)
        { // Normal is aligned with x-axis
            if (Mathf.Abs(local_point.y) <= width / 2 && Mathf.Abs(local_point.z) <= height / 2)
            {
                t = te;
            }
        }
        else if (Mathf.Abs(plane_normal.y) > 0.9f)
        { // Normal is aligned with y-axis
            if (Mathf.Abs(local_point.x) <= width / 2 && Mathf.Abs(local_point.z) <= height / 2)
            {
                t = te;
            }
        }
        else
        { // Normal is aligned with z-axis
            if (Mathf.Abs(local_point.x) <= width / 2 && Mathf.Abs(local_point.y) <= height / 2)
            {
                t = te;
            }
        }
    }

    public static float FresnelReflectance(Vector3 incomingRay, Vector3 normal, float n1, float n2)
    {
        float cosI = Mathf.Clamp(Vector3.Dot(-incomingRay, normal), -1.0f, 1.0f);
        float sinT2 = (n1 / n2) * (n1 / n2) * (1.0f - cosI * cosI);

        if (sinT2 > 1.0f)
        {
            // Total internal reflection
            return 1.0f;
        }

        float cosT = Mathf.Sqrt(1.0f - sinT2);
        float rOrthogonal = ((n1 * cosI) - (n2 * cosT)) / ((n1 * cosI) + (n2 * cosT));
        float rParallel = ((n2 * cosI) - (n1 * cosT)) / ((n2 * cosI) + (n1 * cosT));

        return (rOrthogonal * rOrthogonal + rParallel * rParallel) / 2.0f;
    }
}
