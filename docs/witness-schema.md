# Interleaving Witness — Schema (Draft)

This document describes the fields that make up an Interleaving Witness artifact.
Fields are annotated by their source:

- **[ENGINE]** — directly available from Coyote's `TestReport` / `TraceReport` JSON output
- **[INTERLEAVE]** — must be added by the INTERLEAVE tooling (correlation, inference, or user input)
- **[UNVERIFIED]** — conceptually desirable but not yet proven extractable

---

## Fields

### Identity

| Field | Source | Notes |
|---|---|---|
| `witnessId` | [INTERLEAVE] | Stable UUID generated at capture time |
| `capturedAt` | [INTERLEAVE] | UTC timestamp of capture |
| `status` | [INTERLEAVE] | `REPRODUCIBLE` \| `FIXED` \| `UNVERIFIED` |

### Source context

| Field | Source | Notes |
|---|---|---|
| `sourceCommit` | [INTERLEAVE] | `git rev-parse HEAD` at capture time |
| `sourceBranch` | [INTERLEAVE] | `git branch --show-current` |
| `affectedAssembly` | [INTERLEAVE] | Path to the rewritten DLL under test |

### Concurrency contract

| Field | Source | Notes |
|---|---|---|
| `concurrencyContract` | [INTERLEAVE] | Human-readable description of the concurrent operations |
| `invariant` | [INTERLEAVE] | The business invariant as a string |
| `initialState` | [INTERLEAVE] | Description of the state before concurrent operations begin |
| `concurrentOperations` | [INTERLEAVE] | List of operations run concurrently |

### Engine evidence (directly from Coyote)

| Field | Source | Notes |
|---|---|---|
| `engineName` | [ENGINE] | `"Microsoft.Coyote"` |
| `engineVersion` | [ENGINE] | From `TraceReport.CoyoteVersion` |
| `explorationStrategy` | [ENGINE] | From `TraceReport.Settings.Strategy` |
| `strategyBound` | [ENGINE] | From `TraceReport.Settings.StrategyBound` |
| `seed` | [ENGINE] | From `TraceReport.Settings.Seed` (if set) |
| `maxFairSchedulingSteps` | [ENGINE] | From `TraceReport.Settings.MaxFairSchedulingSteps` |
| `bugReport` | [ENGINE] | From `TestReport.BugReports` (the assertion message) |
| `schedulingSteps` | [ENGINE] | From `TraceReport.Steps` (the ordered IL scheduling decisions) |
| `rawTraceJson` | [ENGINE] | Full content of the `.trace` file |
| `readableTrace` | [ENGINE] | Full content of the `.txt` file |

### Replay

| Field | Source | Notes |
|---|---|---|
| `replayCommand` | [INTERLEAVE] | The exact shell command to reproduce the violation |
| `frozenTracePath` | [INTERLEAVE] | Path to the committed `.trace` file |
| `traceFingerprint` | [INTERLEAVE] | SHA-256 of the raw `.trace` file contents |

### Affected source locations

| Field | Source | Notes |
|---|---|---|
| `affectedLocations` | [UNVERIFIED] | Coyote does not directly map scheduling steps to source lines. INTERLEAVE would need to correlate IL offsets in `TraceReport.Steps` with PDB data. Feasible but not in P0. |

---

## What Coyote actually provides (verified against source)

From `TraceReport.cs` and `TestReport.cs` in `microsoft/coyote@main`:

- `TraceReport.TestName` — the test method name
- `TraceReport.CoyoteVersion` — engine version string
- `TraceReport.Settings.Strategy` — e.g. `"prioritization"`
- `TraceReport.Settings.Seed` — random seed if used
- `TraceReport.Settings.MaxFairSchedulingSteps`
- `TraceReport.Steps` — list of strings encoding each scheduling decision as `op(id:seqId),sp(SchedulingPointType),next(id:seqId)` tokens
- `TestReport.NumOfFoundBugs`
- `TestReport.BugReports` — set of assertion failure messages
- `engine.ReproducibleTrace` — the full JSON string for replay
- `engine.ReadableTrace` — human-readable log

The `.trace` file IS the `ReproducibleTrace` JSON. Feeding it to `Configuration.WithReproducibleTrace()` replays the exact schedule.

## What INTERLEAVE must add

- Git commit / branch binding
- Witness UUID and timestamp
- The concurrency contract definition (human-authored or Bob-generated)
- The invariant description
- The replay shell command
- SHA-256 fingerprint
- Status lifecycle (`REPRODUCIBLE` → `FIXED`)
- Source location correlation (future work — requires PDB analysis)
