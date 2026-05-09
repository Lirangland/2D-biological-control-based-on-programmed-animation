using UnityEngine;
using System;
public class CentipedeBehavior : Behavior
{
    public override void Init()
    {
        eventModule.AddEventListener<Action<AllFoodInformation>>(EventName.DiscoverFood, (foodInfo) => MoveToFood(foodInfo));
        eventModule.AddEventListener<Action<Food>>(EventName.TouchFood, (food) => Eat(food));
    }

    //移动到食物位置
    void MoveToFood(AllFoodInformation foodInformation)
    {
        
        //寻找最近的食物位置，并设置name为“move”的效应器的target为食物位置
        Food nearestFood = null;
        float minDistance = float.MaxValue;
        foreach (var food in foodInformation.foodList)
        {
            float distance = Vector3.Distance(transform.position, food.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestFood = food;
            }
        }
        if (nearestFood != null)
        {
           MoveToTarget moveToTarget = (MoveToTarget)brain.GetEffector("move");
           if (moveToTarget != null)
           {
               moveToTarget.target = new Vector3(nearestFood.transform.position.x, nearestFood.transform.position.y, transform.position.z);
           }
        }
    }

    void Eat(Food food)
    {
        if (brain.HasKey("hunger"))
        {
            int hunger = brain.GetIntState("hunger");
            hunger += food.nutritionValue;
            hunger = Mathf.Clamp(hunger, 0, 100);
            brain.SetIntState("hunger", hunger);
        }
        MoveToTarget moveToTarget = (MoveToTarget)brain.GetEffector("move");
        moveToTarget.target = Vector3.zero;
        Destroy(food.gameObject);
    }

}
