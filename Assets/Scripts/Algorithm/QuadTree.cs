using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 算法命名空间
/// </summary>
namespace Algorithm
{
    /// <summary>
    ///  四叉树 算法类， 管理对象， 可用于碰撞检测
    ///     将平面划分为 四块（右上角为 1， 逆时针顺序 左上角为 2， 左下角为 3， 右下角为 4），根据对象所在位置 分别划分进四个块根节点中
    ///         碰撞检测时根据对象位置获取所在块根节点， 并检测所在根节点中的所有对象与当前对象的 位置关系判断 碰撞情况
    ///         （当节点的对象超过一定数量时 将节点继续出四个子节点进行管理， 防止节点对象太多管理复杂）
    /// </summary>
    public class QuadTree {

        private QuadTreeNode _rootNode;
        private Dictionary<string, QuadTreeObj> _quadObjs;          // 对象,  key 为 GameObject 名称
        private List<QuadTreeObj> _findCollObjLists;                // 检测碰撞时 用于保存全部需要检测的对象数组


        public QuadTree()
        {
            _quadObjs = new Dictionary<string, QuadTreeObj>();
            _findCollObjLists = new List<QuadTreeObj>();
        }


        /// <summary>
        ///  设置四叉树根节点数据
        /// </summary>
        /// <param name="rightPosX"></param>
        /// <param name="upPosY"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetRootNode(float rightPosX, float upPosY, float width, float height)
        {
            if (_rootNode != null)
            {
                _rootNode.Clean();
            }
            _rootNode = new QuadTreeNode(rightPosX, upPosY, width, height, 0);
        }


        /// <summary>
        ///  添加新的对象到 四叉树中
        /// </summary>
        /// <param name="rightPosX"></param>
        /// <param name="upPosY"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void AddObjToTree(float rightPosX, float upPosY, float width, float height, string name)
        {
            _quadObjs.Add(name, new QuadTreeObj(rightPosX, upPosY, width, height, name));
            _rootNode.AddRectToNode(_quadObjs[name]);
        }
        public void AddObjToTree(Vector3 pos, float width, float height, string name)
        {
            AddObjToTree(pos.x + 0.5f * width, pos.y + 0.5f * height, width, height, name);
        }

        /// <summary>
        ///  获取和对象产生碰撞的 所有对象
        /// </summary>
        /// <param name="rightPosX"></param>
        /// <param name="upPosY"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public List<QuadTreeObj> GetColliObjs(float rightPosX, float upPosY, float width, float height, string objID)
        {
            return GetColliObjs(new QuadTreeObj(rightPosX, upPosY, width, height, objID));
        }
        public List<QuadTreeObj> GetColliObjs(QuadTreeObj obj)
        {
            _findCollObjLists.Clear();
            _rootNode.FindObjInQuadNode(obj, _rootNode, _findCollObjLists);
            Debug.Log("查询结果对象数量=" + _findCollObjLists.Count);
            return _findCollObjLists;
        }


        /// <summary>
        ///  输出树结构
        /// </summary>
        public void LogAllTree()
        {
            Debug.Log("root");
            LogTreeNode(_rootNode);
        }
        public void LogTreeNode(QuadTreeNode node)
        {
            Debug.Log("节点");
            Debug.Log("节点管理对象数量=" + node.ManageObjCount);
            if (node.ChildNodes != null)
            {
                int childCount = node.ChildNodes.Count;
                for (int i = 0; i < childCount; i++)
                {
                    LogTreeNode(node.ChildNodes[i]);
                }

            }
            else
            {
                Debug.Log("叶节点");
            }
        }
    }

    /// <summary>
    /// 四叉树节点类， 存储 所管理对象， 子节点对象， 区域矩阵数据等
    /// 
    /// </summary>
    public class QuadTreeNode
    {
        public List<QuadTreeNode> ChildNodes;           // 子节点对象数组
        public List<QuadTreeObj> ManageObjs;            // 管理的对象
        public QuadTreeBound Rect;                      // 节点空间矩阵
        public int DeepLevel;                           // 节点深度， 树根节点为 0
        public static int MaxObjCount = 10;             // 节点最多管理对象数， 超过则将本节点细分出四个空间
        public static int MaxDeepLevel = 5;             // 节点最大深度，超过则不进行细分

        public void Clean()
        {
            if (ChildNodes != null)
            {
                int childNodeCount = ChildNodes.Count;
                for (int i = 0; i < childNodeCount; i++)
                {
                    ChildNodes[i].Clean();
                }
            }
            ManageObjs = null;
            ChildNodes = null;
            Rect = null;
        }


        /// <summary>
        ///  传入节点管理区域信息
        /// </summary>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        public QuadTreeNode(float posX, float posY, float w, float h, int level)
        {
            Rect = new QuadTreeBound(posX, posY, w, h);
            DeepLevel = level;
            ManageObjs = new List<QuadTreeObj>();
        }


        /// <summary>
        /// 判断对应矩阵是否在当前区域内,  左右边界有一个在区域内, 上下边界有一个在区域内 则判定检测的矩阵在区域内
        ///     默认 被检测的对象比区域小
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public bool IsRectInArea(QuadTreeBound rect)
        {
            // 判断 左边界和右边界 是否有一个在区域内
            if (((rect.Left >= Rect.Left && rect.Left < Rect.Right) || (rect.Right > Rect.Left && rect.Right <= Rect.Right)) &&
                ((rect.Down >= Rect.Down && rect.Down < Rect.Up) || (rect.Up > Rect.Down && rect.Up <= Rect.Up)))
            {
                return true;
            }
            return false;
        }
        public bool IsRectInArea(float x, float y, float w, float h)
        {
            return IsRectInArea(new QuadTreeBound(x, y, w, h));
        }


        /// <summary>
        ///  将对象添加到区域内,  如果 当前节点深度等级已达到最大值 则直接加入, 否则 如果区域管理对象数量已达到最大 则分割区域为四块并调整各块管理对象, 其他情况直接加入
        ///     防止树的深度太大影响检索效率
        /// </summary>
        /// <param name="quadObj"></param>
        public void AddRectToNode(QuadTreeObj quadObj)
        {
            if (DeepLevel >= MaxDeepLevel)
            {
                ManageObjs.Add(quadObj);                // 直接加入
            }
            else
            {
                if (ChildNodes == null || ChildNodes.Count <= 0)        // 不存在子区域, 检测管理数量上限
                {
                    if (ManageObjCount >= MaxObjCount)          // 超过管理上限  必须加入子节点中
                    {
                        SplitToFourNode();                          // 细分出子节点
                        AddRectToChildNode(quadObj);                // 将对象加入子节点中
                    }
                    else
                    {
                        ManageObjs.Add(quadObj);                    // 直接加入
                    }
                }
                else
                {
                    AddRectToChildNode(quadObj);                    // 将对象加入子节点中
                }
            }
        }

        /// <summary>
        ///  对象 添加到子节点中,   如果仅在一个子节点区域中直接加入, 如果跨子节点区域加到 父节点中
        /// </summary>
        /// <param name="rect"></param>
        private void AddRectToChildNode(QuadTreeObj quadObj)
        {
            if (ChildNodes == null || ChildNodes.Count <= 0)
            {
                return;
            }
            int tInserId = -1;
            int tInsertCount = 0;
            for (int j = 0; j < 4; j++)
            {
                if (ChildNodes[j].IsRectInArea(quadObj.Rect))                         // 对象加入对应区域的节点内
                {
                    tInserId = j;
                    tInsertCount++;
                }
            }
            if (tInsertCount == 1)
            {
                ChildNodes[tInserId].AddRectToNode(quadObj);    // 仅在一个子区域内时直接  加入该子区域
            }
            else
            {
                ManageObjs.Add(quadObj);                // 对象不在子节点区域内 或者对象在两个子区域内  直接加入到本区域内
            }
        }

        /// <summary>
        ///  细分当前节点管理区域为四个子区域,  节点管理对象分别分入子节点中,  右上为 0 , 逆时针排序
        /// </summary>
        public void SplitToFourNode()
        {
            ChildNodes = new List<QuadTreeNode>(4);
            ChildNodes.Add(new QuadTreeNode(Rect.X, Rect.Y, Rect.W * 0.5f, Rect.H * 0.5f, DeepLevel + 1));                                          // 右上
            ChildNodes.Add(new QuadTreeNode(Rect.X - Rect.W * 0.5f, Rect.Y, Rect.W * 0.5f, Rect.H * 0.5f, DeepLevel + 1));                          // 左上
            ChildNodes.Add(new QuadTreeNode(Rect.X - Rect.W * 0.5f, Rect.Y - Rect.H * 0.5f, Rect.W * 0.5f, Rect.H * 0.5f, DeepLevel + 1));          // 左下
            ChildNodes.Add(new QuadTreeNode(Rect.X, Rect.Y - Rect.H * 0.5f, Rect.W * 0.5f, Rect.H * 0.5f, DeepLevel + 1));                          // 右下
            int objCount = ManageObjCount;
            for (int i = objCount-1; i >= 0; i--)                                   // 逆序 方便加入子节点的对象 移除
            {
                for (int j = 0; j < 4; j++)
                {
                    if (ChildNodes[j].IsRectInArea(ManageObjs[i].Rect))                         // 对象加入对应区域的节点内
                    {
                        ChildNodes[j].AddRectToNode(ManageObjs[i]);
                        ManageObjs.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///  获取给定对象所在的 四叉树节点,  用于从中获取要检测碰撞的对象
        ///     全部用矩阵碰撞盒子筛选碰撞数据
        ///     如果对象跨区域, 则分割对象, 然后分别在子区域内检测. 跨区域只可能是 两个或四个
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public void FindObjInQuadNode(QuadTreeObj obj, QuadTreeNode rootNode, List<QuadTreeObj> objList)
        {
            //Debug.Log("对象矩阵:  X=" + obj.Rect.X + ", Y=" + obj.Rect.Y + ", W=" + obj.Rect.W + ", H=" + obj.Rect.H);
            //Debug.Log("节点矩阵:  X=" + rootNode.Rect.X + ", Y=" + rootNode.Rect.Y + ", W=" + rootNode.Rect.W + ", H=" + rootNode.Rect.H);
            if (rootNode.IsRectInArea(obj.Rect))
            {
                if (rootNode.ChildNodes != null)
                {
                    int childCount = rootNode.ChildNodes.Count;
                    List<int> tInChildNodeId = new List<int>(4);
                    for (int i = 0; i < childCount; i++)
                    {
                        if (rootNode.ChildNodes[i].IsRectInArea(obj.Rect))
                        {
                            tInChildNodeId.Add(i);
                            //FindObjInQuadNode(obj, rootNode.ChildNodes[i], objList);
                        }
                    }
                    switch (tInChildNodeId.Count)
                    {
                        case 1:                         // 仅在一个子区域内, 直接递归检测该子节点
                            FindObjInQuadNode(obj, rootNode.ChildNodes[tInChildNodeId[0]], objList);
                            break;
                        case 2:                         // 跨两个区域
                        case 4:                         // 跨四个区域
                            var objSplitList = SplitObjWithNodeBound(obj, rootNode);    // 分割对象
                            int tCount = objSplitList.Count;
                            for (int i = 0; i < tCount; i++)
                            {
                                FindObjInQuadNode(objSplitList[i], rootNode, objList);  // 每个子对象分别检测
                            }
                            break;

                        default:
                            break;
                    }
                    objList.AddRange(rootNode.ManageObjs);
                }
                else
                {
                    objList.AddRange(rootNode.ManageObjs);           //没有子区域 又在本区域内, 直接加入本节点区域内的全部对象
                }
            }
        }

        /// <summary>
        ///  根据 节点内的区域边界线 将对象切割成 子区域内的对象, 由于都是矩形 所以结果就 一个或两个或四个
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="rootNode"></param>
        /// <returns></returns>
        public List<QuadTreeObj> SplitObjWithNodeBound(QuadTreeObj obj, QuadTreeNode node)
        {
            List<QuadTreeObj> tList = null;
            if (node.ChildNodes != null && node.ChildNodes.Count > 0)
            {
                float tMidX = node.Rect.Right - node.Rect.W * 0.5f;         // 四分象限 中间竖线
                float tMidY = node.Rect.Up - node.Rect.H * 0.5f;            // 四分象限 中间横线

                tList = obj.SplitObjRectWithTwoLine(tMidY, tMidX);
            }

            return tList;
        }


        /// <summary>
        ///  更新所有节点管理的对象状态, 如果对象已经出了象限 则重新执行插入操作 放入合适的节点中
        ///     找到叶子节点(没有子节点) 更新管理的对象位置, 然后再更新父节点管理的对象
        /// </summary>
        public void UpdateAllObjState(QuadTreeNode root, QuadTreeNode curCheckNode)
        {
            if (curCheckNode.ChildNodes != null)
            {
                int childNodeCount = curCheckNode.ChildNodes.Count;
                for (int i = 0; i < childNodeCount; i++)
                {
                    UpdateAllObjState(root, curCheckNode.ChildNodes[i]);
                }
            }
            int objCount = curCheckNode.ManageObjCount;
            for (int i = objCount; i >= 0; i--)
            {
                if (!curCheckNode.IsRectInArea(curCheckNode.ManageObjs[i].Rect))   // 不在之前节点内对象重新执行插入操作
                {
                    root.AddRectToNode(curCheckNode.ManageObjs[i]);
                    curCheckNode.ManageObjs.RemoveAt(i);
                }
            }
        }




        // 管理的对象数量
        public int ManageObjCount
        {
            get
            {
                if (ManageObjs == null)
                {
                    return 0;
                }
                return ManageObjs.Count;
            }
        }
    }

    /// <summary>
    ///  四叉树中的对象，  保存对象位置的矩阵数据（位置， 宽高）
    /// </summary>
    public class QuadTreeObj
    {
        public QuadTreeBound Rect;          // 位置矩阵
        public string ItemID;               // 对象标识


        public QuadTreeObj()
        { }

        public QuadTreeObj(float x, float y, float w, float h, string itemId)
        {
            Rect = new QuadTreeBound(x, y, w, h);
            ItemID = itemId;
        }

        public float RightPosX
        {
            get { return Rect.X; }
            set { Rect.X = value; }
        }
        public float UpPosY
        {
            get { return Rect.Y; }
            set { Rect.Y = value; }
        }
        public float Width
        {
            get { return Rect.W; }
            set { Rect.W = value; }
        }
        public float Height
        {
            get { return Rect.H; }
            set { Rect.H = value; }
        }

        /// <summary>
        ///  更新对象的位置,  objMidPosInRectCenter 为物体在位置矩阵中心的位置坐标, 需转化位置坐标为右上角
        /// </summary>
        /// <param name="objMidPosInRectCenter"></param>
        public void SetObjPosWithCenterPos(Vector3 objMidPosInRectCenter)
        {
            RightPosX = objMidPosInRectCenter.x + Width * 0.5f;
            UpPosY = objMidPosInRectCenter.y + Height * 0.5f;
        }

        /// <summary>
        ///  根据区域线分割对象, lineType 区域线类型(1 横线, 2 竖线),  如果对象没有被分割则返回对象
        /// </summary>
        /// <param name="linePos"></param>
        /// <param name="lineType"></param>
        /// <returns></returns>
        public List<QuadTreeObj> SplitObjRectWithLine(float linePos, int lineType)
        {
            List<QuadTreeObj> tList = new List<QuadTreeObj>(2);
            if (lineType == 1)                  // 横线 分割对象为 上下两子对象
            {
                if (UpPosY-Height < linePos && UpPosY > linePos)
                {
                    tList.Add(new QuadTreeObj(RightPosX, UpPosY, Width, UpPosY - linePos, ItemID)); //上对象
                    tList.Add(new QuadTreeObj(RightPosX, linePos, Width, linePos-(UpPosY-Height), ItemID)); //下对象
                }
            }
            else if (lineType == 2)             // 竖线 分割对象为 左右两子对象
            {
                if (RightPosX - Width < linePos && RightPosX > linePos)
                {
                    tList.Add(new QuadTreeObj(linePos, UpPosY, linePos - (RightPosX - Width), Height, ItemID)); //左对象
                    tList.Add(new QuadTreeObj(RightPosX, UpPosY, RightPosX-linePos, Height, ItemID)); //右对象
                }
            }
            if (tList.Count <= 0)
            {
                tList.Add(this);
            }
            return tList;
        }

        /// <summary>
        ///  使用 横竖 两个线分割对象,  linePosY 横线位置,  linePosX 竖线位置
        /// </summary>
        /// <param name="linePosY"></param>
        /// <param name="linePosX"></param>
        /// <returns></returns>
        public List<QuadTreeObj> SplitObjRectWithTwoLine(float linePosY, float linePosX)
        {
            List<QuadTreeObj> tRetLIst = new List<QuadTreeObj>(4);
            var tList = SplitObjRectWithLine(linePosY, 1);
            int count = tList.Count;
            for (int i = 0; i < count; i++)
            {
                tRetLIst.AddRange(tList[i].SplitObjRectWithLine(linePosX, 2));
            }
            return tRetLIst;
        }
    }

    /// <summary>
    ///  四叉树 位置矩阵， 包含位置（右上角位置）和宽高
    /// </summary>
    public class QuadTreeBound
    {
        public float X;                 // 位置
        public float Y;
        public float W;                 // 宽高
        public float H;

        public QuadTreeBound (float x, float y, float w, float h)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
        }

        public float Left
        {
            get { return X - W; }
        }
        public float Right
        {
            get { return X; }
        }
        public float Up
        {
            get { return Y; }
        }
        public float Down
        {
            get { return Y - H; }
        }
    }
}
