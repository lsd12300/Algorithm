using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Algorithm
{

    /// <summary>
    ///  Jump Point Search 算法.  A*的改进算法, 效率更快
    /// </summary>
    public class JPS
    {
        #region 结构定义

        public class Point
        {
            public Vector2Int coord; // 坐标
            public double f; // 经过当前点到目标点的 估值
            public double g; // 起点到当前点的 路程
            public Point pre; // 上一路径点
            public Vector2Int dir; // 移动方向
        }
        #endregion


        #region 属性

        protected List<Point> m_openList = new List<Point>();
        protected HashSet<Vector2Int> m_closeList = new HashSet<Vector2Int>();

        protected int m_countH;
        protected int m_countV;

        protected int[,] m_grids;
        protected Vector2Int m_end;

        // 八方向
        protected static Vector2Int[] m_moveDirs = new Vector2Int[] {
            new Vector2Int(0,1), new Vector2Int(0,-1), new Vector2Int(-1,0), new Vector2Int(1,0), // 上下左右
            new Vector2Int(-1,1), new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1) // 左上/右上/右下/左下
        };

        protected Dictionary<Vector2Int, Vector2Int> m_dirLeftDict; // 当前方向的左方, 左后方, 左前方, 右方, 右后方, 右前方
        protected Dictionary<Vector2Int, Vector2Int> m_dirLeftDownDict;
        protected Dictionary<Vector2Int, Vector2Int> m_dirLeftUpDict;
        protected Dictionary<Vector2Int, Vector2Int> m_dirRightDict;
        protected Dictionary<Vector2Int, Vector2Int> m_dirRightDownDict;
        protected Dictionary<Vector2Int, Vector2Int> m_dirRightUpDict;

        public bool m_Log = false;

        #endregion


        #region 方法


        public JPS()
        {
            // 左方
            m_dirLeftDict = new Dictionary<Vector2Int, Vector2Int>();
            m_dirLeftDict.Add(new Vector2Int(0, 1), new Vector2Int(-1, 0));
            m_dirLeftDict.Add(new Vector2Int(0, -1), new Vector2Int(1, 0));
            m_dirLeftDict.Add(new Vector2Int(-1, 0), new Vector2Int(0, -1));
            m_dirLeftDict.Add(new Vector2Int(1, 0), new Vector2Int(0, 1));
            //m_dirLeftDict.Add(new Vector2Int(1, 1), new Vector2Int(-1, 1));
            //m_dirLeftDict.Add(new Vector2Int(-1, 1), new Vector2Int(-1, -1));
            //m_dirLeftDict.Add(new Vector2Int(1, -1), new Vector2Int(1, 1));
            //m_dirLeftDict.Add(new Vector2Int(-1, -1), new Vector2Int(1, -1));

            // 左后方
            m_dirLeftDownDict = new Dictionary<Vector2Int, Vector2Int>();
            m_dirLeftDownDict.Add(new Vector2Int(0, 1), new Vector2Int(-1, -1));
            m_dirLeftDownDict.Add(new Vector2Int(0, -1), new Vector2Int(1, 1));
            m_dirLeftDownDict.Add(new Vector2Int(-1, 0), new Vector2Int(1, -1));
            m_dirLeftDownDict.Add(new Vector2Int(1, 0), new Vector2Int(-1, 1));

            // 左前方
            m_dirLeftUpDict = new Dictionary<Vector2Int, Vector2Int>();
            m_dirLeftUpDict.Add(new Vector2Int(0, 1), new Vector2Int(-1, 1));
            m_dirLeftUpDict.Add(new Vector2Int(0, -1), new Vector2Int(1, -1));
            m_dirLeftUpDict.Add(new Vector2Int(-1, 0), new Vector2Int(-1, -1));
            m_dirLeftUpDict.Add(new Vector2Int(1, 0), new Vector2Int(1, 1));

            // 右方
            m_dirRightDict = new Dictionary<Vector2Int, Vector2Int>();
            m_dirRightDict.Add(new Vector2Int(0, 1), new Vector2Int(1, 0));
            m_dirRightDict.Add(new Vector2Int(0, -1), new Vector2Int(-1, 0));
            m_dirRightDict.Add(new Vector2Int(-1, 0), new Vector2Int(0, 1));
            m_dirRightDict.Add(new Vector2Int(1, 0), new Vector2Int(0, -1));

            // 右后方
            m_dirRightDownDict = new Dictionary<Vector2Int, Vector2Int>();
            m_dirRightDownDict.Add(new Vector2Int(0, 1), new Vector2Int(1, -1));
            m_dirRightDownDict.Add(new Vector2Int(0, -1), new Vector2Int(-1, 1));
            m_dirRightDownDict.Add(new Vector2Int(-1, 0), new Vector2Int(1, 1));
            m_dirRightDownDict.Add(new Vector2Int(1, 0), new Vector2Int(-1, -1));

            // 右前方
            m_dirRightUpDict = new Dictionary<Vector2Int, Vector2Int>();
            m_dirRightUpDict.Add(new Vector2Int(0, 1), new Vector2Int(1, 1));
            m_dirRightUpDict.Add(new Vector2Int(0, -1), new Vector2Int(-1, -1));
            m_dirRightUpDict.Add(new Vector2Int(-1, 0), new Vector2Int(-1, 1));
            m_dirRightUpDict.Add(new Vector2Int(1, 0), new Vector2Int(1, -1));
        }


        /// <summary>
		///  对角距离.  八方向, 对角也是一格
		/// </summary>
		/// <returns></returns>
		protected double Diagonal(Vector2Int start, Vector2Int end)
        {
            //return Math.Max(Math.Abs(start.x - end.x), Math.Abs(start.y - end.y));
            var dx = Math.Abs(start.x - end.x);
            var dy = Math.Abs(start.y - end.y);

            // D * (dx + dy) + (D2 - 2 * D) * min(dx, dy);  // D--一格移动代价,  D2--对角移动代价
            return 1 * (dx + dy) + (1.4142136f - 2 * 1) * Math.Min(dx, dy);
        }

        /// <summary>
        ///  欧几里得距离.  任意方向
        /// </summary>
        protected double Euclidean(Point start, Point end)
        {
            int num = start.coord.x - end.coord.x;
            int num2 = start.coord.y - end.coord.y;
            return Math.Sqrt(num * num + num2 * num2);
        }

        /// <summary>
        ///  曼哈顿距离.  上下左右四方向
        /// </summary>
        protected double Manhattan(Point start, Point end)
        {
            return Math.Abs(start.coord.x - end.coord.x) + Math.Abs(start.coord.y - end.coord.y);
        }


        public List<AstarNode> GetPath(int[,] grids, Vector2Int start, Vector2Int end)
        {
            m_openList.Clear();
            m_closeList.Clear();
            m_grids = grids;
            m_end = end;
            m_countH = grids.GetLength(1);
            m_countV = grids.GetLength(0);

            // 加入起始点
            var sPoint = new Point() { coord = start, g = 0, dir = Vector2Int.zero };
            sPoint.f = Diagonal(sPoint.coord, end);
            m_openList.Add(sPoint);
            Point endPoint = null;


            while (m_openList.Count > 0)
            {
                double minVal = m_openList[0].f;
                int minIndex = 0;
                for (int i = 0; i < m_openList.Count; i++)
                {
                    if (m_openList[i].f < minVal)
                    {
                        minVal = m_openList[0].f;
                        minIndex = i;
                    }
                }


                var minPoint = m_openList[minIndex];
                if (minPoint.coord == end)  // 查找结束
                {
                    endPoint = minPoint;
                    break;
                }
                m_openList.RemoveAt(minIndex);
                m_closeList.Add(minPoint.coord);

                if (minPoint.dir == Vector2Int.zero) // 无移动方向时  八方向移动
                {
                    // 八方向搜索
                    for (int i = 0; i < m_moveDirs.Length; i++)
                    {
                        var curDir = m_moveDirs[i];
                        FindNextJumpPoint(minPoint, curDir);
                    }
                }
                else
                {
                    if (IsLineHV(minPoint.dir)) // 当前沿 直线方向搜索
                    {
                        var leftDownPoint = m_dirLeftDownDict[minPoint.dir] + minPoint.coord;
                        var leftPoint = m_dirLeftDict[minPoint.dir] + minPoint.coord;
                        if (CantGrid(leftDownPoint) && !CantGrid(leftPoint)) // 左后不可走 且 左方可走, 沿左方和左前方寻找
                        {
                            FindNextJumpPoint(minPoint, m_dirLeftDict[minPoint.dir]); // 左方
                            FindNextJumpPoint(minPoint, m_dirLeftUpDict[minPoint.dir]); // 左前方
                        }

                        if (!CantGrid(minPoint.dir + minPoint.coord)) FindNextJumpPoint(minPoint, minPoint.dir); // 前方

                        var rightDownPoint = m_dirRightDownDict[minPoint.dir] + minPoint.coord;
                        var rightPoint = m_dirRightDict[minPoint.dir] + minPoint.coord;
                        if (CantGrid(rightDownPoint) && !CantGrid(rightPoint)) // 右后不可走 且 右方可走, 沿右方和右前方寻找
                        {
                            FindNextJumpPoint(minPoint, m_dirRightDict[minPoint.dir]); // 右方
                            FindNextJumpPoint(minPoint, m_dirRightUpDict[minPoint.dir]); // 右前方
                        }
                    }
                    else // 对角方向搜索
                    {
                        var lineDir = new Vector2Int(minPoint.dir.x, 0); // 对角移动的 水平方向
                        if (!CantGrid(lineDir + minPoint.coord)) FindNextJumpPoint(minPoint, lineDir); // 沿 水平方向查找
                        if (!CantGrid(minPoint.dir + minPoint.coord)) FindNextJumpPoint(minPoint, minPoint.dir); // 沿 对角方向查找
                        lineDir = new Vector2Int(0, minPoint.dir.y); // 对角移动的 垂直方向
                        if (!CantGrid(lineDir + minPoint.coord)) FindNextJumpPoint(minPoint, lineDir); // 沿 垂直方向查找
                    }
                }

                if (endPoint != null) break;
            }

            List<AstarNode> path = new List<AstarNode>();
            path.Add(new AstarNode() { x = endPoint.coord.x, y = endPoint.coord.y, bid = 0 });

            var pre = endPoint.pre;
            while (pre != null)
            {
                path.Add(new AstarNode() { x = pre.coord.x, y = pre.coord.y, bid = 0 });
                pre = pre.pre;
            }

            return path;
        }

        /// <summary>
        ///  判断跳点
        ///     1. 起点和终点
        ///     2. 该点有强迫邻居节点. (强迫邻居节点: 邻居点有不可行走格子)
        ///              1. 左后方不可走 且 左方可走 (即 左方为 强迫邻居节点)
        ///              2. 右后方不可走 且 右方可走 (即 右方为 强迫邻居节点)
        ///     3. 对角线移动时, 该点在水平或垂直方向移动 能到达跳点
        /// </summary>
        protected bool IsJumpPoint(Vector2Int coord, Vector2Int moveDir)
        {
            if (m_end.x == coord.x && m_end.y == coord.y) return true;

            // 强迫邻居节点判断
            var leftDownPoint = m_dirLeftDownDict[moveDir] + coord;
            var leftPoint = m_dirLeftDict[moveDir] + coord;
            if (!IsOutGrid(leftDownPoint) && !IsOutGrid(leftPoint) && CantGrid(leftDownPoint) && !CantGrid(leftPoint))
            {
                return true;
            }

            var rightDownPoint = m_dirRightDownDict[moveDir] + coord;
            var rightPoint = m_dirRightDict[moveDir] + coord;
            if (!IsOutGrid(rightDownPoint) && !IsOutGrid(rightPoint) && CantGrid(rightDownPoint) && !CantGrid(rightPoint))
            {
                return true;
            }

            return false;
        }


        /// <summary>
        ///  是否 强迫邻居节点 (邻居里有不可行走点)
        /// </summary>
        protected bool IsStrongNeighbor(Vector2Int coord)
        {
            foreach (var dir in m_moveDirs)
            {
                var curCoords = dir + coord;
                if (!IsOutGrid(curCoords) && m_grids[curCoords.y, curCoords.x] > 0)
                {
                    return true;
                }
            }
            return false;
        }

        protected bool IsOutGrid(Vector2Int coord)
        {
            return coord.x < 0 || coord.y < 0 || coord.x >= m_countH || coord.y >= m_countV;
        }

        /// <summary>
        ///   格子不可走 或 超出范围
        /// </summary>
        protected bool CantGrid(Vector2Int coord)
        {
            return IsOutGrid(coord) || m_grids[coord.y, coord.x] > 0;
        }

        /// <summary>
        ///  是否是 直线方向 (水平或垂直)
        /// </summary>
        protected bool IsLineHV(Vector2Int dir)
        {
            return dir.x * dir.y == 0;
        }

        /// <summary>
        ///  寻找下一个跳点
        /// </summary>
        protected void FindNextJumpPoint(Point point, Vector2Int dir)
        {
            if (IsLineHV(dir)) // 直线方向
            {
                var nextCoord = point.coord + dir;
                Log($"直线方向:   {point.coord},  {nextCoord},  {CantGrid(nextCoord)}");
                while (!CantGrid(nextCoord))
                {
                    Log($"直线方向2:   {point.coord},  {nextCoord},  {IsStrongNeighbor(nextCoord)}");
                    if (IsJumpPoint(nextCoord, dir)) // 搜到 跳点,  加入 OpenList, 停止当前查询
                    {
                        var jumpPoint = new Point() { coord = nextCoord, g = point.g + Diagonal(point.coord, nextCoord), pre = point, dir = dir };
                        jumpPoint.f = jumpPoint.g + Diagonal(nextCoord, m_end);
                        AddOpenList(jumpPoint);
                        break;
                    }
                    nextCoord += dir;
                }
            }
            else // 对角方向.   沿水平和垂直方向搜索
            {
                var nextCoord = point.coord + dir;
                Log($"对角方向:   {point.coord},  {nextCoord},  {CantGrid(nextCoord)}");
                while (!CantGrid(nextCoord))
                {
                    if (nextCoord == m_end) // 找到终点
                    {
                        var jumpPoint = new Point() { coord = nextCoord, g = point.g + Diagonal(point.coord, nextCoord), pre = point };
                        jumpPoint.f = jumpPoint.g;
                        AddOpenList(jumpPoint);
                        break;
                    }

                    var hasAdd = false;
                    // 水平
                    var lineDir = new Vector2Int(dir.x, 0);
                    var lineDirCoord = nextCoord + lineDir;
                    Log($"对角水平方向:   {point.coord},  {nextCoord},  {lineDirCoord},  {CantGrid(lineDirCoord)}");
                    while (!CantGrid(lineDirCoord))
                    {
                        if (IsJumpPoint(lineDirCoord, lineDir)) // 搜到 跳点,  加入 OpenList, 停止当前查询
                        {
                            var jumpPoint = new Point() { coord = nextCoord, g = point.g + Diagonal(point.coord, nextCoord), pre = point, dir = dir };
                            jumpPoint.f = jumpPoint.g + Diagonal(nextCoord, m_end);
                            AddOpenList(jumpPoint);
                            hasAdd = true;
                            break;
                        }
                        lineDirCoord += lineDir;
                    }

                    if (!hasAdd)
                    {
                        // 垂直
                        lineDir = new Vector2Int(0, dir.y);
                        lineDirCoord = nextCoord + lineDir;
                        Log($"对角垂直方向:   {point.coord},  {nextCoord},  {lineDirCoord},  {CantGrid(lineDirCoord)}");
                        while (!CantGrid(lineDirCoord))
                        {
                            if (IsJumpPoint(lineDirCoord, lineDir)) // 搜到 跳点,  加入 OpenList, 停止当前查询
                            {
                                var jumpPoint = new Point() { coord = nextCoord, g = point.g + Diagonal(point.coord, nextCoord), pre = point, dir = dir };
                                jumpPoint.f = jumpPoint.g + Diagonal(nextCoord, m_end);
                                Debug.LogError($"{nextCoord},  {dir},   {point.g},  {point.coord},  {jumpPoint.g},  {jumpPoint.f}");
                                AddOpenList(jumpPoint);
                                break;
                            }
                            lineDirCoord += lineDir;
                        }
                    }

                    nextCoord += dir;
                }
            }
        }

        /// <summary>
        ///  根据新加入点的估值,  更新 OpenList 中已存在点的估值
        /// </summary>
        protected void AddOpenList(Point point)
        {
            Log($"OpenList:  {point.coord},  {point.dir},  {point.g},  {point.f}");
            if (m_closeList.Contains(point.coord)) return;

            m_openList.Add(point);
        }

        protected void Log(string ss)
        {
            if (m_Log) Debug.LogError(ss);
        }

        #endregion

    }
}