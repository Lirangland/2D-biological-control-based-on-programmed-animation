using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

//怪物移动控制，挂载在身体节点上
public class MonsterMove : Receptor
{
    public List<TentacleEnd> TantacleEnds = new List<TentacleEnd>();//触手末端列表，鼠标位于怪物身体周围时，根据等分的区域选择当前控制的触手末端
    public float moveDistance = 3;
    public float tantacleMoveSpeed = 5f;
    public float tantacleMoveThreshold = 0.1f;
    [SerializeField] TentacleEnd curTentacleEnd;//当前控制的触手末端

    public override void OnUpdate()
    {
        if (TantacleEnds.Count == 0)
        {
            return;
        }
        Vector3 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(mousePos);
        worldMousePos.z = 0;
        Vector3 dir = worldMousePos - transform.position;
        //计算dir与transform.up的夹角，范围是0~360，判断鼠标位于身体周围的哪个区域
        float angle = Vector3.SignedAngle(transform.up, dir, -Vector3.forward);
        angle += 360f / TantacleEnds.Count / 2f;//为了让每个区域的边界在两个触手末端的中间，进行偏移
        if (angle < 0)
        {
            angle += 360f;
        }
        int index = (int)(angle / (360f / TantacleEnds.Count));
        if (index >= TantacleEnds.Count)
        {
            index = 0;
        }
        if (curTentacleEnd != TantacleEnds[index] && !Mouse.current.leftButton.isPressed)
        {
            if (curTentacleEnd != null)
            {
                curTentacleEnd.EndMove();
            }
            curTentacleEnd = TantacleEnds[index];
        }

        if (Mouse.current.leftButton.isPressed)
        {
            curTentacleEnd.StartMove();
        }
        else
        {
            Vector3 newDir = worldMousePos - curTentacleEnd.transform.position;
            if (dir.magnitude >= moveDistance)
            {
                Vector3 target = transform.position + dir.normalized * moveDistance;
                newDir = target - curTentacleEnd.transform.position;
            }
            Debug.Log($"dir: {dir}, newDir: {newDir}");
            if (newDir.magnitude > tantacleMoveThreshold)
                curTentacleEnd.transform.position += newDir.normalized * Mathf.Min(newDir.magnitude, 1f) * Time.deltaTime * tantacleMoveSpeed;
        }
        
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            curTentacleEnd.EndMove();
        }
    }

    
}
