using UnityEngine;

namespace Tutorial203
{
    // Cubic Bezier
    public class BezierTest : MonoBehaviour
    {
        // 控制点
        public Transform p0;
        public Transform p1;
        public Transform p2;
        public Transform p3;

        public int   segments = 20; // 曲线分成多少小线段（越大越平滑）
        public float gizmoSize = 0.1f; // 控制点大小
        public Color curveColor = Color.green; // 曲线颜色
        public Color controlPointColor = Color.yellow; // 控制点颜色
        //三次贝塞尔曲线公式实现
        public static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float omt  = 1f - t;
            float omt2 = omt * omt;
            float t2   = t * t;

            return p0 * (omt * omt2) +
                   p1 * (3f * omt2 * t) +
                   p2 * (3f * omt * t2) +
                   p3 * (t * t2);
        }

        private void OnDrawGizmos()
        {
            if (p0 == null || p1 == null || p2 == null || p3 == null)
            {
                return;
            }
            //画控制点
            Gizmos.color = controlPointColor;
            Gizmos.DrawSphere(p0.position, gizmoSize);
            Gizmos.DrawSphere(p1.position, gizmoSize);
            Gizmos.DrawSphere(p2.position, gizmoSize);
            Gizmos.DrawSphere(p3.position, gizmoSize);
            //画控制点连线
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(p0.position, p1.position);
            Gizmos.DrawLine(p1.position, p2.position);
            Gizmos.DrawLine(p2.position, p3.position);
            //画贝塞尔曲线
            Gizmos.color = curveColor;
            Vector3 previousPoint = p0.position;
            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 currentPoint = CubicBezier(p0.position, p1.position, p2.position, p3.position, t);
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }


    }
}
