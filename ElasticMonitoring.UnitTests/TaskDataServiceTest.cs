﻿using System;

using NUnit.Framework;

using RemoteQueue.Handling;

using Rhino.Mocks;

using SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.TaskIndexedStorage.Writing;
using SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.TaskIndexedStorage.Writing.Contracts;
using SKBKontur.Catalogue.TestCore;

namespace SKBKontur.Catalogue.RemoteTaskQueue.ElasticMonitoring.UnitTests
{
    public class TaskDataServiceTest : CoreTestBase
    {
        public override void SetUp()
        {
            base.SetUp();
            mapper = GetMock<ITaskDataTypeToNameMapper>();
            taskDataService = new TaskDataService(mapper);
        }

        [Test]
        public void TestUnknownType()
        {
            mapper.Expect(m => m.TryGetTaskType(ARG.EqualsTo("zzz"), out ARG.Out<Type>(null).Dummy)).Return(false);
            var meta = new MetaIndexedInfo() {Name = "zzz"};
            taskDataService.CreateTaskIndexedInfo(meta, null).AssertEqualsTo(new TaskIndexedInfo<TaskDataService.UnknownData>(meta, null));
        }

        [Test]
        public void TestConstruct()
        {
            mapper.Expect(m => m.TryGetTaskType(ARG.EqualsTo("C1"), out ARG.Out(typeof(C1)).Dummy)).Return(true);
            var meta = new MetaIndexedInfo() {Name = "C1"};
            var data = new C1 {X = 1};
            taskDataService.CreateTaskIndexedInfo(meta, data).AssertEqualsTo(new TaskIndexedInfo<C1>(meta, data));

            var data2 = new C1 {X = 1};
            taskDataService.CreateTaskIndexedInfo(meta, data2).AssertEqualsTo(new TaskIndexedInfo<C1>(meta, data2));

            mapper.Expect(m => m.TryGetTaskType(ARG.EqualsTo("C2"), out ARG.Out(typeof(C2)).Dummy)).Return(true);
            var meta3 = new MetaIndexedInfo() {Name = "C2"};
            var data3 = new C2 {Z = 2};
            taskDataService.CreateTaskIndexedInfo(meta3, data3).AssertEqualsTo(new TaskIndexedInfo<C2>(meta3, data3));
        }

        private ITaskDataTypeToNameMapper mapper;
        private TaskDataService taskDataService;

        // ReSharper disable NotAccessedField.Local
        private class C1
        {
            public int X;
        }

        private class C2
        {
            public int Z;
        }

        // ReSharper restore NotAccessedField.Local
    }
}