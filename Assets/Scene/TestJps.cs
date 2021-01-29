using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Algorithm;

public class TestJps : MonoBehaviour
{
    public string flag;
    public int lineLen;
    public Vector2Int start;
    public Vector2Int end;

    Jps jps;
    LineRenderer _line;


    void Start()
    {
        _line = GetComponent<LineRenderer>();
    }

    void FindPath()
    {
        var s = flag.Split('.');
        int rowLen = s.Length / lineLen;

        bool[,] f = new bool[lineLen, rowLen];
        for (int i = 0; i < lineLen; i++)
        {
            for (int j = 0; j < rowLen; j++)
            {
                f[i, j] = s[i+j* lineLen] == "1" ? true : false;
            }
        }
        var grid = new Jps_Grid(f);
        jps = new Jps(grid);
        var paths = jps.FindPath(start, end);
        if (paths == null)
            return;
        _line.positionCount = paths.Count;

        Vector3[] poses = new Vector3[paths.Count];
        for (int i = 0; i < paths.Count; i++)
        {
            poses[i] = new Vector3(paths[i].x, 0, -paths[i].y);
        }
        _line.SetPositions(poses);

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            FindPath();
        }
    }
}
