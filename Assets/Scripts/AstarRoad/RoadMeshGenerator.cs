using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadMeshGenerator : MonoBehaviour
{
    public Material MainRoadMat;
    public Material SecondaryRoadMat;

    public Mesh CreateRoadMesh(List<Node> path)
    {
        Mesh msh = new Mesh();
        if (path.Count <= 1) return msh;

        List<Vector3> vertices = new();
        List<Vector2> uv = new();
        List<int> triangles = new();

        int vertIndex = 0;
        for(int i = 0; i < path.Count; ++i)
        {
            Vector3 forward = Vector3.zero;
            if (i < path.Count - 1)
            {
                forward += path[i + 1].WorldPos - path[i].WorldPos;
            }
            if (i > 0)
            {
                forward += path[i].WorldPos - path[i - 1].WorldPos;
            }
            forward.Normalize();

            Vector3 left = new Vector3(-forward.z, 0, forward.x);

            vertices.Add( path[i].WorldPos + left * 8f);
            vertices.Add( path[i].WorldPos - left * 8f);

            float completionPercent = i / (float)(path.Count - 1);
            
            uv.Add(new Vector2(0, completionPercent));
            uv.Add(new Vector2(1, completionPercent));
            
            
            if (i < path.Count - 1)
            {
                triangles.Add(vertIndex);
                triangles.Add(vertIndex + 2);
                triangles.Add(vertIndex + 1);
                
                triangles.Add(vertIndex + 1);
                triangles.Add(vertIndex + 2);
                triangles.Add(vertIndex + 3);
            }

            vertIndex += 2;
        }


        msh.vertices = vertices.ToArray();
        msh.triangles = triangles.ToArray();
        msh.uv = uv.ToArray();
        
        return msh;
    }

    public Mesh CreateRoadMesh(Node a, Node b)
    {
        Mesh msh = new Mesh();

        List<Vector3> vertices = new();
        List<Vector2> uv = new();
        List<int> triangles = new();

        Vector3 forwardA = b.WorldPos - a.WorldPos;
        Vector3 forwardB = a.WorldPos - b.WorldPos;
        forwardA.Normalize();
        forwardB.Normalize();
        
        Vector3 leftA = new Vector3(-forwardA.z, 0, forwardA.x);
        Vector3 leftB = new Vector3(-forwardB.z, 0, forwardB.x);
        
        vertices.Add(a.WorldPos + leftA * 8f);
        vertices.Add(a.WorldPos - leftA * 8f);
        
        vertices.Add(b.WorldPos + leftB * 8f);
        vertices.Add(b.WorldPos - leftB * 8f);
        
        triangles.Add(2);
        triangles.Add(1);
        triangles.Add(0);
        
        triangles.Add(0);
        triangles.Add(3);
        triangles.Add(2);

        uv.Add(new Vector2(1, 0));
        uv.Add(new Vector2(0, 0));
        
        uv.Add(new Vector2(0, 1));
        uv.Add(new Vector2(1, 1));
        
        msh.vertices = vertices.ToArray();
        msh.triangles = triangles.ToArray();
        //msh.uv = uv.ToArray();
        
        msh.RecalculateNormals();
        
        return msh;
    }
    
    public void DeleteChildMesh()
    {
        foreach (Transform child in transform)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }
    }
}
