using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Cosmos
{
    /// <summary>
    /// 计时器，需要从外部调用轮询。
    /// 所有逻辑线程安全；
    /// </summary>
    public class TickTimer
    {
        class TickTask
        {
            /// <summary>
            /// 任务Id；
            /// </summary>
            public int TaskId;
            /// <summary>
            /// 间隙时间；
            /// </summary>
            public int IntervalTime;
            /// <summary>
            /// 循环执行次数；
            /// </summary>
            public int LoopCount;
            public double DestTime;
            public Action<int> TaskCallback;
            public Action<int> CancelCallback;
            public double StartTime;
            public int LoopIndex;
            public bool IsPause;
            public double PauseRemainTime;
            public TickTask(int taskId, int loopCount, int intervalTime, double destTime, Action<int> taskCallback, Action<int> cancelCallback, double startTime)
            {
                this.TaskId = taskId;
                this.LoopCount = loopCount;
                this.IntervalTime = intervalTime;
                this.DestTime = destTime;
                this.TaskCallback = taskCallback;
                this.CancelCallback = cancelCallback;
                this.StartTime = startTime;
                this.IsPause = false;
                this.PauseRemainTime = 0;
            }
            public void Dispose()
            {
                this.TaskId = 0;
                this.LoopCount = 0;
                this.IntervalTime = 0;
                this.DestTime = 0;
                this.TaskCallback = null;
                this.CancelCallback = null;
                this.StartTime = 0;
                this.LoopIndex = 0;
                this.IsPause = false;
                this.PauseRemainTime = 0;
            }
        }
        public Action<string> LogInfo { get; set; }
        public Action<string> LogWarn { get; set; }
        public Action<string> LogError { get; set; }
        readonly DateTime startDateTime = new DateTime(1970, 1, 1, 0, 0, 0);
        readonly ConcurrentDictionary<int, TickTask> taskDict;
        // 新增：使用优先队列（最小堆）按到期时间排序
        private readonly SortedDictionary<double, List<TickTask>> timeSlots;
        private readonly Queue<double> expiredSlots;
        
        int taskIndex = 0;
        public int TaskCount { get { return taskDict.Count; } }
        Queue<TickTask> taskQueue;
        bool usePool;
        public bool UsePool { get { return usePool; } }
        bool pause;
        public bool Pause
        {
            get { return pause; }
            set { pause = value; }
        }
        /// <summary>
        /// 计时器构造函数；
        /// </summary>
        /// <param name="usePool">是否使用池缓对task进行缓存。任务量大时，建议为true</param>
        /// <param name="initialCapacity">初始容量，避免频繁扩容</param>
        public TickTimer(bool usePool = false, int initialCapacity = 64)
        {
            taskDict = new ConcurrentDictionary<int, TickTask>(Environment.ProcessorCount * 2, initialCapacity);
            timeSlots = new SortedDictionary<double, List<TickTask>>();
            expiredSlots = new Queue<double>();
            
            this.usePool = usePool;
            if (usePool)
                taskQueue = new Queue<TickTask>(initialCapacity);
            pause = false;
        }
        /// <summary>
        /// 添加一次性任务；
        /// 若任务添加成功，则返回大于0的TaskId；
        /// 若任务添加失败，则返回-1；
        /// </summary>
        /// <param name="delayTime">毫秒级别时间延迟</param>
        /// <param name="taskCallback">执行回调</param>
        /// <returns>添加事件成功后返回的ID</returns>
        public int AddTask(int delayTime, Action<int> taskCallback)
        {
            return AddTask(delayTime, 0, taskCallback, null, 1);
        }
        /// <summary>
        /// 添加任务；
        /// 若任务添加成功，则返回大于0的TaskId；
        /// 若任务添加失败，则返回-1；
        /// </summary>
        /// <param name="intervalTime">毫秒级别时间间隔</param>
        /// <param name="taskCallback">执行回调</param>
        /// <param name="cancelCallback">任务取消回调</param>
        /// <param name="loopCount">执行次数</param>
        /// <returns>添加事件成功后返回的ID</returns>
        public int AddTask(int intervalTime, Action<int> taskCallback, Action<int> cancelCallback, int loopCount = 1)
        {
            return AddTask(intervalTime, 0, taskCallback, cancelCallback, loopCount);
        }
        /// <summary>
        /// 添加任务；
        /// 若任务添加成功，则返回大于0的TaskId；
        /// 若任务添加失败，则返回-1；
        /// </summary>
        /// <param name="intervalTime">毫秒级别时间间隔</param>
        /// <param name="delayTime">毫秒级别时间延迟</param>
        /// <param name="taskCallback">执行回调</param>
        /// <param name="cancelCallback">任务取消回调</param>
        /// <param name="loopCount">执行次数</param>
        /// <returns>添加事件成功后返回的ID</returns>
        public int AddTask(int intervalTime, int delayTime, Action<int> taskCallback, Action<int> cancelCallback, int loopCount = 1)
        {
            int tid = GenerateTaskId();
            double startTime = GetUTCMilliseconds() + delayTime;
            double destTime = startTime + intervalTime;
            
            TickTask task = usePool ? 
                AcquireTickTask(tid, loopCount, intervalTime, destTime, taskCallback, cancelCallback, startTime) :
                new TickTask(tid, loopCount, intervalTime, destTime, taskCallback, cancelCallback, startTime);
            
            if (!taskDict.TryAdd(tid, task))
            {
                if (usePool)
                    ReleaseTickTask(task);
                return -1;
            }
            
            // 添加到时间槽
            AddTaskToTimeSlot(task);
            return tid;
        }
        /// <summary>
        /// 移除任务；
        /// </summary>
        /// <param name="taskId">任务Id</param>
        /// <returns>是否移除成功</returns>
        public bool RemoveTask(int taskId)
        {
            if (!taskDict.TryRemove(taskId, out TickTask task))
                return false;
            task.CancelCallback?.Invoke(taskId);
            if (usePool)
                ReleaseTickTask(task);
            return true;
        }
        public bool PauseTask(int taskId)
        {
            if (!taskDict.TryGetValue(taskId, out TickTask task))
                return false;
            task.IsPause = true;
            var remainTime = task.DestTime - GetUTCMilliseconds();
            task.PauseRemainTime = remainTime > 0 ? remainTime : 0;
            return true;
        }
        public bool UnPauseTask(int taskId)
        {
            if (!taskDict.TryGetValue(taskId, out TickTask task))
                return false;
            task.IsPause = false;
            task.DestTime = task.PauseRemainTime + GetUTCMilliseconds();
            return true;
        }
        public bool HasTask(int taskId)
        {
            return taskDict.ContainsKey(taskId);
        }
        /// <summary>
        /// 优化后的轮询方法；
        /// </summary>
        public void TickRefresh()
        {
            if (pause)
                return;
            
            try
            {
                double nowTime = GetUTCMilliseconds();
                
                // 清理过期时间槽
                expiredSlots.Clear();
                
                // 只处理已到期的时间槽
                foreach (var kvp in timeSlots)
                {
                    if (kvp.Key > nowTime)
                        break; // 由于是排序的，后面的都未到期
                        
                    expiredSlots.Enqueue(kvp.Key);
                    var tasks = kvp.Value;
                    
                    // 批量处理到期任务
                    for (int i = tasks.Count - 1; i >= 0; i--)
                    {
                        var task = tasks[i];
                        if (task.IsPause)
                        {
                            tasks.RemoveAt(i);
                            continue;
                        }
                        
                        ProcessExpiredTask(task, nowTime);
                        tasks.RemoveAt(i);
                    }
                }
                
                // 清理空的时间槽
                while (expiredSlots.Count > 0)
                {
                    var expiredTime = expiredSlots.Dequeue();
                    timeSlots.Remove(expiredTime);
                }
            }
            catch (Exception e)
            {
                LogError?.Invoke(e.ToString());
            }
        }
        
        private void ProcessExpiredTask(TickTask task, double nowTime)
        {
            ++task.LoopIndex;
            
            if (task.LoopIndex < task.LoopCount)
            {
                // 重新调度任务
                task.DestTime = nowTime + task.IntervalTime;
                AddTaskToTimeSlot(task);
                task.TaskCallback?.Invoke(task.TaskId);
            }
            else
            {
                // 完成任务，移除并回调
                if (taskDict.TryRemove(task.TaskId, out _))
                {
                    task.TaskCallback?.Invoke(task.TaskId);
                    task.CancelCallback = null;
                    if (usePool)
                        ReleaseTickTask(task);
                }
            }
        }
        
        private void AddTaskToTimeSlot(TickTask task)
        {
            if (!timeSlots.TryGetValue(task.DestTime, out var taskList))
            {
                taskList = new List<TickTask>();
                timeSlots[task.DestTime] = taskList;
            }
            taskList.Add(task);
        }
        /// <summary>
        /// 重置计时器；
        /// </summary>
        public void Reset()
        {
            pause = false;
            taskDict.Clear();
            if (usePool)
                taskQueue.Clear();
        }
        int GenerateTaskId()
        {
            ++taskIndex;
            while (taskDict.ContainsKey(taskIndex))
            {
                ++taskIndex;
                if (taskIndex == int.MaxValue)
                    taskIndex = 0;
            }
            return taskIndex;
        }
        /// <summary>
        /// 获取毫秒级别时间；
        /// </summary>
        double GetUTCMilliseconds()
        {
            TimeSpan ts = DateTime.UtcNow - startDateTime;
            return ts.TotalMilliseconds;
        }
        TickTask AcquireTickTask(int taskId, int loopCount, int intervalTime, double destTime, Action<int> taskCallback, Action<int> cancelCallback, double startTime)
        {
            TickTask task = null;
            if (taskQueue.Count > 0)
            {
                task = taskQueue.Dequeue();
                task.TaskId = taskId;
                task.LoopCount = loopCount;
                task.IntervalTime = intervalTime;
                task.DestTime = destTime;
                task.TaskCallback = taskCallback;
                task.CancelCallback = cancelCallback;
                task.StartTime = startTime;
                task.IsPause = false;
                task.PauseRemainTime = 0;
            }
            else
                task = new TickTask(taskId, loopCount, intervalTime, destTime, taskCallback, cancelCallback, startTime);
            return task;
        }
        void ReleaseTickTask(TickTask task)
        {
            task.Dispose();
            taskQueue.Enqueue(task);
        }
    }
}
