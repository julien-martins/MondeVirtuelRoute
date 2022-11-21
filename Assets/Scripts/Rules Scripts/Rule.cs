using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="ProceduralRoad/Rule")]
public class Rule : ScriptableObject
{
    public string letter;
    [SerializeField]
    private string[] result = null;

    public string GetResult()
    {
        return result[0];
    }
}
