using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct Node
{
    public Vector2Int Index;
    public Vector3 WorldPos;
    
    public int Cout;
    public int Heuristique;
    
    public Color Color;
}

public class RoadGenerator : MonoBehaviour
{ 
    public Grid Grid;
    public int TileSize = 10;

    private Node[,] nodes;

    public Vector2Int StartPoint;
    public Vector2Int EndPoint;
    
    // Start is called before the first frame update
    void Start()
    {
        InitializeGrid();
    }

    private void OnDrawGizmos()
    {
        InitializeGrid();        
    }

    // Update is called once per frame
    void Update()
    {
        
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
                Node node = new Node();
                node.Cout = 0;
                node.Heuristique = 0;
                node.Index = new Vector2Int(i, j);
                node.WorldPos = new Vector3(i * TileSize - offset_x, 0, j * TileSize - offset_y );

                if (node.Index == StartPoint) node.Color = Color.green;
                else if (node.Index == EndPoint) node.Color = Color.red;
                else node.Color = Color.blue;

                nodes[i, j] = node;
                
                Gizmos.color = node.Color;
                Gizmos.DrawCube(node.WorldPos, Vector3.one * TileSize / 2 );
            }
        }
    }
    
    
    List<Vector2Int> FindPath(Node start, Node end)
    {
        List<Vector2Int> result = new();

        List<Node> closed = new();
        List<Node> open = new();

        open.Add(start);
        
        while (open.Count > 0)
        {
            
        }
        
        return result;
    }
    
}
