using UnityEngine;
using Cosmos;
using Cosmos.Entity;

[RequireComponent(typeof(EntityAnimator))]
public class PlayerEntity : EntityObject
{
    private void Start()
    {
        // 添加玩家需要的各种组件
        AddComponent<PlayerMovementComponent>();
        AddComponent<EntityAbility>();
        AddComponent<PlayerInputComponent>();
        
        
        // 初始化动画系统
        var animator = Handle.GetComponent<EntityAnimator>();
        if (animator != null)
        {
            var inputComponent = GetComponent<PlayerInputComponent>();
            inputComponent.SetupAnimator(animator);
        }
    }
}