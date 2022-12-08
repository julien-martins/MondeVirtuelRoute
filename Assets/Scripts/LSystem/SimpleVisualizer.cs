using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleVisualizer : MonoBehaviour
{
    public LSystemGenerator lSystem;
    public List<Vector3> positions = new List<Vector3>();
    public List<Vector3> Nextpositions = new List<Vector3>();

    public GameObject prefab;
    public Material lineMaterial;

    private int length = 96;
    private float angle = 90;
    
    public int Length
    {
        get 
        {
            if(length > 0)
            {
                return length;
            }
            else
            {
                return 1;
            }
        }
        set => length = value;
    }


    private void Start()
    {
        var sequence = lSystem.GenerateSentence();
        VisualizeSequence(sequence);
    }
    public void VisualizeSequence(string sequence, Vector3 startPos = new (), Vector3 dir = new())
    {
        Stack<AgentParameters> savePoints = new Stack<AgentParameters>();
        var currentPosition = startPos;

        if(dir == Vector3.zero) dir = Vector3.forward;
        Vector3 direction = dir;
        Vector3 tempPosition = Vector3.zero;

        positions.Add(currentPosition);
        Nextpositions.Add(currentPosition);

        angle = Random.Range(70, 80);
        
        foreach(var letter in sequence)
        {
            EncodingLetters encoding = (EncodingLetters)letter;

            switch (encoding)
            {
                case EncodingLetters.save:
                    savePoints.Push(new AgentParameters
                    {
                        position = currentPosition,
                        direction = direction,
                        length = Length
                    });
                    break;
                case EncodingLetters.load:
                    if(savePoints.Count > 0)
                    {
                        var agentParameter = savePoints.Pop();
                        currentPosition = agentParameter.position;
                        direction = agentParameter.direction;
                        Length = agentParameter.length;
                    }
                    else
                    {
                        throw new System.Exception("Don't have saved point in our stack");
                    }
                    break;
                case EncodingLetters.draw:
                    tempPosition = currentPosition;
                    currentPosition += direction * length;
                    //DrawLine(tempPosition, currentPosition, Color.red);
                    Length -= 2;
                    positions.Add(currentPosition);
                    Nextpositions.Add(tempPosition);
                    break;
                case EncodingLetters.turnRight:
                    direction = Quaternion.AngleAxis(angle, Vector3.up)*direction;
                    break;
                case EncodingLetters.turnLeft:
                    direction = Quaternion.AngleAxis(-angle, Vector3.up) * direction;
                    break;
                default:
                    break;
            }
        }

        /*
        foreach(var position in positions)
        {
            Instantiate(prefab, position, Quaternion.identity);
        }
        */
    }

    private void DrawLine(Vector3 start, Vector3 end, Color color)
    {
        GameObject line = new GameObject("line");
        line.transform.position = start;
        var lineRenderer = line.AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    public enum EncodingLetters
    {
        unknown = '1',
        save = '[',
        load = ']',
        draw = 'F',
        turnRight = '+',
        turnLeft = '-'
    }
}
