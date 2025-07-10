using UnityEngine;
using Cosmos;
using Cosmos.Entity;

public class PlayerInputComponent : EntityComponentBase
{
    private EntityAnimator entityAnimator;
    private EntityAbility entityAbility;
    private PlayerMovementComponent movementComponent;
    private bool onAttack;
    private bool onAttackAnim;
    
    public override void OnInit()
    {
        entityAbility = GetEntityComponent<EntityAbility>();
        movementComponent = GetEntityComponent<PlayerMovementComponent>();
    }
    
    public void SetupAnimator(EntityAnimator animator)
    {
        entityAnimator = animator;
        if (entityAnimator != null)
        {
            entityAnimator.OnAttackOff += OnAttackOff;
            entityAnimator.OnAttackAnim += OnAttackAnim;
        }
    }
    
    public override void OnUpdate()
    {
        // 处理攻击输入
        bool attackBtnDown = CosmosEntry.InputManager.GetButtonDown(InputButtonType._MouseLeft);
        if (attackBtnDown && !onAttack)
        {
            entityAnimator?.Attack();
            onAttack = true;
        }
        
        if (onAttack)
            return;
        
        // 处理移动输入
        var h = CosmosEntry.InputManager.GetAxis(InputAxisType._Horizontal);
        var v = CosmosEntry.InputManager.GetAxis(InputAxisType._Vertical);
        var inputDir = new Vector3(h, 0, v);
        
        if (inputDir != Vector3.zero)
        {
            movementComponent?.Move(inputDir);
            entityAnimator?.Move();
        }
        else
        {
            entityAnimator?.Idle();
        }
    }
    
    private void OnAttackOff()
    {
        onAttack = false;
        onAttackAnim = false;
    }
    
    private void OnAttackAnim()
    {
        if (!onAttackAnim)
        {
            entityAbility?.Attack();
            onAttackAnim = true;
        }
    }
}