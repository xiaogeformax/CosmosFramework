using UnityEngine;
using Cosmos;
using Cosmos.Entity;

/// <summary>
/// 实体能力组件 - 负责管理实体的各种能力
/// </summary>
public class EntityAbility : EntityComponentBase
{
    [SerializeField] private float bulletSpeed = 15;
    [SerializeField] private float bulletMoveDuration = 3;
    [SerializeField] private int damage = 120;
    
    private IEntityManager entityManager;
    
    public override void OnInit()
    {
        entityManager = CosmosEntry.EntityManager;
    }
    
    /// <summary>
    /// 执行攻击能力
    /// </summary>
    public void Attack()
    {
        SpawnBullet();
    }
    
    /// <summary>
    /// 生成子弹
    /// </summary>
    private void SpawnBullet()
    {
        entityManager.ShowEntity(EntityContants.EntityBullet, out var entityObject);
        var bullet = entityObject as BulletEntity;
        if (bullet != null)
        {
            bullet.onHit = (hitObject) => {
                // 隐藏子弹实体
                entityManager.HideEntityObject(bullet);
                
                // 处理伤害逻辑
                if (hitObject != null)
                {
                    var enemyEntity = hitObject.GetComponent<EnmeyEntity>();
                    if (enemyEntity != null)
                    {
                        enemyEntity.Damage(damage);
                    }
                }
            };
            
            // 设置子弹位置和方向
            Transform transform = Entity.transform;
            bullet.transform.position = transform.position + transform.forward * 0.5f + new Vector3(0, 1.5f, 0);
            bullet.transform.eulerAngles = transform.eulerAngles;
            
            // 配置子弹属性
            bullet.SetupBullet(bulletSpeed, bulletMoveDuration);
            bullet.OnShoot();
        }
    }
    
}