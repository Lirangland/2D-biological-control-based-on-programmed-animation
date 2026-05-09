using System.Collections.Generic;
using UnityEngine;

public class TentacleEnd : Effector
{
    public List<PointConstraint> pointConstraints = new List<PointConstraint>();//储存整条触手的节点
    bool isMoving = false;//是否正在移动

    public void StartMove()
    {
        transform.GetComponent<PointConstraint>().constraints[0].weightType = ConstraintWeightType.TargetFullWeight;
        foreach (var constraint in pointConstraints)
        {
            constraint.constraints[0].fixedDistance = 0.1f;
        }
    }

    public void EndMove()
    {
        transform.GetComponent<PointConstraint>().constraints[0].weightType = ConstraintWeightType.SelfWeight;
        foreach (var constraint in pointConstraints)
        {
            constraint.constraints[0].fixedDistance = 0f;
        }
    }
    
}
