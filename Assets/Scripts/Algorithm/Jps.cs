using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Priority_Queue;


namespace Algorithm
{
    /// <summary>
    ///  Jps 寻路算法
    ///     基于 A* 的改进算法, 大部分情况下 比 A* 快一个数量级
    ///     
    ///  适用限制:
    ///     1. 无向无权重的规则网络
    ///     2. 网格节点 邻居点数量 小于等于8, 且 仅有可走和不可走两种状态
    ///     3. 每次 水平或垂直轴向移动代价为 1, 对角线移动代价为 √2
    ///     4. 不能穿越不可通行的网格节点
    ///     
    /// 
    ///     网格 左上角为 原点(0, 0)
    /// </summary>
    public class Jps
    {
        public static readonly Vector2Int NullVec = new Vector2Int(-1, -1);
        private readonly Jps_Grid _grid;
        private Vector2Int _start = NullVec;
        private Vector2Int _end = NullVec;

        // 估值函数 优先队列
        private readonly FastPriorityQueue<Jps_Node> _openSet;


        public Jps(Jps_Grid grid, int cap = 256)
        {
            _grid = grid;
            _openSet = new FastPriorityQueue<Jps_Node>(cap);
        }


        public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            if (!_grid.CanMove(start) || !_grid.CanMove(end))
                return null;
            _grid.Clear();
            _start = start;
            _end = end;
            _openSet.Clear();

            // 后面大括号内为  初始化列表
            var startNode = new Jps_Node(_start) { Cost=0,IsOpen=false,IsClose=false,IsForce=false };
            _openSet.Enqueue(startNode, startNode.Cost);
            // 每次取 估值函数最小的节点 检测
            while (_openSet.Count > 0)
            {
                Jps_Node cur = _openSet.Dequeue();
                cur.IsClose = true;
                if (cur.Pos == _end)
                    return Trace(cur);
                IdentitySuccessors(cur);
            }

            return null;
        }

        /// <summary>
        ///  搜索 后续跳点
        /// </summary>
        /// <param name="node"></param>
        private void IdentitySuccessors(Jps_Node node)
        {
            // 剪枝 获取所有需要检测的邻居点
            var neighbours = _grid.Neighbours(node);
            foreach (var n in neighbours)
            {
                Vector2Int jumpPos = Jump(node.Pos, n.Pos - node.Pos, 1);
                if (jumpPos == NullVec)
                    continue;

                Jps_Node jumpNode = _grid[jumpPos.x, jumpPos.y];
                if (jumpNode.IsClose)       // 跳点已检测过, 过滤
                    continue;

                float moveCost = (jumpPos - node.Pos).magnitude;
                float newCost = node.Cost + moveCost;
                if (!jumpNode.IsOpen)       // 判断加入 OpenSet中
                {
                    jumpNode.IsOpen = true;
                    jumpNode.Parent = node;
                    jumpNode.Cost = newCost;
                    _openSet.Enqueue(jumpNode, jumpNode.Cost);
                }
                // 更新代价
                else if (newCost < jumpNode.Cost)
                {
                    jumpNode.Cost = newCost;
                    jumpNode.Parent = node;
                    _openSet.UpdatePriority(jumpNode, newCost);
                }
            }
        }

        /// <summary>
        ///  获取跳点
        /// </summary>
        /// <param name="ndoe"></param>
        /// <param name="dir"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        private Vector2Int Jump(Vector2Int node, Vector2Int dir, int k)
        {
            Vector2Int next = node + dir * k;
            if (!_grid.CanMove(next))
            {
                return NullVec;
            }
            // 终点 也是跳点
            if (next == _end)
            {
                return next;
            }
            _grid[next.x, next.y].Parent = _grid[node.x, node.y];

            // 点存在 强迫邻居点, 则点为 跳点
            var l = _grid.Neighbours(_grid[next.x, next.y]);
            foreach (var n in l)
            //foreach (var n in _grid.Neighbours(_grid[next.x, next.y]))
            {
                if (n.IsForce)
                    return next;
            }

            // 对角线移动
            if (dir.x != 0 && dir.y != 0)
            {
                // 垂直轴向
                if (Jump(next, new Vector2Int(0, dir.y), 1) != NullVec)
                    return next;
                // 水平轴向
                if (Jump(next, new Vector2Int(dir.x, 0), 1) != NullVec)
                    return next;
                // 对角移动, 不经过障碍顶点,  在此处限制
                //if (_grid.CanMove(next.x, next.y + dir.y) && _grid.CanMove(next.x + dir.x, next.y))
                //    return Jump(next, dir, k + 1);
                //return NullVec;
            }

            // 继续当前方向搜索
            return Jump(node, dir, k + 1);
        }

        /// <summary>
        ///  回溯路径
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private List<Vector2Int> Trace(Jps_Node node)
        {
            var path = new List<Vector2Int>();
            path.Add(node.Pos);
            while (node.Parent != null)
            {
                node = node.Parent;
                path.Add(node.Pos);
            }
            path.Reverse();
            return path;
        }
    }

    /// <summary>
    ///  网格节点
    ///     继承 FastPriorityQueueNode
    ///     使用第三方库 优先队列
    /// </summary>
    public class Jps_Node : FastPriorityQueueNode
    {
        public Vector2Int Pos { get; private set; }

        public Jps_Node(Vector2Int vector2Int)
        {
            this.Pos = vector2Int;
        }

        public Jps_Node(int x, int y) : this(new Vector2Int(x, y)) { }

        // 移动消耗
        public float Cost { get; set; }

        // 上一个路径点
        public Jps_Node Parent { get; set; }

        public bool IsOpen { get; set; }

        // 已检测标记
        public bool IsClose { get; set; }

        // 强制邻居点  标记
        public bool IsForce { get; set; }

        public void Reset()
        {
            Cost = 0;
            Parent = null;
            IsClose = false;
            IsOpen = false;
            IsForce = false;
        }
    }

    /// <summary>
    ///  寻路整个网格
    ///     左上角为 原点(0, 0)
    /// </summary>
    public class Jps_Grid
    {
        private Vector2Int[] _dirs =
        {
            // 轴向
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),

            // 对角线
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1),
        };

        // 边界
        private readonly int _boundMaxX;
        private readonly int _boundMaxY;
        private readonly int _boundMinX;
        private readonly int _boundMinY;

        // 格子可行走标记
        private readonly bool[,] _gridFlags;
        private readonly Jps_Node[,] _grid;


        public Jps_Grid(bool[, ] flags)
        {
            _boundMinX = 0;
            _boundMaxX = flags.GetUpperBound(0);        // 数组长度 表示边界
            _boundMinY = 0;
            _boundMaxY = flags.GetUpperBound(1);
            _gridFlags = flags;

            _grid = new Jps_Node[_boundMaxX + 1, _boundMaxY + 1];
            for (int x = _boundMinX; x <= _boundMaxX; x++)
            {
                for (int y = _boundMinY; y <= _boundMaxY; y++)
                {
                    _grid[x, y] = new Jps_Node(x, y);
                }
            }
        }

        public void Clear()
        {
            if (_grid != null)
            {
                foreach (var item in _grid)
                {
                    if (item != null)
                        item.Reset();
                }
            }
        }

        public Jps_Node this[int x, int y] { get {
                return _grid[x, y];
            } }

        // 是否可走,  边界内 且 格子可走
        public bool CanMove(int x, int y)
        {
            return InBounds(x, y) && _gridFlags[x, y];
        }

        public bool CanMove(Vector2Int pos)
        {
            return InBounds(pos.x, pos.y) && _gridFlags[pos.x, pos.y];
        }

        public bool InBounds(int x, int y)
        {
            return x >= _boundMinX && x <= _boundMaxX &&
                   y >= _boundMinY && y <= _boundMaxY;
        }

        /// <summary>
        ///  获取 需要检测的所有邻居点
        ///     使用  轴向移动剪枝 和 对角线移动剪枝 优化查找的邻居点数量
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal List<Jps_Node> Neighbours(Jps_Node node)
        {
            List<Jps_Node> list = new List<Jps_Node>(8);
            if (node.Parent != null)
            {
                // 当前移动方向
                Vector2Int curDir = node.Pos - node.Parent.Pos;
                curDir.x /= curDir.x == 0 ? 1 : Mathf.Abs(curDir.x);
                curDir.y /= curDir.y == 0 ? 1 : Mathf.Abs(curDir.y);

                // 对角线移动
                //      检测  水平轴向 和 垂直轴向 及 当前对角方向 点
                if (curDir.x != 0 && curDir.y != 0)
                {
                    if (CanMove(node.Pos.x + curDir.x, node.Pos.y))
                        list.Add(this[node.Pos.x + curDir.x, node.Pos.y]);

                    if (CanMove(node.Pos.x, node.Pos.y + curDir.y))
                        list.Add(this[node.Pos.x, node.Pos.y + curDir.y]);

                    // 对角移动时 如果要确保不能经过 障碍格子顶点,  在此处添加限制
                    //  011
                    //  111
                    //  111
                    //      一般不能从[0,1] 走到 [1,0], 因为会经过障碍格子顶点
                    //if (CanMove(node.Pos.x, node.Pos.y + curDir.y) && CanMove(node.Pos.x + curDir.x, node.Pos.y) && CanMove(node.Pos.x + curDir.x, node.Pos.y + curDir.y))
                    if (CanMove(node.Pos.x + curDir.x, node.Pos.y + curDir.y))
                        list.Add(this[node.Pos.x + curDir.x, node.Pos.y + curDir.y]);
                }
                else if (curDir.x != 0)
                {
                    // 水平轴向移动  检测
                    //      1. 当前方向
                    //      2. 当前朝向左后方为障碍，左方可走，返回 左前方(普通邻居点) 和 左方点(强迫邻居点)
                    //      3. 当前朝向右后方为障碍，右方可走，返回 右前方(普通邻居点) 和 右方点(强迫邻居点)
                    if (CanMove(node.Pos.x + curDir.x, node.Pos.y))
                        list.Add(this[node.Pos.x + curDir.x, node.Pos.y]);

                    if (CanMove(node.Pos.x, node.Pos.y + 1) && !CanMove(node.Pos.x - curDir.x, node.Pos.y + 1))
                    {
                        list.Add(this[node.Pos.x, node.Pos.y + 1]);
                        this[node.Pos.x, node.Pos.y + 1].IsForce = true;        // 左方点为 强迫邻居点
                        if(CanMove(node.Pos.x + curDir.x, node.Pos.y + 1))
                            list.Add(this[node.Pos.x + curDir.x, node.Pos.y + 1]);
                    }

                    if (CanMove(node.Pos.x, node.Pos.y - 1) && !CanMove(node.Pos.x - curDir.x, node.Pos.y - 1))
                    {
                        list.Add(this[node.Pos.x, node.Pos.y - 1]);
                        this[node.Pos.x, node.Pos.y - 1].IsForce = true;        // 右方点为 强迫邻居点
                        if (CanMove(node.Pos.x + curDir.x, node.Pos.y - 1))
                            list.Add(this[node.Pos.x + curDir.x, node.Pos.y - 1]);
                    }
                }
                else
                {
                    // 垂直轴向移动
                    //      1. 当前方向
                    //      2. 当前朝向左后方为障碍，左方可走，返回 左前方(普通邻居点) 和 左方点(强迫邻居点)
                    //      3. 当前朝向右后方为障碍，右方可走，返回 右前方(普通邻居点) 和 右方点(强迫邻居点)
                    if (CanMove(node.Pos.x, node.Pos.y + curDir.y))
                        list.Add(this[node.Pos.x, node.Pos.y + curDir.y]);

                    if (CanMove(node.Pos.x - 1, node.Pos.y) && !CanMove(node.Pos.x - 1, node.Pos.y - curDir.y))
                    {
                        list.Add(this[node.Pos.x - 1, node.Pos.y]);
                        this[node.Pos.x - 1, node.Pos.y].IsForce = true;        // 左方点为 强迫邻居点
                        if (CanMove(node.Pos.x - 1, node.Pos.y + curDir.y))
                            list.Add(this[node.Pos.x - 1, node.Pos.y + curDir.y]);
                    }

                    if (CanMove(node.Pos.x + 1, node.Pos.y) && !CanMove(node.Pos.x + 1, node.Pos.y - curDir.y))
                    {
                        list.Add(this[node.Pos.x + 1, node.Pos.y]);
                        this[node.Pos.x + 1, node.Pos.y].IsForce = true;        // 右方点为 强迫邻居点
                        if (CanMove(node.Pos.x + 1, node.Pos.y + curDir.y))
                            list.Add(this[node.Pos.x + 1, node.Pos.y + curDir.y]);
                    }
                }
            }
            else
            {
                // 起点,  直接返回所有可移动点
                for (int i = 0; i < 8; i++)
                {
                    int x = node.Pos.x + _dirs[i].x;
                    int y = node.Pos.y + _dirs[i].y;
                    if (CanMove(x, y))
                    {
                        list.Add(this[x, y]);
                    }
                }
            }
            return list;
        }
    }
}