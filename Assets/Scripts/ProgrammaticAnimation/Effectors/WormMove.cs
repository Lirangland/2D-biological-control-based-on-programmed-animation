using UnityEngine;
using System.Collections;
using Unity.VisualScripting;


//蠕虫的运动组件，挂载在尾部体节，通过以自身位置向parent方向遍历点约束列表、修改点约束的固定距离来实现蠕动运动效果
//当一次蠕动结束后才会进行下一次蠕动
public class WormMove : Effector
{
    public Vector3 targetPosition;//目标位置
    public Transform headTransform;//头部体节的transform组件
    public float moveDistance = 0.5f;//每次蠕动的移动距离
    public float moveInterval = 0.3f;//每次蠕动的间隔时间
    [Range(0f, 100f)] public float turnSpeed = 50f;//头部体节转向目标的速度，百分比形式

    public float stopDistance = 0.3f;//距离目标多少范围内停止移动
    public int moveBackupCount = 5;//若多次达到turnSpeed%的约束角度，则进行一次无法被外部打断的从头部到尾部的后退移动，目标位置为头部位置减当前移动目标位置方向上最远7个体节的位置，直到后退移动完成才会进行下一次正常移动
    public int backMoveCount = 5;//后退次数

    [SerializeField] int turnSpeedCount = 0;//达到turnSpeed%的约束角度的次数

    bool isMoving = false;//是否正在蠕动
    SerializableDic<PointConstraint, float> weightBackup = new SerializableDic<PointConstraint, float>(); //权重备份，用于恢复约束权重

    PointConstraint curPoint; //当前体节的点约束组件
    PointConstraint parentPoint; //父级体节的点约束组件

    void Start()
    {
        if (headTransform == null)
        {
            Debug.LogError("没有设置头部节点");
        }
    }

    void AddFixedDistance(PointConstraint pointConstraint, float delta)
    {
        if (pointConstraint == null || pointConstraint.constraints == null || pointConstraint.constraints.Count == 0)
            return;

        pointConstraint.constraints[0].fixedDistance += delta;
    }

    public override void OnUpdate()
    {
        if (Vector3.Distance(headTransform.position, targetPosition) <= stopDistance)
        {
            targetPosition = Vector3.zero;
            return;
        }
        if (targetPosition == Vector3.zero || isMoving || brain.GetBoolState("IsBacking")) return;
        StartCoroutine(MoveCoroutine());
    }

    IEnumerator MoveCoroutine()
    {
        isMoving = true;
        curPoint = GetComponent<PointConstraint>();
        parentPoint = curPoint.parents.Count > 0 ? curPoint.parents[0] : null;
        while (curPoint != null)
        {
            //将当前parent点约束的权重置0、固定距离减少moveDistance，并将当前点固定距离增加moveDistance
            if (parentPoint != null)
            {
                weightBackup.Add(parentPoint, parentPoint.selfWeight);
                parentPoint.selfWeight = 0f;
                AddFixedDistance(parentPoint, -moveDistance);
            }
            else{//到了头部体节位置
                curPoint.selfWeight = 1f;
                //将头部体节转向目标与子体节的连线上
                Vector3 direction = (targetPosition - curPoint.constraints[0].target.transform.position).normalized;
                //Debug.Log("目标方向：" + direction);
                Vector3 beforeDirection = direction;
                direction = curPoint.GetDirection(curPoint.constraints[0], direction);
                //Debug.Log("当前约束方向：" + direction);
                if (Vector3.Angle(beforeDirection, direction) > 0.1f)//如果两个角度大于一定阈值
                {
                    turnSpeedCount++;
                }
                else
                {
                    turnSpeedCount = 0;
                }
                Vector3 currentDirection = curPoint.transform.position - curPoint.constraints[0].target.transform.position;
                //计算两个方向之间的夹角，并根据转速百分比限制每次旋转的最大角度
                float angle = Vector3.SignedAngle(currentDirection, direction, Vector3.forward);
                float maxAngle = turnSpeed/100f * angle;
                Vector3 newDirection = Quaternion.AngleAxis(maxAngle, Vector3.forward) * currentDirection;
                curPoint.transform.position = curPoint.constraints[0].target.transform.position + newDirection * curPoint.constraints[0].fixedDistance;
            }

            AddFixedDistance(curPoint, moveDistance);

            curPoint = parentPoint;
            if (curPoint != null)
                parentPoint = curPoint.parents.Count > 0 ? curPoint.parents[0] : null;

            yield return new WaitForSeconds(moveInterval);
        }
        //恢复所有修改过的约束权重
        foreach (var point in weightBackup)
        {
            point.Key.selfWeight = point.Value;
        }
        weightBackup.Clear();

        if (turnSpeedCount >= moveBackupCount)
        {
            turnSpeedCount = 0;
            StartCoroutine(BackMoveCoroutine());
        }

        isMoving = false;
    }

    IEnumerator BackMoveCoroutine()
    {
        brain.SetBoolState("IsBacking", true);

        int count = backMoveCount;

        //如果距离目标位置大于10倍停止距离，则减少后退次数
        if (Vector3.Distance(headTransform.position, targetPosition) > stopDistance * 10f)
        {
            count = Mathf.Max(1, backMoveCount / 2);
        }

        for (int i = 0; i < count; i++)
        {
            curPoint = headTransform.GetComponent<PointConstraint>();
            parentPoint = curPoint.parents.Count > 0 ? curPoint.parents[0] : null;
            Vector3 backTarget = headTransform.position + (headTransform.position - targetPosition).normalized * curPoint.constraints[0].fixedDistance * 7f;
            while (curPoint != null)
            {
                if (curPoint != this.GetComponent<PointConstraint>()){
                    weightBackup.Add(curPoint, curPoint.selfWeight);
                    curPoint.selfWeight = 1f;
                    AddFixedDistance(curPoint, -moveDistance);
                }
                else{//到了尾部体节位置
                    parentPoint.selfWeight = 0f;
                    //将尾部体节转向目标与父体节的连线上
                    Vector3 direction = (backTarget - curPoint.parents[0].transform.position).normalized;
                    //转向不超过45度
                    Vector3 currentDirection = curPoint.transform.position - curPoint.parents[0].transform.position;
                    float angle = Vector3.SignedAngle(currentDirection, direction, Vector3.forward);
                    float maxAngle = Mathf.Clamp(angle, -45f, 45f);
                    Vector3 newDirection = Quaternion.AngleAxis(maxAngle, Vector3.forward) * currentDirection;
                    curPoint.transform.position = curPoint.parents[0].transform.position + newDirection.normalized * parentPoint.constraints[0].fixedDistance;
                }

                AddFixedDistance(parentPoint, moveDistance);
                parentPoint = curPoint;
                if (curPoint.constraints != null && curPoint.constraints.Count > 0)
                {
                    curPoint = curPoint.constraints[0].target;
                }
                else
                {
                    curPoint = null;
                }

                yield return new WaitForSeconds(moveInterval);
            }

            //恢复所有修改过的约束权重
            foreach (var point in weightBackup)
            {
                point.Key.selfWeight = point.Value;
            }
            weightBackup.Clear();
        }

        brain.SetBoolState("IsBacking", false);   
    }

}
