using System;
using UnityEngine;
using Cosmos;
using Cosmos.Entity;

public class BulletEntity : EntityObject
{
    public Action<GameObject> onHit;

    private void Start()
    {
        // 添加必要组件
        var movementComponent = AddComponent<BulletMovementComponent>();
        var triggerComponent = AddComponent<BulletTriggerComponent>();
        
        // 配置子组件
        triggerComponent.OnHitCallback = OnBulletHit;
    }
    
    public void SetupBullet(float speed, float moveDuration)
    {
        var movementComponent = GetComponent<BulletMovementComponent>();
        movementComponent.Speed = speed;
        movementComponent.MoveDuration = moveDuration;
    }
    
    public void OnShoot()
    {
        var movementComponent = GetComponent<BulletMovementComponent>();
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