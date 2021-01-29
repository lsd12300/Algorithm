using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Algorithm
{

    /// <summary>
    ///  Kd树  最邻近查询
    /// </summary>
    public class KDTree
    {
        public int dimenCount = 3;      // 维度总数


        /// <summary>
        /// 分割数据创建树
        ///     每次分割时  取方差最大的维度 的中间值 为根节点, 将数据划分到左右子树上
        /// </summary>
        /// <param name="data"></param>
        public void Build(Vector3[] data)
        {

        }

        private KdNode CreateNode(int dimen, List<Vector3> data)
        {
            if (data == null || data.Count <= 0)
            {
                return null;
            }

            data.Sort((Vector3 a, Vector3 b) =>{    
                return a[dimen] > b[dimen] ? 1 : -1;
            });
            int next_dimen = (dimen + 1) % dimenCount;
            int mid_id = Mathf.FloorToInt(data.Count / 2);
            List<Vector3> left = new List<Vector3>();
            List<Vector3> right = new List<Vector3>();
            for (int i = 0; i < mid_id; i++)
            {
                left.Add(data[i]);
            }
            for (int i = mid_id+1; i < data.Count; i++)
            {
                right.Add(data[i]);
            }
            

            return null;
        }
    }

    public class KdNode
    {
        public int dimen;           // 分割的 维度
        public Vector3 data;
        public KdNode left;
        public KdNode right;

        public KdNode(int d, Vector3 data, KdNode l, KdNode r)
        {
            this.dimen = d;
            this.data = data;
            this.left = l;
            this.right = r;
        }
    }
}