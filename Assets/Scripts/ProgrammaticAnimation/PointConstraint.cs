using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public enum ConstraintWeightType
{
    SelfWeight,//以当前点权重为基础进行约束
    TargetFullWeight,//以目标点完全权重为基础进行约束
    SelfFullWeight//以当前点完全权重为基础进行约束
}

//点与点之间的约束，可以设置与其他任意多个点之间的约束关系，包括最大距离、最小距离、固定距离、弹性回正、角度约束等，并可在编辑器视图中进行设置。
[System.Serializable]
public class PointConstraintData
{
    public ConstraintWeightType weightType;//约束权重类型
    public bool targetLookAtSelf;//约束目标点是否朝向当前点
    public PointConstraint target;//约束的目标点
    public float minDistance;//最小距离
    public float maxDistance;//最大距离
    public float fixedDistance;//固定距离，如果固定距离大于0，则忽略最小和最大距离
    [Range(0f, 1f)] public float elasticity;//弹性回正系数,大于0时启用弹性回正，数值越大回正越快，否则直接设置距离
    public bool applyAngleConstraint;//是否启用角度约束
    public enum AngleConstraintDirectionType
    {
        NextChainToTarget,//以约束目标点指向当前点的方向为参考
        TransformUp//以目标点的up方向为参考
    }
    public AngleConstraintDirectionType angleConstraintDirectionType;//角度约束参考方向类型
    public float angleConstraintRight;//顺时针方向角度约束
    public float angleConstraintLeft;//逆时针方向角度约束
    public bool useFixedAngleConstraint;//是否使用固定角度约束
    public float fixedAngleConstraint;//固定角度约束
}

public class PointConstraint : MonoBehaviour
{
    public bool autoUpdate = true; // 是否启用自身约束更新
    public bool coverUpdateWhenSmall = false; //若autoUpdate = true则显示该属性，在约束过小时不进行自动更新
    [Min(1)] public int solverIterations = 1; // 每帧解算迭代次数，越大链条越稳定
    [Range(0f, 1f)] public float selfWeight = 0.5f; // 双向影响时当前点权重

    public List<PointConstraintData> constraints = new List<PointConstraintData>();//约束数据数组
    public List<PointConstraint> parents = new List<PointConstraint>();//父级约束列表

    void Awake()
    {
        // 初始化父级约束列表
        if (constraints != null)
        {
            for (int i = 0; i < constraints.Count; i++)
            {
                if (constraints[i].target != null)
                {
                    constraints[i].target.parents.Add(this);
                }
            }
        }
    }

    void Update()
    {
        if (autoUpdate)
            OnUpdate(true);
    }

    public void OnUpdate(bool fromUpdate = false)
    {
        if (constraints == null || constraints.Count == 0) return; // 约束未启用直接返回

        int iterations = Mathf.Max(1, solverIterations);
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            SolveAllConstraints(fromUpdate);
        }
    }

    public void SolveEntireChainConstraints(bool fromUpdate = false)
    {
        if (constraints == null || constraints.Count == 0) return;

        PointConstraint current = this;
        current.SolveAllConstraints(fromUpdate);
        for (int i = 0; i < current.constraints.Count; i++)
        {
            if (current.constraints[i].target != null)
            {
                current = current.constraints[i].target;
                current.SolveEntireChainConstraints(fromUpdate);
            }
        }
    }

    public void SolveAllConstraints(bool fromUpdate = false)
    {
        if (constraints == null || constraints.Count == 0) return;

        for (int i = 0; i < constraints.Count; i++)
        {
            SolveOneConstraint(constraints[i], fromUpdate);
        }
    }

    public void SolveOneConstraint(PointConstraintData constraint, bool fromUpdate = false)
    {
        if (constraint.target == null) return;

        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = constraint.target.transform.position;

        Vector3 originDirection = currentPosition - targetPosition;
        float distance = originDirection.magnitude;

        if (fromUpdate && Mathf.Abs(distance - GetDistance(constraint, distance)) < 0.001f && coverUpdateWhenSmall)
        {
            return; // 如果是从Update调用且当前距离与约束距离非常接近，则不进行解算
        }

        if (distance < 0.001f)
        {
            originDirection = Vector3.right * 0.0001f;
            distance = 0.0001f;
        }

        Vector3 normalizedDirection = originDirection / distance;

        float desiredDistance = GetDistance(constraint, distance);

        Vector3 constrainedDirection = GetDirection(constraint, normalizedDirection);

        Vector3 desiredDelta = constrainedDirection * desiredDistance;

        Vector3 distanceToCorrect = desiredDelta - originDirection;

        float currentPointWeight;
        switch (constraint.weightType)
        {
            case ConstraintWeightType.SelfWeight:
                currentPointWeight = selfWeight;
                break;
            case ConstraintWeightType.TargetFullWeight:
                currentPointWeight = 0f;
                break;
            case ConstraintWeightType.SelfFullWeight:
                currentPointWeight = 1f;
                break;
            default:
                currentPointWeight = selfWeight;
                break;
        }
        float targetPointWeight = 1f - currentPointWeight;

        float stiffness = constraint.elasticity > 0f ? constraint.elasticity : 1f;

        if (currentPointWeight > 0f)
        {
            transform.position = currentPosition + distanceToCorrect * currentPointWeight * stiffness / solverIterations;
        }

        if (targetPointWeight > 0f)
        {
            constraint.target.transform.position = targetPosition - distanceToCorrect * targetPointWeight * stiffness / solverIterations;
        }

        //将targrt转向自己
        if (constraint.targetLookAtSelf)
        {
            constraint.target.transform.rotation = Quaternion.LookRotation(Vector3.forward, constrainedDirection);
        }
    }

    public float GetDistance(PointConstraintData constraint, float currentDistance)
    {
        if (constraint.fixedDistance > 0f)
        {
            return constraint.fixedDistance;
        }

        float minDistance = Mathf.Max(0f, constraint.minDistance);
        float maxDistance = constraint.maxDistance > 0f ? Mathf.Max(minDistance, constraint.maxDistance) : float.PositiveInfinity;

        return Mathf.Clamp(currentDistance, minDistance, maxDistance);
    }

    public Vector3 GetDirection(PointConstraintData constraint, Vector3 direction)
    {
        if (!constraint.applyAngleConstraint)
        {
            return direction;
        }

        PointConstraint targetConstraint = constraint.target;
        if (targetConstraint == null)
        {
            return direction;
        }

        Vector3 referenceDirection;

        if (constraint.angleConstraintDirectionType == PointConstraintData.AngleConstraintDirectionType.TransformUp)
        {
            referenceDirection = constraint.target.transform.up;
        }
        else if (targetConstraint.constraints.Count == 0 || targetConstraint.constraints[0].target == null)
        {
            return direction;
        }
        else
        {
            referenceDirection =  constraint.target.transform.position - targetConstraint.constraints[0].target.transform.position;
        }

        referenceDirection.Normalize();

        if (constraint.useFixedAngleConstraint)
        {
            return Quaternion.AngleAxis(constraint.fixedAngleConstraint, Vector3.back) * referenceDirection;
        }

        float maxRight = Mathf.Max(-180f, constraint.angleConstraintRight);
        float maxLeft = Mathf.Max(-180f, constraint.angleConstraintLeft);

        float angle = Vector3.SignedAngle(referenceDirection, direction, Vector3.back);//计算当前方向与参考方向之间的夹角，正值表示顺时针旋转，负值表示逆时针旋转

        float clampedAngle = Mathf.Clamp(angle, -maxLeft, maxRight);

        Vector3 constrainedDirection = Quaternion.AngleAxis(clampedAngle, Vector3.back) * referenceDirection;//将参考方向绕Z轴旋转clampedAngle度，得到约束后的方向
        return constrainedDirection.normalized;
    }

    public void SetPositionToOrigin(Vector3 targetPosition)
    {
        if (targetPosition != null)
        {
            transform.position = targetPosition;
        }
    }

    public void ApproachTarget(Vector3 targetPosition)
    {
        transform.position = targetPosition;
        for (int i = 0; i < solverIterations; i++)
        {
            SolveEntireChainConstraints();
        }
    }

    public void ReachTargetWithoutEnd(Vector3 targetPosition)
    {
        
        for (int i = 0; i < solverIterations; i++)
        {
            transform.position = targetPosition;
            SolveEntireChainConstraints();
        }
    }

    public void ReachTargetWithEnd(Vector3 targetPosition)
    {
        for (int i = 0; i < solverIterations; i++)
        {
            if (i < solverIterations / 2 || solverIterations == 1)
            {
                transform.position = targetPosition;
            }
            SolveEntireChainConstraints();
        }
    }
}
