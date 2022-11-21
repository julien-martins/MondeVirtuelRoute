using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Node
{
    public Vector2Int Index;
    public Vector3 WorldPos;
    
    public int Cout;
    public int Heuristique;
    
    public Color Color;

    public Node Pred;
    
    public Node(Vector2Int index, Vector3 worldPos, int cout, int heuristique)
    {
        Index = index;
        WorldPos = worldPos;
        Cout = cout;
        Heuristique = heuristique;
        Color = Color.blue;

        Pred = null;
    }

    public override bool Equals(object obj)
    {
        Node a = (Node)obj;
        
        return Index == a.Index;
    }
}

public class RoadGenerator : MonoBehaviour
{ 
    public Grid Grid;
    public int TileSize = 10;

    private Node[,] nodes;
    private float[,] PerlinValue;

    public Vector2Int StartPoint;
    public Vector2Int EndPoint;

    public float PerlinScale = 1.0f;
    [Range(0, 1.0f)]
    public float PerlinThreshold = 0.3f;

    public int PathSplit = 4;
    
    // Start is called before the first frame update
    void Start()
    {
        GeneratePerlinNoise();
        InitializeGrid();
        
    }
    
    private void OnDrawGizmos()
    {
        

        DrawGrid();
    }

    void GeneratePerlinNoise()
    {
        PerlinValue = new float[(int)Grid.cellSize.x, (int)Grid.cellSize.y];

        for (int j = 0; j < Grid.cellSize.y; ++j)
        {
            for (int i = 0; i < Grid.cellSize.x; ++i)
            {
                float x = (i / Grid.cellSize.x * PerlinScale);
                float y = (j / Grid.cellSize.y * PerlinScale);

                float val = Mathf.PerlinNoise(x, y);
                
                PerlinValue[i, j] = val;
            }
        }
    }
    
    //Initialize the grid with all nodes which they use in the Astar algorithm
    void InitializeGrid()
    {
        nodes = new Node[(int)Grid.cellSize.x, (int)Grid.cellSize.y];
        
        var offset_x = (Grid.cellSize.x * TileSize) / 2 + TileSize/2;
        var offset_y = (Grid.cellSize.y * TileSize) / 2 + TileSize/2;
        
        for (int j = 0; j < Grid.cellSize.y; ++j)
        {
            for (int i = 0; i < Grid.cellSize.x; ++i)
            {
                Vector3 worldPos = new Vector3(i * TileSize - offset_x, 0, j * TileSize - offset_y );
                Node node = new Node(new Vector2Int(i, j), worldPos, 0, 0);
                var val = PerlinValue[i, j];
                
                //Change the color in function of the point
                if (node.Index == StartPoint) node.Color = Color.green;
                else if (node.Index == EndPoint) node.Color = Color.red;
                else node.Color = new Color(val, val, val);

                nodes[i, j] = node;
            }
        }
    }

    void DrawGrid()
    {
        for (int j = 0; j < Grid.cellSize.y; ++j)
        {
            for (int i = 0; i < Grid.cellSize.x; ++i)
            {
                Gizmos.color = nodes[i, j].Color;
                Gizmos.DrawCube(nodes[i, j].WorldPos, Vector3.one * TileSize / 2 );
                
            }
        }
    }
    
    int Heuristic(Node start, Node target)
    {
        var dx = target.Index.x - start.Index.x;
        var dy = target.Index.y - start.Index.y;

        return Math.Abs(dx) + Math.Abs(dy);
    }

    bool InMatrix(Vector2Int p)
    {
        if (p.x < 0 || p.x >= Grid.cellSize.x || p.y < 0 || p.y >= Grid.cellSize.y) return false;

        return true;
    }

    private bool goalReach = false;
    
    public void FindPathAction()
    {
        StartCoroutine(FindPath(nodes[StartPoint.x, StartPoint.y], nodes[EndPoint.x, EndPoint.y]));
        /*
        //Separer la recherche du chemin en plusieur partie
        float dist = Vector2.Distance(StartPoint, EndPoint);
        float distRatio = dist / PathSplit;

        List<Vector2Int>[] allPaths = new List<Vector2Int>[PathSplit];

        float distXRatio = (EndPoint.x - StartPoint.x) / PathSplit;
        float distYRatio = (EndPoint.y - StartPoint.y) / PathSplit;

        Vector2Int currentStartPoint = StartPoint;
        Vector2Int currentEndPoint = StartPoint + new Vector2Int((int)distXRatio, (int)distYRatio);

        for (int i = 2; i < PathSplit; ++i)
        {
            Debug.Log("PathSplit : " + i);
            
            allPaths[i-2] = FindPath(nodes[currentStartPoint.x, currentStartPoint.y], nodes[currentEndPoint.x, currentEndPoint.y]);

            currentStartPoint = currentEndPoint;
            currentEndPoint = StartPoint + new Vector2Int((int)distXRatio, (int)distYRatio) * i;
        }
        */
        //FindPath(nodes[StartPoint.x, StartPoint.y], nodes[EndPoint.x, EndPoint.y]);

    }
    
    List<Node> GetNeightbors(Node n)
    {
        List<Node> result = new();

        Vector2Int[] directions = { Vector2Int.up,  Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in directions)
        {
            Vector2Int newPos = n.Index + dir;

            if (!InMatrix(newPos)) continue;

            result.Add(nodes[newPos.x, newPos.y]);
        }
        
        return result;
    }
    
    Node GetSmallestNode(List<Node> nodes)
    {
        Node min = nodes[0];
        
        for (int i = 1; i < nodes.Count; ++i)
        {
            if (nodes[i].Heuristique < min.Heuristique)
                min = nodes[i];
        }
        
        return min;
    }
    
    IEnumerator FindPath(Node start, Node end)
    {
        List<Node> closed = new();
        List<Node> open = new();

        open.Add(start);
        
        while (open.Count > 0)
        {
            Node u = GetSmallestNode(open);
            open.Remove(u);

            if (u.Index == end.Index)
            {
                goalReach = true;
                return;
            }

            List<Node> neightbors = GetNeightbors(u);
            foreach (var neightbor in neightbors)
            {
                Node v = neightbor;
                if (!closed.Contains(v) && PerlinValue[v.Index.x, v.Index.y] > PerlinThreshold)
                {
                    v.Cout = u.Cout + 1;
                    v.Heuristique = v.Cout + Heuristic(v, end);
                    v.Pred = u;
                    
                    open.Add(v);
                }
                
                closed.Add(u);
            }

        }
    }

    List<Vector2Int> RecoverPath(Node n)
    {
        List<Vector2Int> result = new();

        Node val = n;
        while (val != null)
        {
            val.Color = Color.magenta;
            result.Add(val.Index);

            val = val.Pred;
        }
        
        return result;
    }
    
}
