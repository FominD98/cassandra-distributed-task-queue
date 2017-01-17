﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using FluentAssertions;

using GroBuf;

using NUnit.Framework;

using RemoteQueue.Cassandra.Entities;
using RemoteQueue.Cassandra.Repositories;
using RemoteQueue.Cassandra.Repositories.BlobStorages;
using RemoteQueue.Handling;

using RemoteTaskQueue.FunctionalTests.Common;
using RemoteTaskQueue.FunctionalTests.Common.TaskDatas.MonitoringTestTaskData;
using RemoteTaskQueue.Monitoring.Storage.Client;

using SKBKontur.Catalogue.NUnit.Extensions.EdiTestMachinery;
using SKBKontur.Catalogue.Objects;
using SKBKontur.Catalogue.ServiceLib.Logging;
using SKBKontur.Catalogue.TestCore.Waiting;

namespace RemoteTaskQueue.FunctionalTests.Monitoring
{
    public class TaskSearchClientTests : MonitoringTestBase
    {
        [Test]
        public void TestCreateNotDeserializedTaskData()
        {
            var t0 = Timestamp.Now;
            var taskId = QueueTask(new SlowTaskData(), TimeSpan.FromSeconds(1));
            Log.For(this).InfoFormat("TaskId: {0}", taskId);
            var taskMetaInformation = handleTasksMetaStorage.GetMeta(taskId);

            var badBytes = new byte[] {};
            Assert.Catch<Exception>(() => serializer.Deserialize<SlowTaskData>(badBytes));

            taskDataStorage.Overwrite(taskMetaInformation, badBytes);

            var taskId2 = QueueTask(new SlowTaskData(), TimeSpan.FromSeconds(2));

            WaitForTasks(new[] {taskId, taskId2}, TimeSpan.FromSeconds(5));

            var t1 = Timestamp.Now;

            monitoringServiceClient.UpdateAndFlush();

            CheckSearch(string.Format("Meta.Id:\"{0}\"", taskId), t0, t1, taskId);
            CheckSearch(string.Format("Meta.Id:\"{0}\"", taskId2), t0, t1, taskId2);
        }

        [Test]
        public void TestStupid()
        {
            var t0 = Timestamp.Now;

            var taskId = QueueTask(new SlowTaskData());
            WaitForTasks(new[] {taskId}, TimeSpan.FromSeconds(5));

            monitoringServiceClient.UpdateAndFlush();

            var t1 = Timestamp.Now;
            CheckSearch("*", t0, t1, taskId);
            CheckSearch(string.Format("\"{0}\"", taskId), t0, t1, taskId);
            CheckSearch(string.Format("Meta.Id:\"{0}\"", taskId), t0, t1, taskId);
            CheckSearch(string.Format("Meta.Name:{0}", typeof(SlowTaskData).Name), t0, t1, taskId);
            CheckSearch("Meta.Name:Zzz", t0, t1);
        }

        [Test]
        public void TestSearchByExceptionSubstring()
        {
            var t0 = Timestamp.Now;

            var uniqueData = Guid.NewGuid();
            Log.For(this).InfoFormat("ud={0}", uniqueData);
            var taskId = QueueTask(new FailingTaskData {UniqueData = uniqueData, RetryCount = 0});
            WaitForTasks(new[] {taskId}, TimeSpan.FromSeconds(5));

            monitoringServiceClient.UpdateAndFlush();

            var t1 = Timestamp.Now;
            CheckSearch(string.Format("\"{0}\"", taskId), t0, t1, taskId);
            CheckSearch(string.Format("\"{0}\"", uniqueData), t0, t1, taskId);
            CheckSearch(string.Format("ExceptionInfo:\"{0}\"", uniqueData), t0, t1, taskId);
            CheckSearch(string.Format("\"{0}\"", Guid.NewGuid()), t0, t1);
        }

        [Test]
        public void TestSearchByExpiration()
        {
            var t0 = Timestamp.Now;

            var taskId = QueueTask(new SlowTaskData());
            WaitForTasks(new[] {taskId}, TimeSpan.FromSeconds(5));

            monitoringServiceClient.UpdateAndFlush();

            var t1 = Timestamp.Now;
            var ttl = TestRemoteTaskQueueSettings.StandardTestTaskTtl;
            Log.For(this).Info(ToIsoTime(new Timestamp(remoteTaskQueue.GetTaskInfo<SlowTaskData>(taskId).Context.ExpirationTimestampTicks.Value)));
            CheckSearch(string.Format("Meta.ExpirationTime: [\"{0}\" TO \"{1}\"]", ToIsoTime(t0 + ttl), ToIsoTime(t1 + ttl + TimeSpan.FromSeconds(10))), t0, t1, taskId);
        }

        private static string ToIsoTime(Timestamp timestamp)
        {
            return timestamp.ToDateTime().ToString("O");
        }

        [Test]
        public void TestSearchByExceptionSubstring_MultipleDifferentErrors()
        {
            var t0 = Timestamp.Now;

            var uniqueData = Guid.NewGuid();
            Log.For(this).InfoFormat("ud={0}", uniqueData);
            var failingTaskData = new FailingTaskData {UniqueData = uniqueData, RetryCount = 2};
            var taskId = QueueTask(failingTaskData);
            WaitForTasks(new[] {taskId}, TimeSpan.FromSeconds(30));

            monitoringServiceClient.UpdateAndFlush();

            var t1 = Timestamp.Now;
            CheckSearch(string.Format("\"{0}\"", taskId), t0, t1, taskId);
            CheckSearch(string.Format("\"{0}\"", uniqueData), t0, t1, taskId);
            CheckSearch(string.Format("\"{0}\"", Guid.NewGuid()), t0, t1);

            for(var attempts = 1; attempts <= 3; attempts++)
                CheckSearch(string.Format("\"FailingTask failed: {0}. Attempts = {1}\"", failingTaskData, attempts), t0, t1, taskId);
        }

        [Test]
        public void TestUpdateAndFlush()
        {
            var t0 = Timestamp.Now;
            for(var i = 0; i < 100; i++)
            {
                Log.For(this).InfoFormat("Iteration: {0}", i);
                var taskId0 = QueueTask(new SlowTaskData());
                WaitForTasks(new[] {taskId0}, TimeSpan.FromSeconds(5));
                monitoringServiceClient.UpdateAndFlush();
                CheckSearch(string.Format("\"{0}\"", taskId0), t0, Timestamp.Now, taskId0);
            }
        }

        [Test]
        public void TestDataSearchBug()
        {
            var t0 = Timestamp.Now;
            for(var i = 0; i < 100; i++)
            {
                Log.For(this).InfoFormat("Iteration: {0}", i);
                var taskId0 = QueueTask(new SlowTaskData());
                var taskId1 = QueueTask(new SlowTaskData());
                var taskId2 = QueueTask(new SlowTaskData());
                //Console.WriteLine("ids: {0} {1} {2}", taskId0, taskId1, taskId2);
                WaitForTasks(new[] {taskId0, taskId1, taskId2}, TimeSpan.FromSeconds(5));
                monitoringServiceClient.UpdateAndFlush();
                CheckSearch(string.Format("\"{0}\"", taskId0), t0, Timestamp.Now, taskId0);
            }
        }

        [Test]
        public void TestNotStupid()
        {
            var t0 = Timestamp.Now;
            var taskId0 = QueueTask(new SlowTaskData());
            WaitForTasks(new[] {taskId0}, TimeSpan.FromSeconds(5));

            var t1 = Timestamp.Now;

            var taskId1 = QueueTask(new AlphaTaskData());
            WaitForTasks(new[] {taskId1}, TimeSpan.FromSeconds(5));

            monitoringServiceClient.UpdateAndFlush();

            var t2 = Timestamp.Now;

            CheckSearch("*", t0, t2, taskId0, taskId1);

            CheckSearch(string.Format("Meta.Id:\"{0}\" OR Meta.Id:\"{1}\"", taskId0, taskId1), t0, t1, taskId0, taskId1);
            CheckSearch("Meta.State:Finished", t0, t1, taskId0, taskId1);

            CheckSearch("NOT _exists_:Meta.ParentTaskId", t0, t1, taskId0, taskId1);
            CheckSearch("NOT _exists_:Meta.Name", t0, t1);

            CheckSearch("Data.TimeMs:0", t0, t1, taskId0);
            CheckSearch("Data.UseCounter:false", t0, t1, taskId0);
        }

        [Test]
        public void TestPaging()
        {
            var t0 = Timestamp.Now;
            var taskIds = new List<string>();
            const int pageSize = 5;
            for(var i = 0; i < pageSize + pageSize / 2; i++)
            {
                taskIds.Add(QueueTask(new AlphaTaskData()));
                Thread.Sleep(TimeSpan.FromMilliseconds(1)); // note: elastic stores timestamps with millisecond precision
            }
            taskIds.Reverse();
            var expectedTaskIds = taskIds.ToArray();
            WaitForTasks(expectedTaskIds, TimeSpan.FromSeconds(60));
            monitoringServiceClient.UpdateAndFlush();
            var t1 = Timestamp.Now;

            var resultsPage1 = taskSearchClient.Search(new TaskSearchRequest
            {
                FromTicksUtc = t0.Ticks,
                ToTicksUtc = t1.Ticks,
                QueryString = "*",
            }, 0, pageSize);
            resultsPage1.TotalCount.Should().Be(expectedTaskIds.Length);
            resultsPage1.Ids.Should().Equal(expectedTaskIds.Take(pageSize).ToArray());

            var resultsPage2 = taskSearchClient.Search(new TaskSearchRequest
            {
                FromTicksUtc = t0.Ticks,
                ToTicksUtc = t1.Ticks,
                QueryString = "*",
            }, pageSize, pageSize);
            resultsPage2.TotalCount.Should().Be(expectedTaskIds.Length);
            resultsPage2.Ids.Should().Equal(expectedTaskIds.Skip(pageSize).ToArray());
        }

        private string QueueTask<T>(T taskData, TimeSpan? delay = null) where T : ITaskData
        {
            var task = remoteTaskQueue.CreateTask(taskData);
            task.Queue(delay ?? TimeSpan.Zero);
            return task.Id;
        }

        private void WaitForTasks(IEnumerable<string> taskIds, TimeSpan timeSpan)
        {
            WaitHelper.Wait(() =>
                {
                    var tasks = remoteTaskQueue.HandleTaskCollection.GetTasks(taskIds.ToArray());
                    return tasks.All(t => t.Meta.State == TaskState.Finished || t.Meta.State == TaskState.Fatal) ? WaitResult.StopWaiting : WaitResult.ContinueWaiting;
                }, timeSpan);
        }

        [Injected]
        private ISerializer serializer;

        [Injected]
        private HandleTasksMetaStorage handleTasksMetaStorage;

        [Injected]
        private TaskDataStorage taskDataStorage;
    }
}