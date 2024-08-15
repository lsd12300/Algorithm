using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Algorithm
{
    /// <summary>
    ///  工具类.  视椎体相关
    /// </summary>
    public static partial class Utils
    {

        /// <summary>
        ///  平面
        /// </summary>
        public class Plane
        {
            public Vector3 Normal; // 单位法线向量
            public float D2Origin; // 平面沿法线方向到原点的距离


            /// <summary>
            ///  从平面一般方程构建
            /// </summary>
            public static Plane Create(float A, float B, float C, float D)
            {
                var k = 1.0f / Mathf.Sqrt(A * A + B * B + C * C);
                var p = new Plane();
                p.Normal = new Vector3(A * k, B * k, C * k);
                p.D2Origin = D * k;
                return p;
            }
        }


        /// <summary>
        ///  从矩阵中提取视椎体6个平面.
        ///     平面方程: Ax + By + Cz + D = 0
        ///     
        ///     推导:
        ///         MV = V'.  向量V=(x,y,z,w),  向量V'=(x',y',z',w')
        ///         裁剪空间内 位于视椎体内则有  -w' < x' < w'; -w' < y' < w';  -w' < z' < w';
        ///         其中 在左平面右侧时 满足
        ///             -w' < x' ==> 
        ///             -(V * M4) < V * M1 ==> 
        ///             0 < V * (M4 + M1)
        ///         所以可得左平面方程 V * (M4 + M1) = 0  ==> x(M41 + M11) + y(M42 + M12) + z(M43 + M13) + w(M44 + M14) = 0
        ///             因为 w=1, 故平面方程为  x(M41 + M11) + y(M42 + M12) + z(M43 + M13) + (M44 + M14) = 0
        /// 
        ///     结论:
        ///         上平面  x(M41 - M21) + y(M42 - M22) + z(M43 - M23) + (M44 - M24) = 0
        ///         下平面  x(M41 + M21) + y(M42 + M22) + z(M43 + M23) + (M44 + M24) = 0
        ///         左平面  x(M41 + M11) + y(M42 + M12) + z(M43 + M13) + (M44 + M14) = 0
        ///         右平面  x(M41 - M11) + y(M42 - M12) + z(M43 - M13) + (M44 - M14) = 0
        ///         前平面  x(M41 + M31) + y(M42 + M32) + z(M43 + M33) + (M44 + M34) = 0
        ///         后平面  x(M41 - M31) + y(M42 - M32) + z(M43 - M33) + (M44 - M34) = 0
        /// 
        /// 
        /// 
        ///     实际用处:
        ///         当矩阵M 是投影矩阵P时,  裁剪面是相机空间的
        ///         当矩阵M 是观察矩阵V和投影矩阵P的组合(M=VP)时,  裁剪面是世界空间的
        ///         当矩阵M 是世界矩阵W,观察矩阵V和投影矩阵P的组合(M=WVP)时,  裁剪面是模型空间的
        /// 
        /// </summary>
        /// <param name="mt"></param>
        /// <returns></returns>
        public static float[] GetPlaneFromMatrix(Matrix4x4 mt)
        {
            var ret = new float[4 * 6]; // 6个平面, 每个平面4个参数

            // 按 上下左右前后 顺序
            var index = 0;
            // 上
            ret[index++] = mt.m30 - mt.m10;
            ret[index++] = mt.m31 - mt.m11;
            ret[index++] = mt.m32 - mt.m12;
            ret[index++] = mt.m33 - mt.m13;
            // 下
            ret[index++] = mt.m30 + mt.m10;
            ret[index++] = mt.m31 + mt.m11;
            ret[index++] = mt.m32 + mt.m12;
            ret[index++] = mt.m33 + mt.m13;
            // 左
            ret[index++] = mt.m30 + mt.m00;
            ret[index++] = mt.m31 + mt.m01;
            ret[index++] = mt.m32 + mt.m02;
            ret[index++] = mt.m33 + mt.m03;
            // 右
            ret[index++] = mt.m30 - mt.m00;
            ret[index++] = mt.m31 - mt.m01;
            ret[index++] = mt.m32 - mt.m02;
            ret[index++] = mt.m33 - mt.m03;
            // 前
            ret[index++] = mt.m30 + mt.m20;
            ret[index++] = mt.m31 + mt.m21;
            ret[index++] = mt.m32 + mt.m22;
            ret[index++] = mt.m33 + mt.m23;
            // 后
            ret[index++] = mt.m30 - mt.m20;
            ret[index++] = mt.m31 - mt.m21;
            ret[index++] = mt.m32 - mt.m22;
            ret[index++] = mt.m33 - mt.m23;

            return ret;
        }



        /// <summary>
        ///  视椎体和球体相交
        ///     球心到平面的距离
        ///         绝对值 如果小于半径,  则平面和球体相交
        ///         大于0, 在平面正面一侧(可能位于视椎体内部,  需要检查6个面).
        ///         小于0, 在平面背面一侧 且 距离超过球体半径时,  此时肯定不会和视椎体有交集
        ///         
        ///         公式
        ///             C--球心坐标 Vector3
        ///             N--平面法线 Vector3
        ///             D--平面沿法线与原点的距离 float
        ///             
        ///             球心到平面的距离 = Dot(C, N) + D
        ///                 解析:  Dot(C, N) 表示球心投影到平面法线上, 即 球心沿平面法线到原点的距离
        ///     
        /// </summary>
        /// <param name="center">球心坐标</param>
        /// <param name="radius">球体半径</param>
        /// <param name="frustum">视椎体6个平面</param>
        /// <returns></returns>
        public static bool FrusumIntersectSphere(Vector3 center, float radius, Plane[] frustum)
        {
            foreach (Plane p in frustum)
            {
                // 球心到平面的距离
                var sphere2PlaneDis = Vector3.Dot(p.Normal, center) + p.D2Origin;

                // 在平面背面一侧 且距离超过球体半径,  则肯定不会相交
                if (sphere2PlaneDis < -radius) return false;


                // 平面与球体相交
                if (Mathf.Abs(sphere2PlaneDis) < radius) return true;
            }

            // 球体未与视椎体平面相交,  也未在视椎体平面外部,  则表示球体位于视椎体内部
            return true;
        }


        /// <summary>
        ///  归一化平面方程.
        ///     即 aX + bY + cZ + d = 0 其中 a*a + b*b + c*c = 1, 此时 d 表示 平面沿法线方向 到 原点的距离
        ///         令 k = sqrt(A*A + B*B + C*C)
        ///            a = A / k
        ///            b = B / k
        ///            c = C / k
        ///            d = D / k
        ///            
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <param name="D"></param>
        /// <returns></returns>
        public static (float, float, float, float) NormalizePlane(float A, float B, float C, float D)
        {
            var k = 1.0f / Mathf.Sqrt(A * A + B * B + C * C);
            return (A * k, B * k, C * k, D * k);
        }
    }
}
