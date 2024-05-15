using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteAlways]
public class Intersect : MonoBehaviour
{
    [Range(0.0001f, 1)] public float rayAmount;
    public bool showPrimary = true;
    public bool showReflection = true;
    public bool showRefraction = true;
    public bool showNormal = false;
    public List<Lens> lenses = new List<Lens>();
    public Enviroment enviroment;
    public Sensor sensor;


    public SpriteRenderer textureOutputShower;


    public void Update()
    {
            Texture2D tex = new Texture2D(enviroment.texture.width, enviroment.texture.height);
            RenderTexture.active = sensor.output;
            for (int x = 0; x < enviroment.texture.width; x++) for (int y = 0; y < enviroment.texture.height; y++)
            {
                    Vector3 pos = new Vector3(x, y, 0);
                    Vector3 dir = new Vector3(0, 0, 1);
                    Color col = Draw(pos, dir, 0);
                    tex.SetPixel(x, y, col);
            }
            tex.Apply();
            Graphics.Blit(tex, sensor.output);
    }


    public bool refract, reflect;

    private float epsilon = 0.001f;

    [System.Serializable]
    public struct Enviroment
    {
        public Texture2D texture;


        public Color GetSample(Vector3 I)
        {
            if (Mathf.Clamp(I.x, 0, texture.width) != I.x || Mathf.Clamp(I.y, 0, texture.height) != I.y) return Color.black;
            return texture.GetPixelBilinear(I.x / texture.width, I.y / texture.height);
        }

        public float offset;
    }

    [System.Serializable]
    public struct Sensor
    {
        public RenderTexture output;
    }

    [System.Serializable]
    public class Lens
    {
        public bool Intersects(Vector3 pos, Vector3 dir, out Vector3 I)
        {
            if (!concave)
            {
                I = Vector3.zero;
                SphereEntry(GetPosition(), curvature, pos, dir, out float t);
                if (t < 0.0f) return false;
                I = pos + dir * t;
                return true;
            }
            else
            {
                I = Vector3.zero;
                SphereExit(GetPosition(), curvature, pos, dir, out float t);
                if (t < 0.0f) return false;
                I = pos + dir * t;
                return true;
            }
        }

        public Vector3 GetPosition()
        {
            if (!concave) return new Vector3(0, 0, 1) * (offset + curvature) + defaultOffset;
            return new Vector3(0, 0, 1) * (offset - curvature) + defaultOffset;
        }

        public float curvature;
        public float offset;
        public float ior = 1.0f;
        public float ior2 = 1.0f;
        public bool concave;
        public Vector3 defaultOffset = new Vector3(64, 64, 64);
    }

    private Color Draw(Vector3 pos, Vector3 dir, int bounce)
    {
        if (bounce > 5) return Color.black;

        // Find the closest lens intersection.
        float dist = float.PositiveInfinity;
        Lens closestLens = null;
        Vector3 intersection = Vector3.zero;

        // Test for the sensor.
        float sensorDistance = float.PositiveInfinity;
        PlaneIntersect(new Vector3(0, 0, 1) * enviroment.offset, new Vector3(0, 0, -1), enviroment.texture.width * 2, enviroment.texture.height * 2, pos, dir, out float t);
        if (t > 0)
        {
            sensorDistance = t;
        }

        foreach (Lens lens in lenses)
        {
            if (lens.Intersects(pos, dir, out Vector3 I))
            {
                float newDist = Vector3.Distance(pos, I);
                if (newDist < dist)
                {
                    dist = newDist;
                    closestLens = lens;
                    intersection = I;
                }
            }
        }

        if(sensorDistance < dist)
        {
            if (showPrimary) Debug.DrawLine(pos, pos + t * dir, Color.blue);
            return enviroment.GetSample(pos + sensorDistance * dir);
        }


        // Return if there is no intersection.
        if (closestLens == null) return Color.black;

        // Process intersection.
        Vector3 normal = ((intersection - closestLens.GetPosition()) / closestLens.curvature).normalized;
        if (closestLens.concave) normal = -normal;

        Color output = Color.black;


        float fresnel = FresnelReflectance(dir, normal, closestLens.ior, closestLens.ior2);

        // Recursion
        Vector3 refractDir = Refract(dir, normal, closestLens.ior);
        if(refract) output += Draw(intersection + refractDir * epsilon, refractDir, bounce + 1) * (1.0f - fresnel);

        Vector3 reflectDir = Vector3.Reflect(dir, normal);
        if(reflect) output += Draw(intersection + reflectDir * epsilon, reflectDir, bounce + 1) * fresnel;

        if (showPrimary) Debug.DrawLine(pos, intersection, Color.magenta);
        if (showNormal) Debug.DrawRay(intersection, normal * 0.1f);
        if (showReflection) Debug.DrawLine(intersection, intersection + Vector3.Reflect(dir, normal), Color.red);
        if (showRefraction) Debug.DrawLine(intersection, intersection + Refract(dir, normal, closestLens.ior), Color.green);

        return output;
    }

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

    public float FresnelReflectance(Vector3 incomingRay, Vector3 normal, float n1, float n2)
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

