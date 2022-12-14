using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Curves;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

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

    public Node()
    {
        Index = Vector2Int.zero;
        WorldPos = Vector3.zero;
        Cout = 0;
        Heuristique = 0;
        Color = Color.blue;

        Pred = null;
    }
    
    public override bool Equals(object obj)
    {
        Node a = (Node)obj;
        
        return Index == a.Index;
    }
}

[RequireComponent(typeof(Grid), typeof(LSystemGenerator), typeof(RoadMeshGenerator))]
public class RoadGenerator : MonoBehaviour
{
    [Header("General Parameter")] 
    public bool DebugVisualizer = false;
    
    [Header("Grid Parameter")]
    public Grid Grid;
    public int TileSize = 10;
    public GameObject CasePrefab;

    [Header("Astar Parameter")]
    public Vector2Int StartPoint;
    public Vector2Int EndPoint;
    
    [Header("Perlin Noise Parameter")]
    public float PerlinScale = 1.0f;
    [Range(0, 1.0f)]
    public float PerlinThreshold = 0.3f;

    [Header("LSystem Parameter")] 
    public LSystemGenerator LSystemGenerator;
    
    private SimpleVisualizer _simpleVisualizer;
    
    private Mesh roadMesh;
    private List<Mesh> secondaryRoadMesh;
    
    //public GameObject Test;

    [Header("Road Mesh Parameter")] 
    public RoadMeshGenerator RoadMeshGenerator;
    
    private Node[,] nodes;
    private float[,] PerlinValue;
    
    private bool init = false;
    
    private List<Node> path;
    private List<Node> badNode;
    private List<Node> testNode = new();

    private List<Vector2Int> testClotho = new();
    
    private Clothoid _clotho;
    
    // Start is called before the first frame update
    void Start()
    {
        GeneratePerlinNoise();
        InitializeGrid();
    }
    
    private void OnDrawGizmos()
    {
        if (!DebugVisualizer) return;
        DrawGrid();

        if (badNode == null) return;
        foreach (var node in badNode)
        {
            node.Color = Color.red;
        }

        foreach (var node in testNode)
        {
            node.Color = Color.blue;
        }

        foreach (var node in testClotho)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(new Vector3(node.x, 0, node.y), Vector3.one * TileSize);
        }
        
    }
    
    /*
     * Generate a grid with perlin noise value inside
     */
    public void GeneratePerlinNoise()
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
    
    /*
     * Initialize the grid with all nodes which they use in the Astar algorithm
     */
    public void InitializeGrid()
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

    /*
     * Draw the grid only with gizmos for now
     */
    void DrawGrid()
    {
        if (Application.isPlaying) return;
        if (nodes == null) return;
        
        for (int j = 0; j < Grid.cellSize.y; ++j)
        {
            for (int i = 0; i < Grid.cellSize.x; ++i)
            {
                //var obj = Instantiate(CasePrefab, transform);
                //obj.transform.position = nodes[i, j].WorldPos;
           
                Gizmos.color = nodes[i, j].Color;
                Gizmos.DrawCube(nodes[i, j].WorldPos, Vector3.one * TileSize /2);
            }
        }
    }
    
    /*
     * callback function called by the PathFindingEditor class
     */
    public void FindPathAction()
    {
        secondaryRoadMesh = new();
        RoadMeshGenerator.DeleteChildMesh();

        GeneratePerlinNoise();
        InitializeGrid();
        FindPath(nodes[StartPoint.x, StartPoint.y], nodes[EndPoint.x, EndPoint.y]);
        path = RecoverPath(nodes[EndPoint.x, EndPoint.y]);

        badNode = SmoothPath(path);
        
        //Make the Lsystem on the second node on the path
        var secondaryPath = AddLSystem();

        
        foreach (var path in secondaryPath)
        {
            Debug.Log(DrawSecondaryRoad(path).Count);
            secondaryRoadMesh.AddRange(DrawSecondaryRoad(path));
        }
        
        
        GameObject go1 = new GameObject();
        go1.name = "Secondary Mesh";
        go1.transform.parent = transform;
        foreach (var msh in secondaryRoadMesh)
        {
            GameObject go = new GameObject();
            go.name = "mesh";
            go.transform.parent = go1.transform;
            var meshFilter = go.AddComponent<MeshFilter>();
            var meshRenderer = go.AddComponent<MeshRenderer>();

            meshFilter.mesh = msh;
            meshRenderer.material = RoadMeshGenerator.SecondaryRoadMat;
        }
        
    }

    public List<Mesh> DrawSecondaryRoad(List<Node> path)
    {
        if (path.Count == 0) return new();
        
        List<Node> open = new();
        List<Node> closed = new();

        List<Mesh> res = new();
        
        Vector2Int[] dirs =
        {
            new Vector2Int(1, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 1),
            new Vector2Int(-1, -1), new Vector2Int(-1, 1)
        };
        
        open.Add(path[0]);
        while (open.Count > 0)
        {   
            Node n = open[0];
            open.RemoveAt(0);
            
            //Get AllNeighbors
            foreach (Vector2Int dir in dirs)
            {
                Vector2Int newIndex = n.Index + dir;
                if (!InMatrix(newIndex)) continue;

                Node newNode = nodes[newIndex.x, newIndex.y];

                if (!path.Contains(newNode)) continue;
                if (open.Contains(newNode) || closed.Contains(newNode)) continue;

                //Draw Mesh
                res.Add(RoadMeshGenerator.CreateRoadMesh(n, newNode));

                open.Add(newNode);
            }

            closed.Add(n);
        }

        return res;
    }
    
    public void GenerateRoadMesh()
    {
        roadMesh = RoadMeshGenerator.CreateRoadMesh(path);

        var meshFilter = transform.GetOrAddComponent<MeshFilter>();
        var meshRenderer = transform.GetOrAddComponent<MeshRenderer>();

        meshFilter.mesh = roadMesh;
        meshRenderer.material = RoadMeshGenerator.MainRoadMat;
    }

    #region LSystemIntegration

    List<List<Node>> AddLSystem()
    {
        List<List<Node>> res = new();

        Node keepTheLast = null;
        for (int j = 0; j < path.Count-1; ++j)
        {
            keepTheLast = null;
            if(Random.Range(0, 32) != 1) continue;

            var sequence = LSystemGenerator.GenerateSentence();
            
            _simpleVisualizer = new SimpleVisualizer();

            Vector3 normal = new Vector3(-(int)(path[j+1].Index.x - path[j].Index.x), 0, (int)(path[j+1].Index.y - path[j].Index.y));
            
            if(Random.Range(0, 2) == 1)
                _simpleVisualizer.VisualizeSequence(sequence, path[j].WorldPos, normal);
            else
                _simpleVisualizer.VisualizeSequence(sequence, path[j].WorldPos, -normal);

            var t = new List<Node>();
            for (int i = 0; i < _simpleVisualizer.positions.Count; ++i)
            {
                var offsetX = (Grid.cellSize.x / 2) * TileSize;
                var offsetZ = (Grid.cellSize.y / 2) * TileSize;

                Vector2Int coord = new Vector2Int((int)Mathf.Abs((_simpleVisualizer.positions[i].x + offsetX) / TileSize),
                    (int)Mathf.Abs((_simpleVisualizer.positions[i].z + offsetZ) / TileSize));

                Vector2Int nextCoord = new Vector2Int((int)Mathf.Abs((_simpleVisualizer.Nextpositions[i].x + offsetX) / TileSize),
                    (int)Mathf.Abs((_simpleVisualizer.Nextpositions[i].z + offsetZ) / TileSize));
                
                if (InMatrix(coord))
                {
                    bool find = false;
                    foreach (var node in path)
                    {
                        if (!InMatrix(coord.x, coord.y) || !InMatrix(nextCoord.x, nextCoord.y)) continue;
                        
                        var currentNode = nodes[coord.x, coord.y];
                        var nextNode = nodes[nextCoord.x, nextCoord.y];
                        
                        if (currentNode.Index == node.Index || currentNode.Index == nextNode.Index)
                        {
                            find = true;
                            break;
                        }

                    }

                    if (!find)
                    {
                        var path = BresenhamLine(coord.x, coord.y, nextCoord.x, nextCoord.y);
                        t.AddRange(path);
                    }

                }

            }
            res.Add(t);
        }

        return res;
    }

    public List<Node> BresenhamLine(int x0, int y0, int x1, int y1)
    {
        List<Node> res = new();

        // Calculate dx and dy
        int dx = x1 - x0;
        int dy = y1 - y0;
 
        int step;
 
        // If dx > dy we will take step as dx
        // else we will take step as dy to draw the complete
        // line
        if (Math.Abs(dx) > Math.Abs(dy))
            step = Math.Abs(dx);
        else
            step = Math.Abs(dy);
 
        // Calculate x-increment and y-increment for each step
        float x_incr = (float)dx / step;
        float y_incr = (float)dy / step;
 
        // Take the initial points as x and y
        float x = x0;
        float y = y0;
 
        for (int i = 0; i < step; i++) {
            //Get node on the grid and change the color
            if(!InMatrix((int)x, (int)y)) continue;

            var node = nodes[(int)x, (int)y];
            
            node.Color = Color.yellow;
            res.Add(node);

            x += x_incr;
            y += y_incr;
        }
        
        return res;
    }

    #endregion
    
    #region SmoothProcedureFunction
    
    /*
     * Smooth the path with bezier curves
     */
    public List<Node> SmoothPath(List<Node> path)
    {
        var badPos = GetStrongAngle(path);
        var mergedNode = MergeErrorNode(path, badPos);
        var splitErrorNodes = SplitErrorNode(path, mergedNode);

        return GenerateSmoothCurves(splitErrorNodes);
    }
    
    /*
     * Generate curve with QuadraticBezierCurves function
     * on list of nodes returned by SmoothPath
     */
    public List<Node> GenerateSmoothCurves(List<Node> errorNodes)
    {
        List<Node> result = new();

        int startIndex = 0;
        int endIndex = 0;
        for (int i = 0; i < errorNodes.Count; i += 3)
        {
            Vector3 p0 = errorNodes[i].WorldPos;
            Vector3 p1 = errorNodes[i + 1].WorldPos;
            Vector3 p2 = errorNodes[i + 2].WorldPos;

            for (int j = 0; j < path.Count; ++j)
            {
                if (errorNodes[i].Index == path[j].Index)
                {
                    startIndex = j;
                }
                if (errorNodes[i + 2].Index == path[j].Index)
                {
                    endIndex = j;
                }
            }

            //Remove nodes on the path
            for (int j = startIndex; j <= endIndex; ++j)
            {
                path[j].Color = Color.cyan;
                path.RemoveAt(j);
            }
            
            for (float t = 0; t <= 1; t += 0.1f)
            {
                Vector3 p = QuadraticBezierCurves(t, p0, p1, p2);

                var offsetX = (Grid.cellSize.x/2) * TileSize;
                var offsetZ = (Grid.cellSize.y/2) * TileSize;
                
                Vector2Int coord = new Vector2Int((int)Mathf.Abs((p.x + offsetX) / TileSize ), (int)Mathf.Abs((p.z + offsetZ) / TileSize ));
                
                if (InMatrix(coord))
                {
                    result.Add(nodes[coord.x, coord.y]);
                }
            }
        }
        
        return result;
    }

    /*
     * Get the list of nodes where there are a big variation of angle in the path generated by the astar
     * path: list of nodes generated by the astar function
     */
    List<Node> GetStrongAngle(List<Node> path)
    {
        List<Node> BadPos = new();

        if (path.Count <= 1) return BadPos;
        
        var dir = path[1].WorldPos - path[0].WorldPos;
        float prevAngle = Vector3.Angle(dir, transform.right);
        for (int i = 1; i < path.Count-1; i ++)
        {
            dir = path[i + 1].WorldPos - path[i].WorldPos;

            var dist = Vector3.Distance(path[i].WorldPos, path[i + 1].WorldPos);
            
            var angle = Vector3.Angle(dir, transform.right);
            
            if(prevAngle != angle) {
                BadPos.Add(path[i]);
            }
            
            prevAngle = angle;
        }

        //Merge error Node
        var mergedNode = MergeErrorNode(path, BadPos);
        var splitErrorNodes = SplitErrorNode(path, mergedNode);

        BadPos = splitErrorNodes;
        
        return BadPos;
    }
    
    /*
     * function use to merge all the error node on the path generated by GetStrongAngle function
     * path: path generated by the astar
     * badPos: list of nodes generated by GetStrongAngle
     */
    List<Node> MergeErrorNode(List<Node> path, List<Node> badPos)
    {
        List<Node> mergedErrorNodes = new List<Node>();

        List<Node> errorNode = new();
        int errorCount = 0;
        foreach (var path_node in path)
        {
            if(errorNode.Count > 0) errorCount++;
            
            foreach (var badpos in badPos)
            {
                //We are on an error node
                if (path_node.Index == badpos.Index) {
                    errorNode.Add(path_node);
                }
            }

            //We can merge all error after 5 node after the first error node
            if (errorCount > 10)
            {
                mergedErrorNodes.Add(errorNode[errorNode.Count / 2]);

                errorNode = new();
                errorCount = 0;
            }
        }

        return mergedErrorNodes;
    }
    
    /*
     * Take the path with all error merged and create three point which will used on the bezier curves function
     * path: path generated by the astar algorithm
     * badPos: list of error nodes returned by the MergeErrorNode function
     */
    List<Node> SplitErrorNode(List<Node> path, List<Node> badPos)
    {
        List<Node> splitErrorNodes = new();
        
        for (int i = 1; i < path.Count; i++)
        {
            for (int j = 0; j < badPos.Count; j++)
            {
                if (path[i].Index != badPos[j].Index) continue;
                
                for (int k = i - 5; k < i; k++)
                {
                    if (k < 0) continue;
                    
                    splitErrorNodes.Add(path[k]);
                    break;
                }
                
                splitErrorNodes.Add(path[i]);
                
                for (int k = i + 5; k > i; k--)
                {
                    if (k > path.Count) continue;
                    
                    splitErrorNodes.Add(path[k]);
                    break;
                }
                
            }
        }

        return splitErrorNodes;
    }

    /*
     * Basic Quadratic Bezier function
     * with 0 <= t <= 1, and three points p0, p1, p2
     */
    public Vector3 QuadraticBezierCurves(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return (1 - t)*((1-t)*p0 + t * p1) + t * ((1 - t) * p1 + t * p2);
    }

    public Vector3 HermineCurves(float t, Vector3 p0, Vector3 d0, Vector3 p1, Vector3 d1)
    {
        Vector3 res = Vector3.zero;
        res.x = (float)((2 * Math.Pow(t, 3) - 3 * Math.Pow(t, 2) + 1) * p0.x +
                (Math.Pow(t, 3) - 2 * Math.Pow(t, 2) + t) * d0.x +
                (-2 * Math.Pow(t, 3) + 3 * Math.Pow(t, 2) * p1.x + (Math.Pow(t, 3) - Math.Pow(t, 2)) * d1.x));
        
        res.z = (float)((2 * Math.Pow(t, 3) - 3 * Math.Pow(t, 2) + 1) * p0.z +
                        (Math.Pow(t, 3) - 2 * Math.Pow(t, 2) + t) * d0.z +
                        (-2 * Math.Pow(t, 3) + 3 * Math.Pow(t, 2) * p1.z + 
                         (Math.Pow(t, 3) - Math.Pow(t, 2)) * d1.z));
        
        return res;
    }
    #endregion
    
    #region Astar Algorithm
    
    /*
     * Implementation of Astar algorithm with a perlin function as weight
     */
    void FindPath(Node start, Node end)
    {
        List<Node> closed = new();
        List<Node> open = new();

        open.Add(start);
        
        
        while (open.Count > 0)
        {
            Node u = GetSmallestNode(open);
            open.Remove(u);

            if (u.Index == end.Index) return;

            List<Node> neightbors = GetNeightbors(u);
            foreach (var neightbor in neightbors)
            {
                Node v = neightbor;
                if (!closed.Contains(v) && !open.Contains(v) && PerlinValue[v.Index.x, v.Index.y] > PerlinThreshold)
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
    
    /*
     * Recover the all path created by the astar algorithm
     * n: the end node pass in the FindPath function
     */
    List<Node> RecoverPath(Node n)
    {
        List<Node> result = new();

        Node val = n;
        while (val != null)
        {
            val.Color = Color.magenta;
            result.Add(val);

            val = val.Pred;
        }
        
        return result;
    }
    
    /*
     * Get all neightbors around a node
     * used for the astar algorithm
     */
    List<Node> GetNeightbors(Node n)
    {
        List<Node> result = new();

        Vector2Int[] directions = { Vector2Int.up,  Vector2Int.down, Vector2Int.left, Vector2Int.right, new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1),  };

        foreach (var dir in directions)
        {
            Vector2Int newPos = n.Index + dir;

            if (!InMatrix(newPos)) continue;

            result.Add(nodes[newPos.x, newPos.y]);
        }
        
        return result;
    }
    
    /*
     * Check if a point p is in the grid which is a component attach to the gameobject
     */
    bool InMatrix(Vector2Int p)
    {
        if (p.x < 0 || p.x >= Grid.cellSize.x || p.y < 0 || p.y >= Grid.cellSize.y) return false;

        return true;
    }
    
    bool InMatrix(int x, int y) => InMatrix(new Vector2Int(x, y));
    
    /*
     * Get the smallest node on the list
     * used for the astar algorithm
     */
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
    
    /*
     * Heuristic function use to the astar algorithm
     */
    int Heuristic(Node start, Node target)
    {
        var dx = target.Index.x - start.Index.x;
        var dy = target.Index.y - start.Index.y;

        return Math.Abs(dx) + Math.Abs(dy);
    }
    #endregion
}
