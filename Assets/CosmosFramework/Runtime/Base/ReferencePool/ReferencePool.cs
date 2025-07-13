using System.Collections.Generic;
using System;
using System.Collections.Concurrent;

namespace Cosmos
{
    /// <summary>
    /// 对象引用池，用于管理实现了IReference接口的可复用对象
    /// </summary>
    public static class ReferencePool
    {
        #region Internal Classes
        
        /// <summary>
        /// 引用池的内部实现类
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        private sealed class ReferenceTypePool<T>
            where T : class, IReference
        {
            private readonly Pool<T> pool;
            private readonly Type referenceType;
            private int totalSpawnCount = 0;  // 总共生成的对象数量
            private int activeCount = 0;      // 当前活跃对象数量
            private int recycleCount = 0;     // 回收次数

            /// <summary>
            /// 引用类型
            /// </summary>
            public Type ReferenceType => referenceType;
            
            /// <summary>
            /// 总共生成的对象数量
            /// </summary>
            public int TotalSpawnCount => totalSpawnCount;
            
            /// <summary>
            /// 当前活跃的对象数量
            /// </summary>
            public int ActiveCount => activeCount;
            
            /// <summary>
            /// 回收次数
            /// </summary>
            public int RecycleCount => recycleCount;
            
            /// <summary>
            /// 池中当前可用对象数量
            /// </summary>
            public int AvailableCount => pool.Count;

            public ReferenceTypePool(Type type, int capacity = 0)
            {
                referenceType = type;
                pool = new Pool<T>(
                    capacity,
                    CreateInstance,
                    OnSpawn,
                    OnDespawn,
                    null
                );
            }

            /// <summary>
            /// 获取对象实例
            /// </summary>
            public T Acquire()
            {
                totalSpawnCount++;
                activeCount++;
                return pool.Spawn();
            }

            /// <summary>
            /// 回收对象实例
            /// </summary>
            public void Release(T obj)
            {
                if (obj == null)
                    throw new ArgumentNullException(nameof(obj), "Cannot release null reference object.");
                
                activeCount--;
                recycleCount++;
                pool.Despawn(obj);
            }

            /// <summary>
            /// 清空池
            /// </summary>
            public void Clear()
            {
                pool.Clear();
                // 不重置计数器，保留历史信息
            }
            
            private T CreateInstance()
            {
                return (T)Activator.CreateInstance(referenceType);
            }
            
            private void OnSpawn(T obj)
            {
                // 实例从池中取出时的额外操作（如有）
            }
            
            private void OnDespawn(T obj)
            {
                obj.Release();
            }
        }
        
        #endregion

        #region Fields & Properties
        
        private static readonly ConcurrentDictionary<Type, object> s_ReferencePoolMap = 
            new ConcurrentDictionary<Type, object>();
        
        /// <summary>
        /// 引用池中类型的总数
        /// </summary>
        public static int PoolCount => s_ReferencePoolMap.Count;
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// 获取指定类型的对象
        /// </summary>
        /// <typeparam name="T">要获取的对象类型</typeparam>
        /// <returns>对象实例</returns>
        public static T Acquire<T>() where T : class, IReference, new()
        {
            return GetReferencePool<T>().Acquire();
        }
        
        /// <summary>
        /// 获取指定类型的对象
        /// </summary>
        /// <param name="referenceType">要获取的对象类型</param>
        /// <returns>对象实例</returns>
        public static IReference Acquire(Type referenceType)
        {
            ValidateReferenceType(referenceType);
            
            // 使用反射调用泛型方法来获取正确类型的池
            var methodInfo = typeof(ReferencePool).GetMethod(
                nameof(GetReferencePool), 
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            
            var genericMethod = methodInfo.MakeGenericMethod(referenceType);
            var pool = genericMethod.Invoke(null, null);
            
            // 使用反射调用Acquire方法
            var acquireMethod = pool.GetType().GetMethod("Acquire");
            return acquireMethod.Invoke(pool, null) as IReference;
        }
        
        /// <summary>
        /// 回收对象到引用池
        /// </summary>
        /// <param name="reference">要回收的对象</param>
        public static void Release(IReference reference)
        {
            if (reference == null)
                throw new ArgumentNullException(nameof(reference), "Reference is invalid.");
            
            Type referenceType = reference.GetType();
            object pool = s_ReferencePoolMap.GetOrAdd(referenceType, t => 
                CreateReferencePool(referenceType));
                
            // 使用反射调用Release方法
            var method = pool.GetType().GetMethod("Release");
            method.Invoke(pool, new object[] { reference });
        }
        
        /// <summary>
        /// 回收多个相同类型的对象到引用池
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="references">要回收的对象集合</param>
        public static void Release<T>(IEnumerable<T> references) where T : class, IReference
        {
            if (references == null)
                throw new ArgumentNullException(nameof(references), "References collection is invalid.");
            
            var pool = GetReferencePool<T>();
            foreach (var reference in references)
            {
                if (reference != null)
                    pool.Release(reference);
            }
        }
        
        /// <summary>
        /// 回收多个对象到引用池
        /// </summary>
        /// <param name="references">要回收的对象数组</param>
        public static void Release(params IReference[] references)
        {
            if (references == null)
                throw new ArgumentNullException(nameof(references), "Reference array is invalid.");
            
            foreach (var reference in references)
            {
                if (reference != null)
                    Release(reference);
            }
        }
        
        /// <summary>
        /// 添加指定容量的引用池
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        /// <param name="capacity">池容量</param>
        public static void AddPool<T>(int capacity = 0) where T : class, IReference, new()
        {
            s_ReferencePoolMap.GetOrAdd(typeof(T), t => 
                new ReferenceTypePool<T>(typeof(T), capacity));
        }
        
        /// <summary>
        /// 移除引用池
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        public static bool RemovePool<T>() where T : class, IReference
        {
            var type = typeof(T);
            if (s_ReferencePoolMap.TryRemove(type, out var pool))
            {
                // 使用反射调用Clear方法
                pool.GetType().GetMethod("Clear").Invoke(pool, null);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 移除引用池
        /// </summary>
        /// <param name="referenceType">引用类型</param>
        /// <returns>是否成功移除</returns>
        public static bool RemovePool(Type referenceType)
        {
            ValidateReferenceType(referenceType);
            
            if (s_ReferencePoolMap.TryRemove(referenceType, out var pool))
            {
                // 使用反射调用Clear方法
                pool.GetType().GetMethod("Clear").Invoke(pool, null);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 清除所有引用池
        /// </summary>
        public static void ClearAll()
        {
            foreach (var pool in s_ReferencePoolMap.Values)
            {
                // 使用反射调用Clear方法
                pool.GetType().GetMethod("Clear").Invoke(pool, null);
            }
            s_ReferencePoolMap.Clear();
        }
        
        /// <summary>
        /// 获取指定类型的引用池信息
        /// </summary>
        /// <typeparam name="T">引用类型</typeparam>
        /// <returns>引用池信息</returns>
        public static ReferencePoolInfo GetPoolInfo<T>() where T : class, IReference, new()
        {
            var pool = GetReferencePool<T>();
            return new ReferencePoolInfo(
                typeof(T), 
                pool.ActiveCount, 
                pool.RecycleCount, 
                pool.AvailableCount);
        }
        
        /// <summary>
        /// 获取指定类型的引用池信息
        /// </summary>
        /// <param name="referenceType">引用类型</param>
        /// <returns>引用池信息</returns>
        public static ReferencePoolInfo GetPoolInfo(Type referenceType)
        {
            ValidateReferenceType(referenceType);
            
            if (s_ReferencePoolMap.TryGetValue(referenceType, out var poolObject))
            {
                // 使用反射获取池信息
                var poolType = poolObject.GetType();
                int activeCount = (int)poolType.GetProperty("ActiveCount").GetValue(poolObject);
                int recycleCount = (int)poolType.GetProperty("RecycleCount").GetValue(poolObject);
                int availableCount = (int)poolType.GetProperty("AvailableCount").GetValue(poolObject);
                
                return new ReferencePoolInfo(referenceType, activeCount, recycleCount, availableCount);
            }
            
            return default;
        }
        
        /// <summary>
        /// 获取所有引用池信息
        /// </summary>
        /// <returns>引用池信息数组</returns>
        public static ReferencePoolInfo[] GetAllPoolInfos()
        {
            var result = new ReferencePoolInfo[s_ReferencePoolMap.Count];
            int index = 0;
            
            foreach (var poolPair in s_ReferencePoolMap)
            {
                var poolObject = poolPair.Value;
                var poolType = poolObject.GetType();
                
                Type referenceType = (Type)poolType.GetProperty("ReferenceType").GetValue(poolObject);
                int activeCount = (int)poolType.GetProperty("ActiveCount").GetValue(poolObject);
                int recycleCount = (int)poolType.GetProperty("RecycleCount").GetValue(poolObject);
                int availableCount = (int)poolType.GetProperty("AvailableCount").GetValue(poolObject);
                
                result[index++] = new ReferencePoolInfo(referenceType, activeCount, recycleCount, availableCount);
            }
            
            return result;
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// 获取指定类型的引用池
        /// </summary>
        private static ReferenceTypePool<T> GetReferencePool<T>() where T : class, IReference
        {
            return (ReferenceTypePool<T>)s_ReferencePoolMap.GetOrAdd(typeof(T), t => 
                new ReferenceTypePool<T>(typeof(T)));
        }
        
        /// <summary>
        /// 创建引用池的实例
        /// </summary>
        private static object CreateReferencePool(Type referenceType)
        {
            // 创建泛型ReferenceTypePool的实例
            var poolType = typeof(ReferenceTypePool<>).MakeGenericType(referenceType);
            return Activator.CreateInstance(poolType, referenceType, 0);
        }
        
        /// <summary>
        /// 验证类型是否为有效的引用类型
        /// </summary>
        private static void ValidateReferenceType(Type referenceType)
        {
            if (referenceType == null)
                throw new ArgumentNullException(nameof(referenceType), "Reference type is invalid.");
                
            if (!referenceType.IsClass || referenceType.IsAbstract)
                throw new ArgumentException("Reference type must be a non-abstract class type.", nameof(referenceType));
                
            if (!typeof(IReference).IsAssignableFrom(referenceType))
                throw new ArgumentException("Reference type must implement IReference interface.", nameof(referenceType));
        }
        
        #endregion
    }
}