using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;


namespace Algorithm
{

    public struct AstarNode
    {
        public int x;
        public int y;
        public long bid;

        public AstarNode(int xx, int yy, long bbid)
        {
            x = xx;
            y = yy;
            bid = bbid;
        }
    }


    public class FlowField
    {

        public struct Vec2
        {
            public int x;
            public int y;

            public Vec2(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            private static readonly Vec2 s_Zero = new Vec2(0, 0);
            public static Vec2 zero => s_Zero;


            public static Vec2 operator -(Vec2 v)
            {
                return new Vec2(-v.x, -v.y);
            }

            public static Vec2 operator +(Vec2 a, Vec2 b)
            {
                return new Vec2(a.x + b.x, a.y + b.y);
            }

            public static Vec2 operator -(Vec2 a, Vec2 b)
            {
                return new Vec2(a.x - b.x, a.y - b.y);
            }

            public static Vec2 operator *(Vec2 a, Vec2 b)
            {
                return new Vec2(a.x * b.x, a.y * b.y);
            }

            public static Vec2 operator *(int a, Vec2 b)
            {
                return new Vec2(a * b.x, a * b.y);
            }

            public static Vec2 operator *(Vec2 a, int b)
            {
                return new Vec2(a.x * b, a.y * b);
            }

            public static Vec2 operator /(Vec2 a, int b)
            {
                return new Vec2(a.x / b, a.y / b);
            }

            public static bool operator ==(Vec2 lhs, Vec2 rhs)
            {
                return lhs.x == rhs.x && lhs.y == rhs.y;
            }

            public static bool operator !=(Vec2 lhs, Vec2 rhs)
            {
                return !(lhs == rhs);
            }
        }


        private int m_countH;
        private int m_countV;
        private float[,] m_grids;
        private int[,] m_gridWeights;
        private long[,] m_gridBids;
        private Vec2[,] m_flowField;
        private Queue<Vec2> m_nextGrids = new Queue<Vec2>();
        private bool m_hasUpdateEnd = false;
        private List<(int, int, Action<List<AstarNode>>)> m_asyncGetPath = new List<(int, int, Action<List<AstarNode>>)>(); // 异步等待 路径返回
        private List<(int[], Action<List<AstarNode>[]>)> m_asyncGetPath2 = new List<(int[], Action<List<AstarNode>[]>)>(); // 异步等待 路径返回


        private static Vec2[] m_dirs = new Vec2[] {
            new Vec2(){ x = 0, y = 1 },
            new Vec2(){ x = 0, y = -1 },
            new Vec2(){ x = -1, y = 0 },
            new Vec2(){ x = 1, y = 0 },

            new Vec2(){ x = -1, y = 1 },
            new Vec2(){ x = 1, y = 1 },
            new Vec2(){ x = 1, y = -1 },
            new Vec2(){ x = -1, y = -1 },
        };


        public FlowField(int countH, int countV)
        {
            m_countH = countH;
            m_countV = countV;
            m_grids = new float[m_countV, m_countH];
            m_flowField = new Vec2[m_countV, m_countH];
        }


        public void UpdateFlowField(int x, int y, int[,] gridWeights, long[,] gridBids)
        {
            Clear();

            m_gridWeights = gridWeights;
            m_gridBids = gridBids;
            m_grids[y, x] = 0;
            m_flowField[y, x] = new Vec2(0, 0);
            m_nextGrids.Enqueue(new Vec2(x, y));

            GenFlowField();
        }

        public void UpdateFlowField(int[] points, int[,] gridWeights, long[,] gridBids)
        {
            Clear();

            m_gridWeights = gridWeights;
            m_gridBids = gridBids;
            for (int i = 0; i < points.Length; i+=2)
            {
                var x = points[i];
                var y = points[i+1];
                m_grids[y, x] = 0;
                m_flowField[y, x] = new Vec2(0, 0);
                m_nextGrids.Enqueue(new Vec2(x, y));
            }

            GenFlowField();
        }

        public List<AstarNode> GetPath(int x, int y)
        {
            if (!m_hasUpdateEnd)
            {
                return null;
            }

            var l = new List<AstarNode>();
            if (!IsOut(x, y))
            {
                l.Add(new AstarNode(x, y, m_gridBids[y, x]));

                var pre = new Vec2(x, y);
                var next = pre + m_flowField[y, x];
                var curDir = m_flowField[y, x];
                var curBid = m_gridBids[y, x];
                var preAdd = true;
                while (m_flowField[next.y, next.x] != Vec2.zero)
                {
                    if (curBid != m_gridBids[next.y, next.x]) // 建筑ID变化点
                    {
                        if (!preAdd) l.Add(new AstarNode(pre.x, pre.y, m_gridBids[pre.y, pre.x]));
                        l.Add(new AstarNode(next.x, next.y, m_gridBids[next.y, next.x]));
                        curDir = m_flowField[next.y, next.x];
                        curBid = m_gridBids[next.y, next.x];
                        preAdd = true;
                    }
                    else if (curDir != m_flowField[next.y, next.x]) // 拐点
                    {
                        l.Add(new AstarNode(next.x, next.y, m_gridBids[next.y, next.x]));
                        curDir = m_flowField[next.y, next.x];
                        //curBid = m_gridBids[next.y, next.x];
                        preAdd = true;
                    }
                    else
                    {
                        preAdd = false;
                    }
                    pre = next;
                    next += m_flowField[next.y, next.x];
                }
                if (!preAdd) l.Add(new AstarNode(pre.x, pre.y, m_gridBids[pre.y, pre.x]));
                l.Add(new AstarNode(next.x, next.y, m_gridBids[next.y, next.x]));
            }

            return l;
        }

        public void UpdateFlowFieldAsync(int x, int y, int[,] gridWeights, long[,] gridBids, Action cb = null)
        {
            //Debug.LogError($"UpdateFlowFieldAsync 11:   {Time.frameCount}");
            m_hasUpdateEnd = false;

            Task.Run(() => {
                UpdateFlowField(x, y, gridWeights, gridBids);
                UpdateEndCheck();
            });
            //Debug.LogError($"UpdateFlowFieldAsync 22:   {Time.frameCount}");
        }

        public void UpdateFlowFieldAsync(int[] points, int[,] gridWeights, long[,] gridBids, Action cb = null)
        {
            //Debug.LogError($"UpdateFlowFieldAsync 11:   {Time.frameCount}");
            m_hasUpdateEnd = false;

            Task.Run(() => {
                UpdateFlowField(points, gridWeights, gridBids);
                UpdateEndCheck();
            });
            //Debug.LogError($"UpdateFlowFieldAsync 22:   {Time.frameCount}");
        }

        public void GetPathAsync(int x, int y, System.Action<List<AstarNode>> cb)
        {
            if (!m_hasUpdateEnd)
            {
                m_asyncGetPath.Add((x, y, cb));
                return;
            }

            Task.Run(() => {
                var l = GetPath(x, y);
                cb?.Invoke(l);
            });
        }

        public void GetPathAsync(int[] points, System.Action<List<AstarNode>[]> cb)
        {
            if (!m_hasUpdateEnd)
            {
                m_asyncGetPath2.Add((points, cb));
                return;
            }

            Task.Run(() => {
                List<AstarNode>[] ret = new List<AstarNode>[points.Length/2];
                for (int i = 0; i < points.Length; i+=2)
                {
                    var l = GetPath(points[i], points[i+1]);
                    ret[i / 2] = l;
                }
                cb?.Invoke(ret);
            });
        }

        private void UpdateEndCheck()
        {
            m_hasUpdateEnd = true;

            if (m_asyncGetPath.Count > 0)
            {
                foreach (var item in m_asyncGetPath)
                {
                    GetPathAsync(item.Item1, item.Item2, item.Item3);
                }
                m_asyncGetPath.Clear();
            }
            if (m_asyncGetPath2.Count > 0)
            {
                foreach (var item in m_asyncGetPath2)
                {
                    GetPathAsync(item.Item1, item.Item2);
                }
                m_asyncGetPath.Clear();
            }
        }

        private void GenFlowField()
        {
            while (m_nextGrids.Count > 0)
            {
                var curCoord = m_nextGrids.Dequeue();

                for (int i = 0; i < m_dirs.Length; i++)
                {
                    var nextCoord = new Vec2(curCoord.x + m_dirs[i].x, curCoord.y + m_dirs[i].y);
                    if (!IsOut(nextCoord.x, nextCoord.y)) // 越界检测
                    {
                        var nextCost = m_grids[curCoord.y, curCoord.x] + GetCost(curCoord.x, curCoord.y, nextCoord.x, nextCoord.y, i);
                        if (nextCost < m_grids[nextCoord.y, nextCoord.x]) // 未检测 或 有更优值
                        {
                            //Debug.LogError($"({curCoord.x},{curCoord.y}),  ({nextCoord.x},{nextCoord.y}),  {nextCost},  {m_grids[nextCoord.y, nextCoord.x]}");

                            //m_grids[nextCoord.y, nextCoord.x] = m_grids[curCoord.y, curCoord.x] + Mathf.Abs(nextCoord.x - curCoord.x) + Mathf.Abs(nextCoord.y - curCoord.y);
                            m_grids[nextCoord.y, nextCoord.x] = nextCost;
                            m_flowField[nextCoord.y, nextCoord.x] = new Vec2(-m_dirs[i].x, -m_dirs[i].y);
                            m_nextGrids.Enqueue(nextCoord);
                        }
                    }
                }
            }
            //Log();
        }

        private float GetCost(int sx, int sy, int ex, int ey, int dirIndex)
        {
            if (dirIndex <= 3) return m_gridWeights[ey, ex]; // 上下左右
            return 1.4142136f * m_gridWeights[ey, ex]; // 对角移动
        }

        private bool IsOut(int x, int y)
        {
            if (x < 0 || y < 0 || x >= m_countH || y >= m_countV) return true;
            return false;
        }

        private void Clear()
        {
            for (int i = 0; i < m_countV; i++)
            {
                for (int j = 0; j < m_countH; j++)
                {
                    m_grids[i, j] = int.MaxValue;
                }
            }
            m_nextGrids.Clear();
        }

        public void Log()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    sb.Append(m_grids[i, j]);
                    sb.Append(',');
                }
                sb.Append('\n');
            }
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    sb.Append($"({m_flowField[i, j].x},{m_flowField[i, j].y})");
                    sb.Append(',');
                }
                sb.Append('\n');
            }

            Debug.LogError(sb.ToString());
            sb.Length = 0;
        }
    }
}