using UnityEngine;

//将物体的位置每帧逼近目标位置，形成移动效果。可以与PointConstraint等约束组件配合使用，形成更复杂的动画效果。
public class MoveToTarget : Effector
{
    public Vector3 target; // 目标位置
    public float moveSpeed = 5f; // 移动速度，数值越大移动越快
    public float stopDistance = 0.1f; // 停止距离，距离目标小于此值时停止移动
    public float rotateSpeed = 360f; // 旋转速度，数值越大旋转越快

    public override void OnUpdate()
    {
        if (target == null) return;

        Vector3 direction = target - transform.position;
        float distance = direction.magnitude;

        if (distance > stopDistance && target != Vector3.zero)
        {
            //移动
            Vector3 moveDirection = direction.normalized;
            transform.position += moveDirection * moveSpeed * Time.deltaTime;

            //旋转
            PointConstraint pointConstraint = GetComponent<PointConstraint>();
            if (pointConstraint != null)
            {
                for (int i = 0; i < pointConstraint.solverIterations; i++)
                {
                    pointConstraint.SolveEntireChainConstraints(); // 先解算约束关系，更新约束后的目标位置
                }
                moveDirection = pointConstraint.GetDirection(pointConstraint.constraints[0], moveDirection);
            }


            Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
    }
}
