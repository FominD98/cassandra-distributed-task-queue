﻿using System;

using RemoteQueue.Cassandra.Entities;
using RemoteQueue.Cassandra.Repositories.Indexes;
using RemoteQueue.Configuration;

using SKBKontur.Catalogue.RemoteTaskQueue.Common;

namespace FunctionalTests
{
    public abstract class FunctionalTestBase : FunctionalTestBaseWithoutServices
    {
        public override void SetUp()
        {
            base.SetUp();
            var exchangeServiceClient = Container.Get<IExchangeServiceClient>();
            exchangeServiceClient.Start();
            exchangeServiceClient.ChangeTaskTtl(TimeSpan.FromHours(24));
            taskDataRegistry = Container.Get<ITaskDataRegistry>();
        }

        public override void TearDown()
        {
            Container.Get<IExchangeServiceClient>().Stop();
            base.TearDown();
        }

        protected TaskIndexShardKey TaskIndexShardKey(string taskName, TaskState taskState)
        {
            return new TaskIndexShardKey(taskDataRegistry.GetTaskTopic(taskName), taskState);
        }

        private ITaskDataRegistry taskDataRegistry;
    }
}