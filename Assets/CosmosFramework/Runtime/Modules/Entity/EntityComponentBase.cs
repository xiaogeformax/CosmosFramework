using UnityEngine;

namespace Cosmos.Entity
{
    /// <summary>
    /// 实体组件基类
    /// </summary>
    public abstract class EntityComponentBase : IEntityComponent
    {
        /// <summary>
        /// 所属实体
        /// </summary>
        public EntityObject Entity { get; set; }
        
        /// <summary>
        /// 初始化组件
        /// </summary>
        public virtual void OnInit() { }
        
        /// <summary>
        /// 组件更新逻辑
        /// </summary>
        public virtual void OnUpdate() { }
        
        /// <summary>
        /// 显示组件
        /// </summary>
        public virtual void OnShow() { }
        
        /// <summary>
        /// 隐藏组件
        /// </summary>
        public virtual void OnHide() { }
        
        /// <summary>
        /// 回收组件
        /// </summary>
        public virtual void OnRecycle() { }
        
        /// <summary>
        /// 销毁组件
        /// </summary>
        public virtual void OnDestroy() { }
        
        /// <summary>
        /// 获取实体上的其他组件
        /// </summary>
        protected T GetEntityComponent<T>() where T : class, IEntityComponent
        {
            return Entity?.GetComponent<T>();
        }
        
        /// <summary>
        /// 获取实体上的Unity组件
        /// </summary>
        protected T GetUnityComponent<T>() where T : Component
        {
            return Entity?.Handle.GetComponent<T>();
        }
    }
}