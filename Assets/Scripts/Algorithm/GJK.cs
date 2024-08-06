using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Algorithm
{

    /// <summary>
    ///  多边形.
    /// </summary>
    public class Shape
    {
        public List<Vector2> m_points; // 顶点顺时针方向

        /// <summary>
        ///  获取多边形的中心点
        /// </summary>
        /// <param name="cneter"></param>
        /// <returns></returns>
        public Vector2 Center()
        {
            if (m_points?.Count <= 0) return Vector2.zero;

            var center = Vector2.zero;
            foreach (var point in m_points)
            {
                center += point;
            }
            center /= m_points.Count;
            return center;
        }

        /// <summary>
        ///  求 多边形在给定方向上的 最远顶点
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public Vector2 GetFarestWithDir(Vector2 dir)
        {
            var min = float.MinValue;
            var p = Vector2.zero;
            foreach (var point in m_points)
            {
                var dot = Vector2.Dot(dir, point);
                if (dot > min)
                {
                    min = dot;
                    p = point;
                }
            }

            return p;
        }
    }


    /// <summary>
    ///  GJK 碰撞检测算法
    /// </summary>
    public class GJK
    {

        private List<Vector2> m_simplexShape = new List<Vector2>(); // 单纯形


        /// <summary>
        ///  检测两个多边形是否碰撞
        /// </summary>
        public bool Check(Shape s1, Shape s2)
        {
            m_simplexShape.Clear();
            // 两图形中心点方向 做初始迭代方向
            var dir = s2.Center() - s1.Center();
            dir.Normalize();

            while (true)
            {
                // 计算 Support点, 并判断是否跨原点
                var supPoint = Support(s1, s2, dir);


                // 点重复检测, 说明所有可能的单纯形都已检查过
                foreach (var point in m_simplexShape)
                {
                    if(point == supPoint) return false;
                }


                var dotSupport = Vector2.Dot(dir, supPoint);
                if (dotSupport < 0) // 未跨原点, 返回 false
                {
                    return false;
                }
                else
                {
                    m_simplexShape.Add(supPoint);
                }

                // 更新单纯形和迭代方向
                if (CheckSimplex(ref dir))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///  给定方向, 计算 明可夫斯基差图形上的点.
        ///     1. 对图形1 取给定方向上的最远点
        ///     2. 对图形2 取给定方向 反方向上的最远点
        ///     3. 两点相减即可
        /// </summary>
        private Vector2 Support(Shape s1, Shape s2, Vector2 dir)
        {
            var p1 = s1.GetFarestWithDir(dir);
            var p2 = s2.GetFarestWithDir(-dir);
            return p1 - p2;
        }

        /// <summary>
        ///  更新单纯形和迭代方向
        /// </summary>
        private bool CheckSimplex(ref Vector2 newDir)
        {
            var count = m_simplexShape.Count;

            if (count == 3) // 如果单纯形不包含原点, 则保留离原点最近的边 上的两个点 继续迭代
            {
                if (IsPointInPolygon(Vector2.zero, m_simplexShape))
                {
                    return true;
                }

                FindClosestEdgeToOrigin(m_simplexShape, out var index1, out var index2);
                var removeIndex = -1;
                for (int i = 0; i < count; i++)
                {
                    if (i != index1 && i != index2)
                    {
                        removeIndex = i;
                        break;
                    }
                }
                m_simplexShape.RemoveAt(removeIndex);
            }

            if (count == 2) // 使用两点直线的垂直线 过原点的方向, 作为新的迭代方向
            {
                newDir = GetPerpDirOriginSide(m_simplexShape[0], m_simplexShape[1]);
            }
            else if (count == 1) // 使用相反反向 为新迭代方向
            {
                newDir = -newDir;
            }

            return false;
        }


        /// <summary>
        ///  获取两点直线 朝原点侧的垂线.
        ///     使用两次叉乘即可  AB x AO x AB
        /// </summary>
        private Vector2 GetPerpDirOriginSide(Vector2 p1, Vector2 p2)
        {
            Vector3 dir1 = p2 - p1;
            Vector3 dir2 = Vector2.zero - p1;
            return Vector3.Cross(Vector3.Cross(dir1, dir2), dir1);
        }

        /// <summary>
        ///  查找离原点最近的边
        /// </summary>
        private void FindClosestEdgeToOrigin(List<Vector2> polygon, out int index1, out int index2)
        {
            index1 = index2 = 0;
            var dis = float.MaxValue;
            var count = polygon.Count;
            for (int i = 0, j = count-1; i < count; j=i++)
            {
                var dis2Edge = GetPoint2LineDis(Vector2.zero, polygon[i], polygon[j]);
                if (dis2Edge < dis)
                {
                    dis = dis2Edge;
                    index1 = i;
                    index2 = j;
                }
            }
        }


        #region 公共图形方法

        /// <summary>
        ///  判断点在多边形内.
        ///     射线法:
        ///         以被检测点为原点 朝任意方向(一般取水平向右) 作射线, 统计射线与多边形的交点数量, 为偶数 则在多边形外.
        ///         复杂度 O(n)
        ///         适用任意多边形
        /// </summary>
        private static bool IsPointInPolygon(Vector2 point, List<Vector2> polygon)
        {
            var flag = false;
            var count = polygon.Count;
            for (int i = 0, j = count-1; i < count; j = i++) // 遍历所有边 (一条边两个顶点)
            {
                var p1 = polygon[i];
                var p2 = polygon[j];
                if (IsPointOnSegment(point, p1, p2)) return true; // 点在边上

                // 前一个 等价 min(p1.y, p2.y) < point.y <= max(p1.y, p2.y)
                // 后一个 判断 点在射线与边交点的左边 (因为射线水平向右, 所以起点在左边时  才有可能相交)
                //      使用直线斜率方式算交点坐标
                //      由于射线水平向右, 如果 p1.y-p2.y==0 则是水平线, 即射线在边上.  上面已经处理了
                if ((p1.y - point.y > 0 != p2.y - point.y > 0)
                    && (point.x - (point.y - p1.y) * (p1.x - p2.x) / (p1.y - p2.y) - p1.x) < 0)
                {
                    flag = !flag;
                }
            }
            return flag;
        }

        /// <summary>
        ///  判断点在线段上
        /// </summary>
        private static bool IsPointOnSegment(Vector2 point, Vector2 p1, Vector2 p2)
        {
            var p1p = p1 - point;
            var p2p = p2 - point;

            var cross = p1p.x * p2p.y - p1p.y * p2p.x; // 二维向量 叉乘的模

            // 叉乘判断共线,  点乘判断在两点范围内
            //  叉乘Sin==0, 说明角度 0或180 共线. 
            //  点乘 x1x2 + y1y2.  共线的情况下, 如果点在线段外, 则x1和x2, y1和y2同号, 必大于0
            //                                   如果点在线段内, 则x1和x2, y1和y2异号, 结果小于等于0
            return cross == 0 && Vector2.Dot(p1p, p2p) <= 0;
        }


        /// <summary>
        ///  点到直线的距离.
        ///     假设 直线公式 Ax + By + C = 0, 则点到直线的距离 d = abs(Axp + Byp + C) / Sqrt(A*A + B*B),  点(xp, yp)
        /// </summary>
        /// <param name="point"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        private static float GetPoint2LineDis(Vector2 point, Vector2 p1, Vector2 p2)
        {
            if (p2.x - p1.x == 0)
            {
                return Mathf.Abs(point.x - p1.x);
            }

            var k = (p2.y - p1.y) / (p2.x - p1.x);
            var c = p1.y - k * p1.x; // A = k, B = -1
            return Mathf.Abs(k * point.x - point.y + c) / Mathf.Sqrt(k*k + 1);
        }
        #endregion
    }
}