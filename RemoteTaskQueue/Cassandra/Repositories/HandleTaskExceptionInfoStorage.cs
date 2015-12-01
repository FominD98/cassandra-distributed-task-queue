using System;
using System.Collections.Generic;
using System.Linq;

using log4net;

using RemoteQueue.Cassandra.Entities;
using RemoteQueue.Cassandra.Repositories.BlobStorages;

namespace RemoteQueue.Cassandra.Repositories
{
    public class HandleTaskExceptionInfoStorage : IHandleTaskExceptionInfoStorage
    {
        public HandleTaskExceptionInfoStorage(ITaskExceptionInfoBlobStorage storage)
        {
            this.storage = storage;
            logger = LogManager.GetLogger(typeof(HandleTaskExceptionInfoStorage));
        }

        public void TryAddExceptionInfo(string taskId, Exception e)
        {
            try
            {
                storage.Write(taskId, new TaskExceptionInfo
                    {
                        ExceptionMessageInfo = e.ToString()
                    });
            }
            catch
            {
                logger.ErrorFormat("�� ������ �������� ������ ��� ������ '{0}'. {1}", taskId, e);
            }
        }

        public bool TryGetExceptionInfo(string taskId, out TaskExceptionInfo exceptionInfo)
        {
            exceptionInfo = storage.Read(taskId);
            return exceptionInfo != null;
        }

        public IDictionary<string, TaskExceptionInfo> ReadExceptionInfos(string[] taskIds)
        {
            return storage.Read(taskIds);
        }

        private readonly ITaskExceptionInfoBlobStorage storage;
        private readonly ILog logger;
    }
}