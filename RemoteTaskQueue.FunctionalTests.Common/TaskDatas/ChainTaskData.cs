﻿using RemoteQueue.Configuration;
using RemoteQueue.Handling;

namespace RemoteTaskQueue.FunctionalTests.Common.TaskDatas
{
    [TaskName("ChainTaskData")]
    public class ChainTaskData : ITaskData
    {
        public string ChainName { get; set; }
        public int ChainPosition { get; set; }
        public string LoggingTaskIdKey { get; set; }
    }
}