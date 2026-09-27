// INTERLEAVE — Systematic concurrency tests (Coyote) — M1 repaired
//
// Three test groups:
//
//  1. SystematicTest_LastSeat_FindsNoViolation
//       Post-fix exploration.  Coyote must find ZERO bugs across 500 schedules.
//       A non-zero bug count means the fix is incomplete.
//
//  2. ReplayAttempt_LastSeat_FrozenTrace_NoBugReported
//       Supplies the frozen pre-fix witness to the repaired binary and asserts
//       0 bugs are reported.  The test passes when Coyote reports 0 bugs and
//       fails when Coyote reports any bug (fix is incomplete) or when
//       INTERLEAVE_WITNESS_TRACE is absent/invalid (no attempt was made).
//       A pass records the observed outcome only; exact schedule alignment is
//       UNRESOLVED — see docs/milestones.md and docs/replay.md.
//
//  3. Regression group (Coyote systematic):
//       - Regression_ZeroCapacity_RejectsAllReservations
//       - Regression_OneSeatContention_AvailableSeatsNeverNegative
//       - Regression_AvailableSeats_ConsistentWithReservationCount
//       - Regression_ReservationList_NoDuplicateIds
//       - Regression_ConcurrentReads_AreSafeSnapshots
//
// IMPORTANT: Before running these tests the build output must be rewritten with:
//   coyote rewrite coyote.rewrite.json
// The GitHub Actions workflow handles this automatically.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Interleave.Core;
using Microsoft.Coyote;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using Xunit;
using Xunit.Abstractions;

namespace Interleave.Tests.Concurrency;

public sealed class LastSeatConcurrencyTests
{
    private readonly ITestOutputHelper _output;

    public LastSeatConcurrencyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Shared workload used by exploration and regression tests
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Core workload: two concurrent TryReserve calls against capacity 1.
    /// Asserts capacity safety, successful completion for the one available seat,
    /// and agreement between returned reservations and persisted state.
    /// </summary>
    private static async Task RunConcurrentReservations()
    {
        const int capacity = 1;
        var service = new SeatReservationService(capacity: capacity);

        Task<Reservation?> taskAlice = Task.Run(() => service.TryReserve("alice"));
        Task<Reservation?> taskBob   = Task.Run(() => service.TryReserve("bob"));

        await Task.WhenAll(taskAlice, taskBob);

        // Count successful return values (non-null results from TryReserve).
        int successfulReturns = (taskAlice.Result != null ? 1 : 0) +
                                (taskBob.Result   != null ? 1 : 0);

        // Capacity safety and liveness: exactly one of two callers must receive
        // the sole available seat.
        Specification.Assert(
            successfulReturns == capacity,
            $"Expected exactly {capacity} successful reservation for two callers, " +
            $"but observed {successfulReturns}. " +
            $"(alice={taskAlice.Result?.ReservationId ?? "null"}, " +
            $"bob={taskBob.Result?.ReservationId ?? "null"})");

        // Return values must match stored reservations: every non-null return
        // value must match the persisted reservation, including its holder.
        IReadOnlyList<Reservation> stored = service.Reservations;
        if (taskAlice.Result != null)
        {
            Specification.Assert(
                stored.Any(r => r.ReservationId == taskAlice.Result.ReservationId &&
                                r.HolderId == taskAlice.Result.HolderId),
                $"alice's returned reservation '{taskAlice.Result.ReservationId}' " +
                $"is not present in the stored reservation list.");
        }
        if (taskBob.Result != null)
        {
            Specification.Assert(
                stored.Any(r => r.ReservationId == taskBob.Result.ReservationId &&
                                r.HolderId == taskBob.Result.HolderId),
                $"bob's returned reservation '{taskBob.Result.ReservationId}' " +
                $"is not present in the stored reservation list.");
        }

        // Stored reservation count must also not exceed capacity.
        Specification.Assert(
            stored.Count <= capacity,
            $"Stored reservation count ({stored.Count}) exceeds capacity ({capacity}).");

        Specification.Assert(
            stored.Count == successfulReturns,
            $"Successful return count ({successfulReturns}) does not match stored reservation count ({stored.Count}).");

        Specification.Assert(
            service.AvailableSeats + stored.Count == capacity,
            $"Available seats ({service.AvailableSeats}) plus stored reservations ({stored.Count}) " +
            $"does not equal capacity ({capacity}).");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. Post-fix systematic exploration
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Systematic concurrency test against the REPAIRED implementation.
    /// Coyote explores up to 500 schedules; zero bugs must be found.
    /// A non-zero count indicates the fix does not cover the explored schedules.
    /// </summary>
    [Fact]
    public void SystematicTest_LastSeat_FindsNoViolation()
    {
        var configuration = Configuration.Create()
            .WithTestingIterations(500)
            .WithMaxSchedulingSteps(1000)
            .WithVerbosityEnabled(VerbosityLevel.Error);

        var engine = TestingEngine.Create(configuration, RunConcurrentReservations);
        engine.Run();

        var report = engine.TestReport;

        _output.WriteLine($"[INTERLEAVE] Explored {report.NumOfExploredFairPaths} fair paths, " +
                          $"{report.NumOfExploredUnfairPaths} unfair paths.");
        _output.WriteLine($"[INTERLEAVE] Bugs found: {report.NumOfFoundBugs}");

        foreach (var bug in report.BugReports)
        {
            _output.WriteLine($"[INTERLEAVE] Bug report: {bug}");
        }

        // Post-fix: the systematic test must find NO bugs.
        // If this assertion fails, the repair is incomplete.
        Assert.Equal(0, report.NumOfFoundBugs);

        // At least one path must have been explored; a zero count indicates the
        // engine did not actually run (e.g. misconfigured workload).
        Assert.True(report.NumOfExploredFairPaths + report.NumOfExploredUnfairPaths > 0,
            "Coyote explored 0 paths — the workload may not have executed.");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. Trace-based replay attempt against repaired source
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Supplies the frozen pre-fix witness to the repaired binary under
    /// Coyote's WithReproducibleTrace API and asserts 0 bugs are reported.
    ///
    /// WHAT THE BASELINE REPLAY PROVED (frozen witness last-seat-20260927082356.trace):
    ///   Supplying the frozen trace to the baseline binary (tree
    ///   77b0749dfaf971886eead2da8716c934d54d7322, broken implementation) and
    ///   running under the Coyote-rewritten IL reproduced exactly one
    ///   Specification.Assert failure: "2 successful reservations were made
    ///   against a venue with capacity 1."  No seed value is present in the
    ///   frozen artifacts; no seed is claimed for the replay invocation.
    ///
    /// OBSERVED POST-FIX OUTCOME:
    ///   When the same frozen trace is supplied to the repaired binary, Coyote
    ///   1.7.11 prints "Failed to reproduce the bug." and NumOfFoundBugs == 0.
    ///   That is the complete observed evidence.  The cause is not established
    ///   by the output and is not claimed here.  Coyote 1.7.11 TestReport
    ///   exposes no consumed-step count; exact schedule alignment is UNRESOLVED.
    ///   The open milestone item in docs/milestones.md tracks this.
    ///
    /// TEST BEHAVIOUR:
    ///   - Missing or invalid INTERLEAVE_WITNESS_TRACE → fails (no attempt made).
    ///   - Coyote reports 1 or more bugs → fails (invariant violated; fix incomplete).
    ///   - Coyote reports 0 bugs → passes, recording the observed outcome.
    ///     A pass does NOT claim exact schedule alignment.
    ///
    /// Set INTERLEAVE_WITNESS_TRACE to the path of the frozen .trace file.
    /// CI supplies witnesses/frozen/last-seat-20260927082356.trace explicitly.
    /// </summary>
    [Fact]
    public void ReplayAttempt_LastSeat_FrozenTrace_NoBugReported()
    {
        string? tracePath = Environment.GetEnvironmentVariable("INTERLEAVE_WITNESS_TRACE");

        // No trace supplied — no replay attempt was made; fail rather than pass silently.
        Assert.True(
            !string.IsNullOrWhiteSpace(tracePath) && File.Exists(tracePath),
            $"INTERLEAVE_WITNESS_TRACE is not set to a valid file path " +
            $"(value: '{tracePath ?? "(not set)"}').  " +
            $"Set it to witnesses/frozen/last-seat-20260927082356.trace before running this test.");

        _output.WriteLine($"[INTERLEAVE] Supplying frozen witness to repaired binary: {tracePath}");
        _output.WriteLine("[INTERLEAVE] Pass = 0 bugs reported. Exact alignment is UNRESOLVED — see docs/milestones.md.");

        string traceJson = File.ReadAllText(tracePath!);

        var configuration = Configuration.Create()
            .WithReproducibleTrace(traceJson)
            .WithVerbosityEnabled(VerbosityLevel.Info);

        var engine = TestingEngine.Create(configuration, RunConcurrentReservations);
        engine.Run();

        var report = engine.TestReport;

        _output.WriteLine($"[INTERLEAVE] Bugs found: {report.NumOfFoundBugs}");
        _output.WriteLine($"[INTERLEAVE] Explored {report.NumOfExploredFairPaths} fair path(s), " +
                          $"{report.NumOfExploredUnfairPaths} unfair path(s).");
        foreach (var bug in report.BugReports)
        {
            _output.WriteLine($"[INTERLEAVE] Bug report: {bug}");
        }

        // Passing this assertion records the observed outcome: 0 bugs were
        // reported when the frozen trace was supplied to the repaired binary.
        // It does NOT establish that the frozen schedule was followed exactly.
        Assert.Equal(0, report.NumOfFoundBugs);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. Regression tests — systematic, Coyote-controlled
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Zero-capacity venue: every concurrent TryReserve must return null;
    /// the reservation collection must remain empty.
    /// </summary>
    [Fact]
    public void Regression_ZeroCapacity_RejectsAllReservations()
    {
        var configuration = Configuration.Create()
            .WithTestingIterations(200)
            .WithMaxSchedulingSteps(500)
            .WithVerbosityEnabled(VerbosityLevel.Error);

        var engine = TestingEngine.Create(configuration, async () =>
        {
            var service = new SeatReservationService(capacity: 0);

            Task<Reservation?> t1 = Task.Run(() => service.TryReserve("alice"));
            Task<Reservation?> t2 = Task.Run(() => service.TryReserve("bob"));

            await Task.WhenAll(t1, t2);

            Specification.Assert(
                t1.Result is null,
                "alice should have received null on a zero-capacity venue.");

            Specification.Assert(
                t2.Result is null,
                "bob should have received null on a zero-capacity venue.");

            Specification.Assert(
                service.Reservations.Count == 0,
                $"Reservation list should be empty on zero-capacity venue but has {service.Reservations.Count} entry/entries.");
        });

        engine.Run();

        _output.WriteLine($"[INTERLEAVE][ZeroCapacity] Paths: {engine.TestReport.NumOfExploredFairPaths} fair, " +
                          $"{engine.TestReport.NumOfExploredUnfairPaths} unfair. Bugs: {engine.TestReport.NumOfFoundBugs}");

        Assert.True(engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths > 0,
            "Coyote explored 0 paths for the zero-capacity regression.");
        Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
    }

    /// <summary>
    /// One-seat contention: across all explored schedules AvailableSeats must
    /// never go negative — the seat count is bounded below by zero.
    /// </summary>
    [Fact]
    public void Regression_OneSeatContention_AvailableSeatsNeverNegative()
    {
        var configuration = Configuration.Create()
            .WithTestingIterations(500)
            .WithMaxSchedulingSteps(1000)
            .WithVerbosityEnabled(VerbosityLevel.Error);

        var engine = TestingEngine.Create(configuration, async () =>
        {
            var service = new SeatReservationService(capacity: 1);

            Task<Reservation?> t1 = Task.Run(() => service.TryReserve("alice"));
            Task<Reservation?> t2 = Task.Run(() => service.TryReserve("bob"));

            await Task.WhenAll(t1, t2);

            int available = service.AvailableSeats;
            Specification.Assert(
                available >= 0,
                $"AvailableSeats is {available} — seat count must never be negative.");
        });

        engine.Run();

        _output.WriteLine($"[INTERLEAVE][NeverNegative] Paths: {engine.TestReport.NumOfExploredFairPaths} fair, " +
                          $"{engine.TestReport.NumOfExploredUnfairPaths} unfair. Bugs: {engine.TestReport.NumOfFoundBugs}");

        Assert.True(engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths > 0,
            "Coyote explored 0 paths for the one-seat contention regression.");
        Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
    }

    /// <summary>
    /// Capacity and collection consistency: after two concurrent callers finish,
    /// AvailableSeats + Reservations.Count must equal the initial capacity (1).
    /// This verifies the decrement and the list insertion are always in sync.
    /// </summary>
    [Fact]
    public void Regression_AvailableSeats_ConsistentWithReservationCount()
    {
        const int capacity = 1;

        var configuration = Configuration.Create()
            .WithTestingIterations(500)
            .WithMaxSchedulingSteps(1000)
            .WithVerbosityEnabled(VerbosityLevel.Error);

        var engine = TestingEngine.Create(configuration, async () =>
        {
            var service = new SeatReservationService(capacity: capacity);

            Task<Reservation?> t1 = Task.Run(() => service.TryReserve("alice"));
            Task<Reservation?> t2 = Task.Run(() => service.TryReserve("bob"));

            await Task.WhenAll(t1, t2);

            int available     = service.AvailableSeats;
            int reservedCount = service.Reservations.Count;

            Specification.Assert(
                available + reservedCount == capacity,
                $"AvailableSeats ({available}) + Reservations.Count ({reservedCount}) != capacity ({capacity}). " +
                "The seat count and reservation list are out of sync.");
        });

        engine.Run();

        _output.WriteLine($"[INTERLEAVE][Consistency] Paths: {engine.TestReport.NumOfExploredFairPaths} fair, " +
                          $"{engine.TestReport.NumOfExploredUnfairPaths} unfair. Bugs: {engine.TestReport.NumOfFoundBugs}");

        Assert.True(engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths > 0,
            "Coyote explored 0 paths for the state-consistency regression.");
        Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
    }

    /// <summary>
    /// Exercise both public readers while reservation writers are active. Each
    /// availability value and detached reservation snapshot must remain within
    /// the venue's bounds, and the final state must reconcile exactly.
    /// </summary>
    [Fact]
    public void Regression_ConcurrentReads_AreSafeSnapshots()
    {
        const int capacity = 2;

        var configuration = Configuration.Create()
            .WithTestingIterations(500)
            .WithMaxSchedulingSteps(1000)
            .WithVerbosityEnabled(VerbosityLevel.Error);

        var engine = TestingEngine.Create(configuration, async () =>
        {
            var service = new SeatReservationService(capacity: capacity);

            Task<Reservation?> writerAlice = Task.Run(() => service.TryReserve("alice"));
            Task<Reservation?> writerBob = Task.Run(() => service.TryReserve("bob"));
            Task reader = Task.Run(() =>
            {
                for (int i = 0; i < 32; i++)
                {
                    int available = service.AvailableSeats;
                    IReadOnlyList<Reservation> snapshot = service.Reservations;

                    Specification.Assert(
                        available >= 0 && available <= capacity,
                        $"AvailableSeats returned out-of-range value {available}.");

                    Specification.Assert(
                        snapshot.Count <= capacity,
                        $"Reservation snapshot has {snapshot.Count} entries for capacity {capacity}.");

                    foreach (Reservation reservation in snapshot)
                    {
                        Specification.Assert(
                            reservation is not null,
                            "Reservation snapshot contains a null entry.");
                    }
                }
            });

            await Task.WhenAll(writerAlice, writerBob, reader);

            int finalAvailable = service.AvailableSeats;
            int finalReserved = service.Reservations.Count;
            Specification.Assert(
                finalAvailable + finalReserved == capacity,
                $"Final state does not reconcile: available={finalAvailable}, reserved={finalReserved}, capacity={capacity}.");
        });

        engine.Run();

        _output.WriteLine($"[INTERLEAVE][ConcurrentReads] Paths: {engine.TestReport.NumOfExploredFairPaths} fair, " +
                          $"{engine.TestReport.NumOfExploredUnfairPaths} unfair. Bugs: {engine.TestReport.NumOfFoundBugs}");

        Assert.True(engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths > 0,
            "Coyote explored 0 paths for the concurrent-read regression.");
        Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
    }

    /// <summary>
    /// No duplicate reservation IDs: every reservation in the list must have a
    /// distinct ID.  Verifies the deterministic counter produces unique values
    /// even under concurrent calls and that no reservation is recorded twice.
    /// </summary>
    [Fact]
    public void Regression_ReservationList_NoDuplicateIds()
    {
        var configuration = Configuration.Create()
            .WithTestingIterations(500)
            .WithMaxSchedulingSteps(1000)
            .WithVerbosityEnabled(VerbosityLevel.Error);

        var engine = TestingEngine.Create(configuration, async () =>
        {
            // Capacity 2 so both callers must succeed; this exercises the
            // counter under concurrent successful completions.
            const int capacity = 2;
            var service = new SeatReservationService(capacity: capacity);

            Task<Reservation?> t1 = Task.Run(() => service.TryReserve("alice"));
            Task<Reservation?> t2 = Task.Run(() => service.TryReserve("bob"));

            await Task.WhenAll(t1, t2);

            // Both TryReserve calls must return non-null: with capacity 2 and two
            // callers there is always a seat for each.  A null here means the
            // service incorrectly rejected a reservation it should have accepted.
            Specification.Assert(
                t1.Result != null,
                "alice's TryReserve returned null on a capacity-2 venue; expected a reservation.");

            Specification.Assert(
                t2.Result != null,
                "bob's TryReserve returned null on a capacity-2 venue; expected a reservation.");

            // The stored list must contain exactly two entries.  This rules out
            // the vacuous case where distinctness passes on zero or one element.
            IReadOnlyList<Reservation> list = service.Reservations;
            Specification.Assert(
                list.Count == capacity,
                $"Expected {capacity} stored reservations for capacity {capacity}, " +
                $"but found {list.Count}.");

            // All stored IDs must be distinct.
            var ids = list.Select(r => r.ReservationId).ToList();
            bool allUnique = ids.Count == ids.Distinct().Count();
            Specification.Assert(
                allUnique,
                $"Duplicate reservation ID detected in list of {list.Count} reservation(s): [{string.Join(", ", ids)}]");
        });

        engine.Run();

        _output.WriteLine($"[INTERLEAVE][NoDuplicateIds] Paths: {engine.TestReport.NumOfExploredFairPaths} fair, " +
                          $"{engine.TestReport.NumOfExploredUnfairPaths} unfair. Bugs: {engine.TestReport.NumOfFoundBugs}");

        Assert.True(engine.TestReport.NumOfExploredFairPaths + engine.TestReport.NumOfExploredUnfairPaths > 0,
            "Coyote explored 0 paths for the unique-ID regression.");
        Assert.Equal(0, engine.TestReport.NumOfFoundBugs);
    }
}
