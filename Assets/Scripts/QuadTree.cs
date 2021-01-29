using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace QuadTree {
    public class QuadTree
    {
        
    }


    class TreeNode{
        public TreeNode childs;
        public Vector2 pos;
        public Vector2 size;
        public List<int> uid_list;


        public void SetRect(Vector2 pos, Vector2 size)
        {
            this.pos = pos;
            this.size = size;
        }

    }

    class TreeItem
    {

    }

    /// <summary>
    ///  物体的形状
    /// </summary>
    class Shape
    {

    }

    /// <summary>
    ///  矩形
    /// </summary>
    class ShapeRect : Shape
    {
        public Vector2 pos;
        public Vector2 size;


        public void SetRect(Vector2 pos, Vector2 size)
        {
            this.pos = pos;
            this.size = size;
        }
    }
}
