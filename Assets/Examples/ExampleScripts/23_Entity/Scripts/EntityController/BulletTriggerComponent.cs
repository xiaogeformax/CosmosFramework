using System;
using UnityEngine;
using Cosmos;
using Cosmos.Entity;

public class BulletTriggerComponent : EntityComponentBase
{
    private EntityBulletTrigger bulletTrigger;
    public Action<GameObject> OnHitCallback { get; set; }

    public override void OnInit()
    {
        bulletTrigger = Entity.Handle.GetOrAddComponentInChildren<EntityBulletTrigger>("BulletTrigger");
        bulletTrigger.onTriggerHit = InvokeHitCallback;
    }
    
    public void InvokeHitCallback(GameObject hitObject)
    {
        OnHitCallback?.Invoke(hitObject);
    }
}