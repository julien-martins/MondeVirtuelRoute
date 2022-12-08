using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabGeneration : MonoBehaviour
{
    public GameObject Ground;

    void GenerateRoad(Vector3 pointA, Vector3 pointB)
    {
        Instantiate(Ground, new Vector3(pointA.x + (pointB.x - pointA.x) / 2, pointA.y + (pointB.y - pointA.y) / 2, pointA.z + (pointB.z - pointA.z) / 2), new Quaternion(0, Mathf.Sin(pointB.z - pointA.z), 0, 1));


    }
}
