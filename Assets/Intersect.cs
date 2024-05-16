using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteAlways]
public class LensFlareRenderer : MonoBehaviour
{
    public bool showPrimary = true;
    public bool showReflection = true;
    public bool showRefraction = true;
    public bool showNormal = false;
    public List<Lens> lenses = new List<Lens>();
    public Enviroment enviroment;
    public Aperature aperature;
    public Sensor sensor;

    public void Update()
    {
        aperature.CalculateVertices();

        Texture2D tex = new Texture2D(enviroment.texture.width, enviroment.texture.height);
        RenderTexture.active = sensor.output;
        for (int x = 0; x < enviroment.texture.width; x++) for (int y = 0; y < enviroment.texture.height; y++)
            {
                Vector3 pos = new Vector3(x, y, 0);
                Vector3 dir = new Vector3(0, 0, 1);
                Color col = Draw(new Ray(pos, dir), 0);
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
        public float offset;


        public Color GetSample(Vector3 I)
        {
            if (Mathf.Clamp(I.x, 0, texture.width) != I.x || Mathf.Clamp(I.y, 0, texture.height) != I.y) return Color.black;
            return texture.GetPixelBilinear(I.x / texture.width, I.y / texture.height);
        }


        public bool Intersects(Ray ray, out float t)
        {
            Math.PlaneIntersect(new Vector3(0, 0, 1) * offset, new Vector3(0, 0, -1), texture.width * 2, texture.height * 2, ray.origin, ray.direction, out t);
            return t > 0.0f;
        }
    }

    [System.Serializable]
    public struct Sensor
    {
        public RenderTexture output;
    }


    private Color Draw(Ray ray, int bounce)
    {
        if (bounce > 5) return Color.black;

        // Find the closest lens intersection.
        float dist = float.PositiveInfinity;
        Lens closestLens = null;
        Vector3 intersection = Vector3.zero;


        float aperatureDistance = float.PositiveInfinity;
        {
            if (aperature.Intersects(ray, out float t))
            {
                aperatureDistance = t;
            }
        }

        // Test for the sensor.
        float sensorDistance = float.PositiveInfinity;
        {
            if (enviroment.Intersects(ray, out float t))
            {
                sensorDistance = t;
            }
        }

        foreach (Lens lens in lenses)
        {
            if (lens.Intersects(ray, out Vector3 I))
            {
                float newDist = Vector3.Distance(ray.origin, I);
                if (newDist < dist)
                {
                    dist = newDist;
                    closestLens = lens;
                    intersection = I;
                }
            }
        }

        // Sensor is the closest.
        if (sensorDistance < dist && sensorDistance < aperatureDistance)
        {
            if (showPrimary) Debug.DrawLine(ray.origin, ray.origin + sensorDistance * ray.direction, Color.blue);
            return enviroment.GetSample(ray.origin + sensorDistance * ray.direction);
        }

        // Aperature is the closest.
        if (aperatureDistance < dist && aperatureDistance < sensorDistance)
        {
            if (showPrimary) Debug.DrawLine(ray.origin, ray.origin + aperatureDistance * ray.direction, Color.cyan);
            return Color.black;
        }

        // Return if there is no intersection.
        if (closestLens == null && aperatureDistance == float.PositiveInfinity) return Color.black;

        // Process intersection.
        Vector3 normal = closestLens.GetNormal(intersection);

        Color output = Color.black;


        float fresnel = Math.FresnelReflectance(ray.direction, normal, closestLens.iorOut, closestLens.iorIn);

        // Recursion
        Vector3 refractDir = Math.Refract(ray.direction, normal, closestLens.ior);
        if (refract) output += Draw(new Ray(intersection + refractDir * epsilon, refractDir), bounce + 1) * (1.0f - fresnel);

        Vector3 reflectDir = Vector3.Reflect(ray.direction, normal);
        if (reflect) output += Draw(new Ray(intersection + reflectDir * epsilon, reflectDir), bounce + 1) * fresnel;

        if (showPrimary) Debug.DrawLine(ray.origin, intersection, Color.magenta);
        if (showNormal) Debug.DrawRay(intersection, normal * 0.1f);
        if (showReflection) Debug.DrawLine(intersection, intersection + reflectDir, Color.red);
        if (showRefraction) Debug.DrawLine(intersection, intersection + refractDir, Color.green);

        return output;
    }
}

