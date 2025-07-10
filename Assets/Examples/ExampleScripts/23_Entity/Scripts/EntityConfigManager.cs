using System;
using UnityEngine;

namespace Cosmos.Entity
{
    /// <summary>
    /// 实体配置类 - 用于集中管理实体的属性配置
    /// </summary>
    [Serializable]
    public class EntityConfig
    {
        [SerializeField] private string entityType;
        [SerializeField] private string prefabPath;
        [SerializeField] private int poolSize = 10;
        
        public string EntityType => entityType;
        public string PrefabPath => prefabPath;
        public int PoolSize => poolSize;
        
        public EntityConfig(string entityType, string prefabPath, int poolSize = 10)
        {
            this.entityType = entityType;
            this.prefabPath = prefabPath;
            this.poolSize = poolSize;
        }
    }
    
    /// <summary>
    /// 实体配置管理器 - 提供统一的配置管理
    /// </summary>
    public class EntityConfigManager
    {
        private static EntityConfigManager instance;
        public static EntityConfigManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new EntityConfigManager();
                return instance;
            }
        }
        
        // 添加更多的实体配置预设或从配置文件加载
        public EntityConfig GetEntityConfig(string entityType)
        {
            // 实际开发中应该从配置文件或ScriptableObject加载
            switch (entityType)
            {
                case EntityContants.EntityBullet:
                    return new EntityConfig(EntityContants.EntityBullet, "EntityBullet", 20);
                case EntityContants.EntityEnmey:
                    return new EntityConfig(EntityContants.EntityEnmey, "EntityEnemy", 10);
                default:
                    return null;
            }
        }
    }
}