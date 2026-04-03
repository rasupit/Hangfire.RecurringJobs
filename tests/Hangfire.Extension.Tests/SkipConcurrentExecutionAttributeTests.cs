using Hangfire;
using Hangfire.Common;
using Hangfire.Extension;
using Hangfire.Server;
using Hangfire.Storage;
using NSubstitute;

namespace Hangfire.Extension.Tests;

public sealed class SkipConcurrentExecutionAttributeTests
{
    [Fact]
    public void Constructor_Throws_WhenTimeoutIsNotPositive()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new SkipConcurrentExecutionAttribute(0));

        Assert.Equal("timeoutInSeconds", exception.ParamName);
    }

    [Fact]
    public void OnPerforming_AcquiresDistributedLock_AndOnPerformed_DisposesIt()
    {
        var connection = Substitute.For<IStorageConnection>();
        var cancellationToken = Substitute.For<IJobCancellationToken>();
        var lockHandle = Substitute.For<IDisposable>();
        var attribute = new SkipConcurrentExecutionAttribute(15);
        var performContext = CreatePerformContext(connection, cancellationToken);
        var performingContext = new PerformingContext(performContext);

        connection.AcquireDistributedLock(
                "Hangfire.Extension.Tests.SkipConcurrentExecutionAttributeTests+SampleJob.Run",
                TimeSpan.FromSeconds(15))
            .Returns(lockHandle);

        attribute.OnPerforming(performingContext);

        Assert.False(performingContext.Canceled);
        connection.Received(1).AcquireDistributedLock(
            "Hangfire.Extension.Tests.SkipConcurrentExecutionAttributeTests+SampleJob.Run",
            TimeSpan.FromSeconds(15));

        var performedContext = new PerformedContext(performingContext, result: null, canceled: false, exception: null);
        attribute.OnPerformed(performedContext);

        lockHandle.Received(1).Dispose();
    }

    [Fact]
    public void OnPerforming_CancelsJob_WhenLockCannotBeAcquired()
    {
        var connection = Substitute.For<IStorageConnection>();
        var cancellationToken = Substitute.For<IJobCancellationToken>();
        var attribute = new SkipConcurrentExecutionAttribute(5);
        var performContext = CreatePerformContext(connection, cancellationToken);
        var performingContext = new PerformingContext(performContext);

        connection
            .When(x => x.AcquireDistributedLock(Arg.Any<string>(), Arg.Any<TimeSpan>()))
            .Do(_ => throw new DistributedLockTimeoutException("lock-timeout"));

        attribute.OnPerforming(performingContext);

        Assert.True(performingContext.Canceled);
    }

    private static PerformContext CreatePerformContext(
        IStorageConnection connection,
        IJobCancellationToken cancellationToken)
    {
        var storage = Substitute.For<JobStorage>();
        var job = Job.FromExpression(() => SampleJob.Run());
        var backgroundJob = new BackgroundJob("job-1", job, DateTime.UtcNow);

        return new PerformContext(storage, connection, backgroundJob, cancellationToken);
    }

    private static class SampleJob
    {
        public static void Run()
        {
        }
    }
}
