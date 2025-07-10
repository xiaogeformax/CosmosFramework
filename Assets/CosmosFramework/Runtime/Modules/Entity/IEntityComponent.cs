using UnityEngine;

namespace Cosmos.Entity
{
    /// <summary>
    /// 实体组件接口
    /// </summary>
    public interface IEntityComponent
    {
        /// <summary>
        /// 所属实体
        /// </summary>
        EntityObject Entity { get; set; }
        
        /// <summary>
        /// 初始化组件
        /// </summary>
        void OnInit();
        
        /// <summary>
        /// 组件更新逻辑
        /// </summary>
        void OnUpdate();
        
        /// <summary>
        /// 显示组件
        /// </summary>
        void OnShow();
        
        /// <summary>
        /// 隐藏组件
        /// </summary>
        void OnHide();
        
        /// <summary>
        /// 回收组件
        /// </summary>
        void OnRecycle();
        
        /// <summary>
        /// 销毁组件
        /// </summary>
        void OnDestroy();
    }
}