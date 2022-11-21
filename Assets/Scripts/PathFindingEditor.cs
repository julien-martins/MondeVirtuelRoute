using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadGenerator))]
public class PathFindingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoadGenerator roadGenerator = (RoadGenerator)target;
        if (GUILayout.Button("Find Path"))
        {
            roadGenerator.FindPathAction();
        }
    }
}
