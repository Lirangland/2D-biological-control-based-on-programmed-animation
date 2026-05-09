using System.Collections.Generic;
using UnityEngine;

public class Eye : Receptor
{
    public float viewDistance = 10f; // 视距
    public float viewAngle = 360f; // 视野角度，以transform.up为中心的夹角范围
    List<Transform> visibleTargets = new List<Transform>(); // 视距内的可见目标列表

    public override void OnUpdate()
    {
        //获取视距内的所有带有Visible组件的物体，并根据距离向每个物体发射一段稍长于这段距离的射线，若射线长度超过这段距离或击中该物体则说明该物体在视距内，将其加入可见目标列表
        //射线忽略生物层碰撞
        visibleTargets.Clear();
        Collider2D[] targetsInViewRadius = Physics2D.OverlapCircleAll(transform.position, viewDistance);
        foreach (var target in targetsInViewRadius)        {
            if (target.GetComponent<Visible>() == null) continue;
            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.up, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.transform.position);
                RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToTarget, dstToTarget + 0.1f, ~LayerMask.GetMask("Creature"));
                if (hit.distance >= dstToTarget)
                {
                    visibleTargets.Add(target.transform);
                }
                else if (hit != false)
                {
                    if (hit.collider.gameObject == target.gameObject)
                    {
                        visibleTargets.Add(target.transform);
                    }
                }
            }
        }

        //如果可视列表有食物，向决策中心发送“发现食物”事件
        List<Food> foodList = new List<Food>();
        foreach (var target in visibleTargets)
        {
            if (target.GetComponent<Food>() != null)
            {
                foodList.Add(target.GetComponent<Food>());
            }
        }
        if (foodList.Count > 0)
        {
            brain.EventTrigger<AllFoodInformation>(EventName.DiscoverFood, new AllFoodInformation(foodList));
        }
    }
}

