using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

//挂载在点约束节点上，为骨骼链绘制线条
[RequireComponent(typeof(PointConstraint))]
public class LineController : MonoBehaviour
{
    public bool autoUpdate = true; //是否自动更新
    public string controllerName; // 效应器名称，便于在编辑器中识别和管理
    public Brain brain; // 效应器所在生物的决策中心引用
    public LineRenderer lineRenderer;
    public int insertPointCount = 0;//线条插值点数量，越大线条越平滑
    public float zOffset = 0.01f;//线条相对于约束点的z轴偏移，避免与约束点重叠时出现穿模问题
    List<Vector3> linePoints = new List<Vector3>();
    public bool isTargetDirection = true;//线条是否指向约束目标点，true时以constraints[0].target为目标点遍历划线，false时以parent[0]约束点为目标遍历画线
    public PointConstraint end;//线条的终点约束组件，如果不为null则以该约束组件为终点进行划线，否则以最后一个约束点为终点
    public bool applyCollider = false;//是否启用Collider组件，启用后会在运行时根据线条生成多边形Collider组件，碰撞体会随着线条变化而变化，适用于需要与线条进行物理交互的情况
    PolygonCollider2D polygonCollider2D;

    void Start()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
                Debug.LogError("没有设置LineRenderer组件");
        }
        if (applyCollider)
        {
            polygonCollider2D = gameObject.GetComponent<PolygonCollider2D>();
            if (polygonCollider2D == null)
            {
                polygonCollider2D = gameObject.AddComponent<PolygonCollider2D>();
            }
            Rigidbody2D rb = gameObject.GetComponent<Rigidbody2D>();
            if (rb == null)            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0;
            }
        }
        UpdateTransformList();
    }

    void Update()
    {
        if (!autoUpdate) return;
        OnUpdate();
    }

    public void OnUpdate()
    {
        UpdateTransformList();
        if (linePoints.Count == 0) return;
        lineRenderer.positionCount = linePoints.Count;
        for (int i = 0; i < linePoints.Count; i++)
        {
            lineRenderer.SetPosition(i, linePoints[i]);
        }
        if (applyCollider)
        {
            UpdateCollider();
        }
    }

    void UpdateCollider()
    {
        if (linePoints.Count < 2 || polygonCollider2D == null)
            return;

        //世界坐标转本地坐标
        List<Vector2> localPoints = new List<Vector2>(linePoints.Count);
        Transform localTransform = transform;
        foreach (Vector3 worldPoint in linePoints)
        {
            Vector3 localPoint = localTransform.InverseTransformPoint(worldPoint);
            localPoints.Add(new Vector2(localPoint.x, localPoint.y));
        }

        //计算每个点的累积距离和总长
        List<float> distances = new List<float>(localPoints.Count);
        float totalLength = 0f;
        distances.Add(0f);
        for (int i = 1; i < localPoints.Count; i++)
        {
            float segLength = Vector2.Distance(localPoints[i - 1], localPoints[i]);
            totalLength += segLength;
            distances.Add(totalLength);
        }
        if (totalLength < 0.001f) return;

        float widthMultiplier = lineRenderer.widthMultiplier;
        AnimationCurve widthCurve = lineRenderer.widthCurve;
        if (widthCurve == null || widthCurve.keys.Length == 0)
            widthCurve = AnimationCurve.Constant(0, 1, 1f);

        //生成左右轮廓（不包括端点）,前方是终点减起点，右侧是顺时针方向
        List<Vector2> leftPoints = new List<Vector2>();
        List<Vector2> rightPoints = new List<Vector2>();

        //先计算每个点的切线和半宽（包括端点），切线是朝向终点的
        List<Vector2> tangents = new List<Vector2>(localPoints.Count);
        List<float> halfWidths = new List<float>(localPoints.Count);
        for (int i = 0; i < localPoints.Count; i++)
        {
            Vector2 current = localPoints[i];
            float t = distances[i] / totalLength;
            float width = widthCurve.Evaluate(t) * widthMultiplier;
            float halfWidth = width * 0.5f;
            halfWidths.Add(halfWidth);

            Vector2 tangent;
            if (i == 0)
                tangent = (localPoints[1] - current).normalized;
            else if (i == localPoints.Count - 1)
                tangent = (current - localPoints[i-1]).normalized;
            else
            {
                Vector2 prevDir = (current - localPoints[i-1]).normalized;
                Vector2 nextDir = (localPoints[i+1] - current).normalized;
                tangent = (prevDir + nextDir).normalized;
                if (tangent == Vector2.zero) tangent = nextDir;
            }
            tangents.Add(tangent);
        }

        //生成左右轮廓点（去端点，端点用半圆代替）
        for (int i = 1; i < localPoints.Count - 1; i++)
        {
            Vector2 current = localPoints[i];
            Vector2 perp = new Vector2(-tangents[i].y, tangents[i].x).normalized;
            float half = halfWidths[i];
            leftPoints.Add(current + perp * half);
            rightPoints.Add(current - perp * half);
        }

        //生成起点半圆
        Vector2 startPos = localPoints[0];
        Vector2 startTangent = tangents[0];
        float startHalf = halfWidths[0];
        Vector2 startPerp = new Vector2(-startTangent.y, startTangent.x).normalized;
        List<Vector2> startCapPoints = GenerateSemiCircle(startPos, startTangent, startHalf, lineRenderer.numCapVertices, true);

        //生成终点半圆
        Vector2 endPos = localPoints[localPoints.Count - 1];
        Vector2 endTangent = tangents[localPoints.Count - 1];
        float endHalf = halfWidths[localPoints.Count - 1];
        Vector2 endPerp = new Vector2(-endTangent.y, endTangent.x).normalized;
        List<Vector2> endCapPoints = GenerateSemiCircle(endPos, endTangent, endHalf, lineRenderer.numCapVertices, false);

        //组装，起点半圆→左侧轮廓→终点半圆→右侧轮廓（逆序）
        List<Vector2> colliderVertices = new List<Vector2>();
        colliderVertices.AddRange(startCapPoints);
        colliderVertices.AddRange(leftPoints);
        colliderVertices.AddRange(endCapPoints);
        for (int i = rightPoints.Count - 1; i >= 0; i--)
            colliderVertices.Add(rightPoints[i]);


        if (colliderVertices.Count < 3) return;
        polygonCollider2D.SetPath(0, colliderVertices.ToArray());
    }

    List<Vector2> GenerateSemiCircle(Vector2 center, Vector2 tangent, float halfWidth, int numVertices, bool isStart)
    {
        if (isStart)
            tangent = -tangent; //起点半圆朝向外部，反转切线方向
        float tangentAngle = Mathf.Atan2(tangent.y, tangent.x);//转化为直角坐标系下的弧度
        float leftAngle = tangentAngle + Mathf.PI / 2f;//左
        float rightAngle = tangentAngle - Mathf.PI / 2f;//右
        float startAngle = leftAngle;
        float endAngle = rightAngle;
        //确定生成点的数量（包括两端）
        //由于左右侧去除了端点，这里需要把端点加上
        int pointCount = Mathf.Max(2, numVertices + 2);
        List<Vector2> points = new List<Vector2>(pointCount);
        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1);
            float angle = startAngle - t * Mathf.PI;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            points.Add(center + dir * halfWidth);
        }
        return points;
    }

    Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 result = uuu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + ttt * p3;
        return result + Vector3.forward * zOffset;//添加z轴偏移
    }

    void AddBezierSegment(Transform previousPoint, Transform lastPoint, Transform currentPoint)
    {
        Vector3 startPosition = lastPoint.position;
        Vector3 endPosition = currentPoint.position;
        Vector3 segmentDirection = (endPosition - startPosition).normalized;
        Vector3 startDirection = previousPoint != null ? (startPosition - previousPoint.position).normalized : segmentDirection;

        float controlDistance = Vector3.Distance(startPosition, endPosition) / 3f;
        Vector3 controlPoint1 = startPosition + startDirection * controlDistance;
        Vector3 controlPoint2 = endPosition - segmentDirection * controlDistance;

        int sampleCount = Mathf.Max(1, insertPointCount);
        for (int i = 1; i <= sampleCount; i++)
        {
            float t = (float)i / (sampleCount + 1);
            linePoints.Add(GetBezierPoint(startPosition, controlPoint1, controlPoint2, endPosition, t));
        }

        linePoints.Add(endPosition);
    }

    public void UpdateTransformList()
    {
        linePoints.Clear();
        PointConstraint curPoint = GetComponent<PointConstraint>();
        PointConstraint lastPoint = null;
        PointConstraint previousPoint = null;
        if (curPoint == null) return;
        if (isTargetDirection)
        {
            if (curPoint.constraints.Count > 0 && curPoint.constraints[0].target != null)
            {
                while (curPoint != null)
                {
                    if (lastPoint != null)//在两点之间插入insertPointCount点，采用三次贝塞尔曲线
                    {
                        AddBezierSegment(previousPoint != null ? previousPoint.transform : null, lastPoint.transform, curPoint.transform);
                    }
                    else
                    {
                        linePoints.Add(curPoint.transform.position);
                    }

                    previousPoint = lastPoint;
                    lastPoint = curPoint;

                    if (curPoint != null && curPoint.constraints.Count > 0)
                    {
                        curPoint = curPoint.constraints[0].target;
                    }
                    else
                    {
                        curPoint = null;
                    }
                    if (end != null && curPoint == end)
                    {
                        linePoints.Add(end.transform.position);
                        break;
                    }
                }
            }
        }
        else
        {
            if (curPoint.parents.Count > 0)
            {
                while (curPoint != null)
                {
                    if (lastPoint != null)
                    {
                        AddBezierSegment(previousPoint != null ? previousPoint.transform : null, lastPoint.transform, curPoint.transform);
                    }
                    else
                    {
                        linePoints.Add(curPoint.transform.position);
                    }

                    previousPoint = lastPoint;
                    lastPoint = curPoint;

                    if (curPoint != null && curPoint.parents.Count > 0)
                    {
                        curPoint = curPoint.parents[0];
                    }
                    else
                    {
                        curPoint = null;
                    }
                    if (end != null && curPoint == end)
                    {
                        linePoints.Add(end.transform.position);
                        break;
                    }
                }
            }
        }
    }
}
