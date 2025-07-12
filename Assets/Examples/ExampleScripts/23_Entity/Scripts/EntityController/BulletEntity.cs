using System;
using UnityEngine;
using Cosmos;
using Cosmos.Entity;

public class BulletEntity : EntityObject
{
    public Action<GameObject> onHit;

    private BulletMovementComponent movementComponent;
    private BulletTriggerComponent triggerComponent;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        if (isInitialized) return;
        
        // 添加必要组件
        movementComponent = AddComponent<BulletMovementComponent>();
        triggerComponent = AddComponent<BulletTriggerComponent>();
        
        // 配置子组件
        triggerComponent.OnHitCallback = OnBulletHit;
        
        isInitialized = true;
    }
    
    public void SetupBullet(float speed, float moveDuration)
    {
        // 确保组件已初始化
        InitializeComponents();
        
        movementComponent.Speed = speed;
        movementComponent.MoveDuration = moveDuration;
    }
    
    public void OnShoot()
    {
        // 确保组件已初始化
        InitializeComponents();
        
        movementComponent.ResetTime();
    }

    private void OnBulletHit(GameObject hitObject)
    {
        onHit?.Invoke(hitObject);
    }

    public override void OnHide()
    {
        base.OnHide();
        transform.ResetWorldTransform();
    }
}