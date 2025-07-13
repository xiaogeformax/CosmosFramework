using System;
using System.Runtime.InteropServices;

namespace Cosmos
{
    /// <summary>
    /// 引用池信息结构体
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct ReferencePoolInfo
    {
        private readonly Type referenceType;
        private readonly int activeCount;
        private readonly int recycleCount;
        private readonly int availableCount;

        /// <summary>
        /// 引用对象类型
        /// </summary>
        public Type ReferenceType => referenceType;

        /// <summary>
        /// 当前活跃的对象数量
        /// </summary>
        public int ActiveCount => activeCount;

        /// <summary>
        /// 对象回收次数
        /// </summary>
        public int RecycleCount => recycleCount;

        /// <summary>
        /// 池中当前可用对象数量
        /// </summary>
        public int AvailableCount => availableCount;

        /// <summary>
        /// 引用池信息构造函数
        /// </summary>
        /// <param name="referenceType">引用对象类型</param>
        /// <param name="activeCount">当前活跃的对象数量</param>
        /// <param name="recycleCount">对象回收次数</param>
        /// <param name="availableCount">池中当前可用对象数量</param>
        public ReferencePoolInfo(Type referenceType, int activeCount, int recycleCount, int availableCount)
        {
            this.referenceType = referenceType;
            this.activeCount = activeCount;
            this.recycleCount = recycleCount;
            this.availableCount = availableCount;
        }
        
        /// <summary>
        /// 获取引用池信息的字符串表示
        /// </summary>
        /// <returns>信息字符串</returns>
        public override string ToString()
        {
            return $"ReferencePool[{referenceType.Name}] - Active: {activeCount}, Recycled: {recycleCount}, Available: {availableCount}";
        }
    }
}