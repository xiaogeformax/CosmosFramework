using System;
using UnityEngine;
using Cosmos.Entity;

public class BulletMovementComponent : EntityComponentBase
{
    public float Speed { get; set; }
    public float MoveDuration { get; set; }
    
    private float currentTime = 0;
    public Action OnTimeExpired { get; set; }
    
    public void ResetTime()
    {
        currentTime = 0;
    }
    
    public override void OnUpdate()
    {
        Transform transform = Entity.transform;
        transform.position += transform.forward * Time.deltaTime * Speed;
        
        currentTime += Time.deltaTime;
        if (currentTime >= MoveDuration)
        {
            var triggerComponent = GetEntityComponent<BulletTriggerComponent>();
            triggerComponent?.InvokeHitCallback(null);
        }
    }
    
    public override void OnRecycle()
    {
        currentTime = 0;
    }
}