using Interleave.Core;
using Microsoft.Coyote;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using Xunit;
using Xunit.Abstractions;

namespace Interleave.Tests.Concurrency;

public sealed class DuplicateJobClaimTests
{
    private readonly ITestOutputHelper _output;

    public DuplicateJobClaimTests(ITestOutputHelper output) => _output = output;

    private static async Task RunConcurrentClaims()
    {
        const string jobId = "job-1";
        var service = new JobClaimService();

        Task<ClaimResult> workerA = Task.Run(() => service.TryClaim(jobId));
        Task<ClaimResult> workerB = Task.Run(() => service.TryClaim(jobId));
        await Task.WhenAll(workerA, workerB);

        int claimedCount = (workerA.Result == ClaimResult.Claimed ? 1 : 0) +
                           (workerB.Result == ClaimResult.Claimed ? 1 : 0);
        Specification.Assert(
            claimedCount <= 1,
            $"INVARIANT VIOLATED: job '{jobId}' was claimed by {claimedCount} workers " +
            $"(workerA={workerA.Result}, workerB={workerB.Result}).");
        Specification.Assert(
            service.ExecutionCount(jobId) == claimedCount,
            "The stored claim count must match the number of successful results.");
    }

    [Fact]
    public void SystematicTest_DuplicateJobClaim_HasAtMostOneWinner()
    {
        var configuration = Configuration.Create()
            .WithTestingIterations(500)
            .WithMaxSchedulingSteps(1000)
            .WithVerbosityEnabled(VerbosityLevel.Error);

        var engine = TestingEngine.Create(configuration, RunConcurrentClaims);
        engine.Run();
        var report = engine.TestReport;

        _output.WriteLine($"[INTERLEAVE][DJC] Explored {report.NumOfExploredFairPaths} fair, " +
                          $"{report.NumOfExploredUnfairPaths} unfair paths; " +
                          $"{report.NumOfFoundBugs} bug(s).");
        foreach (var bug in report.BugReports)
            _output.WriteLine($"[INTERLEAVE][DJC] Bug: {bug}");

        Assert.Equal(0, report.NumOfFoundBugs);
        Assert.True(report.NumOfExploredFairPaths + report.NumOfExploredUnfairPaths > 0,
            "Coyote explored no schedules; the workload may not have executed.");
    }

    [Fact]
    public void ReplayAttempt_DuplicateJobClaim_FrozenTrace_NoBugReported()
    {
        string? tracePath = Environment.GetEnvironmentVariable("INTERLEAVE_JOB_CLAIM_TRACE");
        Assert.True(!string.IsNullOrWhiteSpace(tracePath) && File.Exists(tracePath),
            "Set INTERLEAVE_JOB_CLAIM_TRACE to the frozen Duplicate Job Claim .trace file.");

        string traceJson = File.ReadAllText(tracePath!);
        var configuration = Configuration.Create()
            .WithReproducibleTrace(traceJson)
            .WithVerbosityEnabled(VerbosityLevel.Info);

        var engine = TestingEngine.Create(configuration, RunConcurrentClaims);
        engine.Run();
        var report = engine.TestReport;

        _output.WriteLine($"[INTERLEAVE][DJC] Supplied frozen trace: {tracePath}");
        _output.WriteLine($"[INTERLEAVE][DJC] Replay attempt reported {report.NumOfFoundBugs} bug(s), " +
                          $"{report.NumOfExploredFairPaths} fair and " +
                          $"{report.NumOfExploredUnfairPaths} unfair path(s). " +
                          "Exact schedule alignment is not inferred from a zero-bug report.");
        foreach (var bug in report.BugReports)
            _output.WriteLine($"[INTERLEAVE][DJC] Bug: {bug}");

        Assert.Equal(0, report.NumOfFoundBugs);
    }
}
