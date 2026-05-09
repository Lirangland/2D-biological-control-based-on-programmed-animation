using System.Collections.Generic;
using UnityEngine;
//定义所有的感受器发送给处理层的事件数据结构

public class AllFoodInformation//食物信息
{
    public List<Food> foodList;

    public AllFoodInformation(List<Food> foodList)
    {
        this.foodList = foodList;
    }
}

