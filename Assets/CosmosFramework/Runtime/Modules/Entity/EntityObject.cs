﻿using System;
using System.Collections.Generic;
using UnityEngine;
namespace Cosmos.Entity
{
    /// <summary>
    /// 实体对象基类 - 采用组件化设计
    /// </summary>
    public class EntityObject : MonoBehaviour, IEquatable<EntityObject>
    {
        private string entityName;
        private Dictionary<Type, IEntityComponent> components = new Dictionary<Type, IEntityComponent>();
        
        /// <summary>
        /// 实体名称
        /// </summary>
        public string EntityName
        {
            get { return entityName; }
            set
            {
                gameObject.name = value;
                entityName = value;
            }
        }
        
        /// <summary>
        /// 实体组名称
        /// </summary>
        public string EntityGroupName { get; private set; }
        
        /// <summary>
        /// 实体游戏对象
        /// </summary>
        public GameObject Handle { get { return gameObject; } }
        
        /// <summary>
        /// 实体唯一ID
        /// </summary>
        public int EntityObjectId { get; private set; }

        /// <summary>
        /// 初始化实体
        /// </summary>
        public virtual void OnInit(string entityName, int entityObjectId, string entityGroupName)
        {
            EntityName = entityName;
            EntityGroupName = entityGroupName;
            EntityObjectId = entityObjectId;
        }

        /// <summary>
        /// 显示实体
        /// </summary>
        public virtual void OnShow()
        {
            gameObject.SetActive(true);
            foreach (var component in components.Values)
            {
                component.OnShow();
            }
        }

        /// <summary>
        /// 隐藏实体
        /// </summary>
        public virtual void OnHide()
        {
            foreach (var component in components.Values)
            {
                component.OnHide();
            }
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 回收实体
        /// </summary>
        public virtual void OnRecycle() 
        {
            foreach (var component in components.Values)
            {
                component.OnRecycle();
            }
        }

        /// <summary>
        /// 添加实体组件
        /// </summary>
        public T AddComponent<T>() where T : class, IEntityComponent, new()
        {
            Type type = typeof(T);
            if (components.TryGetValue(type, out IEntityComponent existingComponent))
            {
                return existingComponent as T;
            }

            T component = new T();
            component.Entity = this;
            component.OnInit();
            components.Add(type, component);
            return component;
        }

       
        /// <summary>
        /// 获取实体逻辑组件
        /// </summary>
        public T GetComponent<T>() where T : class, IEntityComponent
        {
            Type type = typeof(T);
            if (components.TryGetValue(type, out IEntityComponent component))
            {
                return component as T;
            }
            return null;
        }
        
        /// <summary>
        /// 获取Unity组件
        /// </summary>
        public T GetUnityComponent<T>() where T : Component
        {
            return gameObject.GetComponent<T>();
        }
        
        /// <summary>
        /// 获取子对象上的Unity组件
        /// </summary>
        public T GetUnityComponentInChildren<T>(string childName = null) where T : Component
        {
            if (string.IsNullOrEmpty(childName))
            {
                return gameObject.GetComponentInChildren<T>();
            }
            else
            {
                Transform child = gameObject.transform.Find(childName);
                return child != null ? child.GetComponent<T>() : null;
            }
        }

        /// <summary>
        /// 移除实体组件
        /// </summary>
        public bool RemoveComponent<T>() where T : class, IEntityComponent
        {
            Type type = typeof(T);
            if (components.TryGetValue(type, out IEntityComponent component))
            {
                component.OnDestroy();
                return components.Remove(type);
            }
            return false;
        }

        /// <summary>
        /// 判断实体相等
        /// </summary>
        public bool Equals(EntityObject other)
        {
            return other.EntityName == this.EntityName &&
                other.EntityObjectId == this.EntityObjectId;
        }
        
        /// <summary>
        /// 更新所有组件
        /// </summary>
        protected virtual void Update()
        {
            foreach (var component in components.Values)
            {
                component.OnUpdate();
            }
        }
    }
}