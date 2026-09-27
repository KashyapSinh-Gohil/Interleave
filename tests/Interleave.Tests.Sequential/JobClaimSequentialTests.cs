using Interleave.Core;
using Xunit;

namespace Interleave.Tests.Sequential;

public sealed class JobClaimSequentialTests
{
    [Fact]
    public void Claim_ReturnsAlreadyExecutedForRepeatedJob()
    {
        var service = new JobClaimService();

        Assert.Equal(ClaimResult.Claimed, service.TryClaim("job-1"));
        Assert.Equal(ClaimResult.AlreadyExecuted, service.TryClaim("job-1"));
        Assert.Equal(1, service.ExecutionCount("job-1"));
    }

    [Fact]
    public void Claim_TracksDifferentJobsIndependently()
    {
        var service = new JobClaimService();

        Assert.Equal(ClaimResult.Claimed, service.TryClaim("job-1"));
        Assert.Equal(ClaimResult.Claimed, service.TryClaim("job-2"));
        Assert.Equal(1, service.ExecutionCount("job-1"));
        Assert.Equal(1, service.ExecutionCount("job-2"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Claim_RejectsBlankJobIds(string? jobId)
    {
        var service = new JobClaimService();

        Assert.ThrowsAny<ArgumentException>(() => service.TryClaim(jobId!));
        Assert.ThrowsAny<ArgumentException>(() => service.ExecutionCount(jobId!));
    }
}
