using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Curves;
public class CurvesGenerator : MonoBehaviour
{
    public double startX = 0;
    public double startY = 0;

    public double startDirection = 1.0;

    public double startCurvature = 0.0;

    public double a = 11.0;

    public double length = 125.0;

    public int n = 10;
    
    private Clothoid _clotho;

    private List<GameObject> cubes = new();
    // Start is called before the first frame update
    void Start()
    {

        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var cube in cubes)
        {
            Destroy(cube);
        }
        
        _clotho = new Clothoid(startX, startY, startDirection, startCurvature, a, length);
        //_clotho =  Clothoid.FromPoseAndPoint(0.0, 0.0, 0.0, 25.0, 25.0);
        Vector3 centre = new Vector3();

        foreach (Point2D point in _clotho.GetPoints(n))
        {
            centre.x = (float)point.X;
            centre.y = 0.0f;

            centre.z = (float)point.Y;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = centre;
            go.transform.parent = transform;
            cubes.Add(go);

        }
    }


}
