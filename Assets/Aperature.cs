using UnityEngine;

[System.Serializable]
public class Aperature
{
    public Vector3 position;
    [Range(0, 10)] public int numPoints;
    public float radius;
    public float rotationOffset;
    public float width, height;

    private Vector2[] vertices;

    public Aperature()
    {
        CalculateVertices();
    }

    public bool Intersects(Ray ray, out float t)
    {
        t = -1.0f;
        Math.PlaneIntersect(position, new Vector3(0, 0, -1), width, height, ray.origin, ray.direction, out float te);
        if(te < 0)
        {
            return false;
        }
        t = te;

        if (!IsInsideAperature(ray.origin + ray.direction * t))
        {
            return true;
        }


        return false;
    }

    public bool IsInsideAperature(Vector2 point)
    {
        int numVertices = vertices.Length;
        bool inside = false;
        for (int i = 0, j = numVertices - 1; i < numVertices; j = i++)
        {
            if ((vertices[i].y > point.y) != (vertices[j].y > point.y) &&
                point.x < (vertices[j].x - vertices[i].x) * (point.y - vertices[i].y) / (vertices[j].y - vertices[i].y) + vertices[i].x)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    public void CalculateVertices()
    {
        vertices = new Vector2[numPoints];
        float angleStep = 360.0f / (float)numPoints * Mathf.Deg2Rad;
        for (int i = 0; i < numPoints; i++)
        {
            float angle = i * angleStep + rotationOffset;
            vertices[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius + new Vector2(position.x, position.y);
        }
    }
}
