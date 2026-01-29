using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Tape : MonoBehaviour
{
    private MeshFilter meshFilter;
    private Mesh mesh;

    [Header("Tape Settings")]
    [SerializeField] private float tapeWidth = 0.4f;

    [Header("Jagged Edge Settings")]
    [SerializeField] private int segments = 10;
    [SerializeField] private float maxJaggedness = 0.1f;

    private List<Vector3> lastJaggedEdge = new List<Vector3>();

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.name = "Tape Mesh";
        meshFilter.mesh = mesh;
    }

    public List<Vector3> GetLastJaggedEdge()
    {
        return lastJaggedEdge;
    }

    public float getTapeWidth()
    {
        return tapeWidth;
    }

    public void CreateTape(Vector3 startPoint, Vector3 endPoint, List<Vector3> endJaggedEdge, List<Vector3> startJaggedEdge)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        Vector3 direction = (endPoint - startPoint).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized * tapeWidth / 2f;

        // Generate Start Edge vertices
        if (startJaggedEdge != null && startJaggedEdge.Count > 0)
        {
            vertices.AddRange(startJaggedEdge);
            for (int i = 0; i <= segments; i++)
            {
                uvs.Add(new Vector2(0, (float)i / segments));
            }
        }
        else
        {
            // Create a straight start edge
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                vertices.Add(Vector3.Lerp(startPoint - perpendicular, startPoint + perpendicular, t));
                uvs.Add(new Vector2(0, t));
            }
        }

        // Generate End Edge vertices
        lastJaggedEdge.Clear();
        foreach(Vector3 jaggedPoint in endJaggedEdge)
        {
            vertices.Add(jaggedPoint);
            lastJaggedEdge.Add(jaggedPoint);
        }

        // Make the triangles
        for (int i = 0; i < segments; i++)
        {
            int baseIndex = i;
            int nextIndex = i + 1;
            int endBaseIndex = i + segments + 1;
            int endNextIndex = i + segments + 2;

            // First triangle
            triangles.Add(baseIndex);
            triangles.Add(endNextIndex);
            triangles.Add(nextIndex);

            // Second triangle
            triangles.Add(baseIndex);
            triangles.Add(endBaseIndex);
            triangles.Add(endNextIndex);
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
    }

    public void CreateTape(float heightOfTape, List<Vector3> endJaggedEdge, List<Vector3> startJaggedEdge)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Define start and end points to create the tape's height and orientation
        Vector3 startPoint = Vector3.zero;
        Vector3 endPoint = new Vector3(0, heightOfTape, 0);

        // Generate Start Edge verticies
        if (startJaggedEdge != null && startJaggedEdge.Count > 0)
        {
            // Offset the provided startJaggedEdge vertices to the startPoint
            for (int i = 0; i < startJaggedEdge.Count; i++)
            {
                vertices.Add(startJaggedEdge[i] + startPoint);
            }
        }
        else
        {
            // Create a straight start edge if none is provided
            Vector3 perpendicular = Vector3.Cross(Vector3.up, Camera.main.transform.forward).normalized * tapeWidth / 2f;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                vertices.Add(Vector3.Lerp(startPoint - perpendicular, startPoint + perpendicular, t));
            }
        }

        // Add UVs for the start edge
        for (int i = 0; i <= segments; i++)
        {
            uvs.Add(new Vector2((float)i / segments, 0));
        }

        // Generate End Edge (Jagged) vertices
        lastJaggedEdge.Clear();
        if (endJaggedEdge != null && endJaggedEdge.Count > 0)
        {
            // Offset the provided endJaggedEdge vertices to the endPoint
            foreach (Vector3 jaggedPoint in endJaggedEdge)
            {
                Vector3 finalPoint = jaggedPoint + endPoint;
                vertices.Add(finalPoint);
                lastJaggedEdge.Add(finalPoint);
            }
        }
        else
        {
            // Create a straight end edge if none is provided
            Vector3 perpendicular = Vector3.Cross(Vector3.up, Camera.main.transform.forward).normalized * tapeWidth / 2f;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 point = Vector3.Lerp(endPoint - perpendicular, endPoint + perpendicular, t);
                vertices.Add(point);
                lastJaggedEdge.Add(point);
            }
        }

        // Add UVs for the end edge
        for (int i = 0; i <= segments; i++)
        {
            uvs.Add(new Vector2((float)i / segments, 1));
        }

        // Make the triangles
        for (int i = 0; i < segments; i++)
        {
            int baseIndex = i;
            int nextIndex = i + 1;
            int endBaseIndex = i + segments + 1;
            int endNextIndex = i + segments + 2;

            // First triangle
            triangles.Add(baseIndex);
            triangles.Add(endBaseIndex);
            triangles.Add(nextIndex);

            // Second triangle
            triangles.Add(nextIndex);
            triangles.Add(endBaseIndex);
            triangles.Add(endNextIndex);
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
    }

    public void CreateTapeRollPiece(Vector3 startPoint, Vector3 endPoint, List<Vector3> topEdge, float totalY)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        Vector3 direction = (endPoint - startPoint).normalized;
        //Vector3 perpendicular = Vector3.Cross(direction, Vector3.forward).normalized * tapeWidth / 2f;
        Vector3 perpendicular = Vector3.Cross(Vector3.up, Camera.main.transform.forward).normalized * tapeWidth / 2f;

        // --- Generate Start Edge Vertices ---
        if (topEdge != null && topEdge.Count > 0)
        {
            foreach(Vector3 point in topEdge)
            {
                //We likely need to subtract the total height from the y before we do it else this will be just
                //as tall as the new tape piece
                Vector3 newPoint = new Vector3(point.x, Mathf.Abs(totalY - point.y), point.z);
                vertices.Add(newPoint);
            }
            for (int i = 0; i <= segments; i++)
            {
                uvs.Add(new Vector2(0, (float)i / segments));
            }
        }
        else
        {
            // Create a straight start edge
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                vertices.Add(Vector3.Lerp(startPoint - perpendicular, startPoint + perpendicular, t));
                uvs.Add(new Vector2(0, t));
            }
        }

        // Create a straight end edge
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            vertices.Add(Vector3.Lerp(endPoint - perpendicular, endPoint + perpendicular, t));
            uvs.Add(new Vector2(0, t));
        }

        // Make triangles
        for (int i = 0; i < segments; i++)
        {
            int baseIndex = i;
            int nextIndex = i + 1;
            int endBaseIndex = i + segments + 1;
            int endNextIndex = i + segments + 2;

            // First triangle
            triangles.Add(baseIndex);
            triangles.Add(endNextIndex);
            triangles.Add(nextIndex);

            // Second triangle
            triangles.Add(baseIndex);
            triangles.Add(endBaseIndex);
            triangles.Add(endNextIndex);
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
    }
}