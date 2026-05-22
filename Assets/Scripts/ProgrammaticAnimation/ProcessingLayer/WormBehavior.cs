using System;
using UnityEngine;

public class WormBehavior : Behavior
{
    public override void Init()
    {
        eventModule.AddEventListener<Action<AllFoodInformation>>(EventName.DiscoverFood, (foodInfo) => MoveToFood(foodInfo));
        eventModule.AddEventListener<Action<Food>>(EventName.TouchFood, (food) => Eat(food));
    }

    //移动到食物位置
    void MoveToFood(AllFoodInformation foodInformation)
    {
        if (brain.GetBoolState("isBacking")) return;
        
        //寻找最有营养的食物位置，并设置name为“move”的效应器的target为食物位置
        Food nearestFood = null;
        float maxNutritionValue = float.MinValue;
        foreach (var food in foodInformation.foodList)
        {
            if (food.nutritionValue > maxNutritionValue)
            {
                maxNutritionValue = food.nutritionValue;
                nearestFood = food;
            }
        }
        if (nearestFood != null)
        {
           WormMove wormMove = (WormMove)brain.GetEffector("move");
           if (wormMove != null)
           {
               wormMove.targetPosition = new Vector3(nearestFood.transform.position.x, nearestFood.transform.position.y, transform.position.z);
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
            brain.SetBoolState("StopMove", true);
        }
        Destroy(food.gameObject);
    }
}
