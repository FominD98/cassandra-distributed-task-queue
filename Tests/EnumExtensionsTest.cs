using NUnit.Framework;

using RemoteQueue.Cassandra.Entities;

namespace RemoteQueue.Tests
{
    [TestFixture]
    public class EnumExtensionsTest
    {
        [Test]
        public void GetCassandraName_TaskState()
        {
            Assert.That(TaskState.Unknown.GetCassandraName(), Is.EqualTo("Unknown"));
            Assert.That(TaskState.New.GetCassandraName(), Is.EqualTo("New"));
            Assert.That(TaskState.WaitingForRerun.GetCassandraName(), Is.EqualTo("WaitingForRerun"));
            Assert.That(TaskState.WaitingForRerunAfterError.GetCassandraName(), Is.EqualTo("WaitingForRerunAfterError"));
            Assert.That(TaskState.Finished.GetCassandraName(), Is.EqualTo("Finished"));
            Assert.That(TaskState.InProcess.GetCassandraName(), Is.EqualTo("InProcess"));
            Assert.That(TaskState.Fatal.GetCassandraName(), Is.EqualTo("Fatal"));
            Assert.That(TaskState.Canceled.GetCassandraName(), Is.EqualTo("Canceled"));
        }

        [Test]
        public void GetCassandraName_EnumsWithTheSameValues()
        {
            Assert.That((int)AnotherEnum.Whatever, Is.EqualTo((int)TaskState.Unknown));
            Assert.That(TaskState.Unknown.GetCassandraName(), Is.EqualTo("Unknown"));
            Assert.That(AnotherEnum.Whatever.GetCassandraName(), Is.EqualTo("Whatever"));
        }

        private enum AnotherEnum
        {
            [CassandraName("Whatever")]
            Whatever = 0,
        }
    }
}