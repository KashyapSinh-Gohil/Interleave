namespace Interleave.Core;

/// <summary>Outcome of one attempt to claim a job for execution.</summary>
public enum ClaimResult
{
    Claimed,
    AlreadyExecuted,
}

/// <summary>
/// In-memory, thread-safe claim registry for the INTERLEAVE proof of concept.
/// A successful claim is recorded exactly once per job ID.
/// </summary>
public sealed class JobClaimService
{
    private readonly object _gate = new();
    private readonly HashSet<string> _claimedJobs = new(StringComparer.Ordinal);

    /// <summary>Returns whether the job has already been claimed (zero or one).</summary>
    public int ExecutionCount(string jobId)
    {
        ValidateJobId(jobId);
        lock (_gate)
        {
            return _claimedJobs.Contains(jobId) ? 1 : 0;
        }
    }

    /// <summary>
    /// Atomically claims a job. Exactly one concurrent caller can receive
    /// <see cref="ClaimResult.Claimed"/>; later callers receive
    /// <see cref="ClaimResult.AlreadyExecuted"/>.
    /// </summary>
    public ClaimResult TryClaim(string jobId)
    {
        ValidateJobId(jobId);
        lock (_gate)
        {
            return _claimedJobs.Add(jobId)
                ? ClaimResult.Claimed
                : ClaimResult.AlreadyExecuted;
        }
    }

    private static void ValidateJobId(string jobId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
    }
}
