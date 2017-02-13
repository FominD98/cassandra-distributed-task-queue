using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using ExchangeService.Exceptions;
using ExchangeService.UserClasses;

using NUnit.Framework;

using RemoteQueue.Cassandra.Entities;
using RemoteQueue.Cassandra.Repositories.Indexes;
using RemoteQueue.Handling;

using SKBKontur.Catalogue.RemoteTaskQueue.TaskDatas;
using SKBKontur.Catalogue.TestCore.Waiting;

namespace FunctionalTests.ExchangeTests
{
    public class SimpleTaskTest : FunctionalTestBase
    {
        public override void SetUp()
        {
            base.SetUp();
            testCounterRepository = Container.Get<ITestCounterRepository>();
            taskQueue = Container.Get<RemoteTaskQueue>();
        }

        [Test]
        [Repeat(10)]
        public void TestRun()
        {
            var taskId = taskQueue.CreateTask(new SimpleTaskData()).Queue();
            Wait(new[] {taskId}, 1);
            Thread.Sleep(2000);
            Assert.AreEqual(1, testCounterRepository.GetCounter(taskId));
            Assert.AreEqual(TaskState.Finished, taskQueue.GetTaskInfo<SimpleTaskData>(taskId).Context.State);
            Container.CheckTaskMinimalStartTicksIndexStates(new Dictionary<string, TaskIndexShardKey>
                {
                    {taskId, TaskIndexShardKey("SimpleTaskData", TaskState.Finished)}
                });
        }

        [Test]
        public void TestRunMultipleTasks()
        {
            var taskIds = new List<string>();
            Enumerable.Range(0, 42).AsParallel().ForAll(x =>
                {
                    var taskId = taskQueue.CreateTask(new SimpleTaskData()).Queue();
                    lock(taskIds)
                        taskIds.Add(taskId);
                });
            WaitForTasksToFinish(taskIds, TimeSpan.FromSeconds(10));
            Container.CheckTaskMinimalStartTicksIndexStates(taskIds.ToDictionary(s => s, s => TaskIndexShardKey("SimpleTaskData", TaskState.Finished)));
        }

        [Test]
        public void TestCancel()
        {
            var taskId = taskQueue.CreateTask(new SimpleTaskData()).Queue(TimeSpan.FromSeconds(1));
            Assert.That(taskQueue.TryCancelTask(taskId), Is.EqualTo(TaskManipulationResult.Success));
            Wait(new[] {taskId}, 0);
            Thread.Sleep(2000);
            Assert.AreEqual(0, testCounterRepository.GetCounter(taskId));
            Assert.AreEqual(TaskState.Canceled, taskQueue.GetTaskInfo<SimpleTaskData>(taskId).Context.State);
            Container.CheckTaskMinimalStartTicksIndexStates(new Dictionary<string, TaskIndexShardKey>
                {
                    {taskId, TaskIndexShardKey("SimpleTaskData", TaskState.Canceled)}
                });
        }

        [Test]
        public void TestCancel_UnknownTask()
        {
            Assert.That(taskQueue.TryCancelTask(Guid.NewGuid().ToString()), Is.EqualTo(TaskManipulationResult.Failure_TaskDoesNotExist));
        }

        [Test]
        public void TestCancel_LockAcquiringFails()
        {
            var taskId = taskQueue.CreateTask(new SimpleTaskData()).Queue(TimeSpan.FromSeconds(5));
            var remoteLockCreator = ((RemoteTaskQueue)taskQueue).RemoteLockCreator;
            using(remoteLockCreator.Lock(taskId))
            {
                Assert.That(taskQueue.TryCancelTask(taskId), Is.EqualTo(TaskManipulationResult.Failure_LockAcquiringFails));
            }
        }

        [Test]
        public void TestRerun()
        {
            var taskId = taskQueue.CreateTask(new SimpleTaskData()).Queue();
            Wait(new[] {taskId}, 1);
            Assert.That(taskQueue.TryRerunTask(taskId, TimeSpan.FromMilliseconds(1)), Is.EqualTo(TaskManipulationResult.Success));
            Wait(new[] {taskId}, 2);
            Thread.Sleep(2000);
            Assert.AreEqual(2, testCounterRepository.GetCounter(taskId));
            var taskMeta = taskQueue.GetTaskInfo<SimpleTaskData>(taskId).Context;
            Assert.AreEqual(TaskState.Finished, taskMeta.State);
            Assert.AreEqual(2, taskMeta.Attempts);
            Container.CheckTaskMinimalStartTicksIndexStates(new Dictionary<string, TaskIndexShardKey>
                {
                    {taskId, TaskIndexShardKey("SimpleTaskData", TaskState.Finished)}
                });
        }

        [Test]
        public void TestRerun_UnknownTask()
        {
            Assert.That(taskQueue.TryRerunTask(Guid.NewGuid().ToString(), TimeSpan.Zero), Is.EqualTo(TaskManipulationResult.Failure_TaskDoesNotExist));
        }

        [Test]
        public void TestRerun_LockAcquiringFails()
        {
            var taskId = taskQueue.CreateTask(new SimpleTaskData()).Queue(TimeSpan.FromSeconds(5));
            using(taskQueue.RemoteLockCreator.Lock(taskId))
                Assert.That(taskQueue.TryRerunTask(taskId, TimeSpan.Zero), Is.EqualTo(TaskManipulationResult.Failure_LockAcquiringFails));
        }

        private void Wait(string[] taskIds, int criticalValue, int ms = 5000)
        {
            var current = 0;
            while(true)
            {
                var attempts = taskIds.Select(testCounterRepository.GetCounter).ToArray();
                Console.WriteLine(Now() + " CurrentValues: " + string.Join(", ", attempts));
                var minValue = attempts.Min();
                if(minValue >= criticalValue)
                    break;
                Thread.Sleep(sleepInterval);
                current += sleepInterval;
                if(current > ms)
                    throw new TooLateException("����� �������� ��������� {0} ��.", ms);
            }
        }

        private void WaitForTasksToFinish(IEnumerable<string> taskIds, TimeSpan timeSpan)
        {
            WaitHelper.Wait(() =>
                {
                    var tasks = taskQueue.HandleTaskCollection.GetTasks(taskIds.ToArray());
                    return tasks.All(t => t.Meta.State == TaskState.Finished) ? WaitResult.StopWaiting : WaitResult.ContinueWaiting;
                }, timeSpan);
        }

        private static string Now()
        {
            return DateTime.UtcNow.ToString("dd.MM.yyyy mm:hh:ss.ffff");
        }

        private const int sleepInterval = 200;
        private ITestCounterRepository testCounterRepository;
        private RemoteTaskQueue taskQueue;
    }
}