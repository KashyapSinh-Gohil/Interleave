# Replaying a frozen Coyote witness

## What is preserved

A Coyote evidence run can emit a human-readable `.txt` report, a machine-readable
`.trace` used as replay input, and a `.trace.dgml` graph. The checked-in files in
`witnesses/frozen/` are immutable evidence covered by `SHA256SUMS`. Their
hashes prove file integrity; they do not by themselves prove which source
revision produced them.

## Run both repaired trace attempts

Prerequisites: .NET 8 and Microsoft Coyote CLI 1.7.11. Use absolute paths
because the test host's working directory can differ from the repository root.

```sh
dotnet restore
dotnet build --configuration Debug
coyote rewrite coyote.rewrite.json

export INTERLEAVE_WITNESS_TRACE="$PWD/witnesses/frozen/last-seat-20260927082356.trace"
dotnet test tests/Interleave.Tests.Concurrency/ \
  --no-build --configuration Debug \
  --filter FullyQualifiedName~ReplayAttempt_LastSeat_FrozenTrace_NoBugReported

export INTERLEAVE_JOB_CLAIM_TRACE="$PWD/witnesses/frozen/duplicate-job-claim-20260927140504.trace"
dotnet test tests/Interleave.Tests.Concurrency/ \
  --no-build --configuration Debug \
  --filter FullyQualifiedName~ReplayAttempt_DuplicateJobClaim_FrozenTrace_NoBugReported
```

The suite's replay facts fail if their environment variable is missing or the
file does not exist. Coyote rewrites build outputs, so re-run `dotnet build`
before `coyote rewrite` after source changes.

## Observed evidence

| Scenario and state | Recorded result | What it establishes |
|---|---|---|
| Last Seat broken baseline | The frozen report records 5 fair paths and 1 invariant violation: 2 successful reservations against capacity 1. A separate baseline replay was also observed to reproduce the failure once. | This schedule violated the capacity invariant on the recorded broken build. |
| Last Seat repaired bounded run | 500 fair paths, 0 unfair paths, 0 reported bugs. | No violation appeared in those explored schedules. |
| Last Seat repaired trace attempt | Coyote printed “Failed to reproduce the bug”; the replay fact observed 0 bugs. | Zero reported bugs for that attempt. Exact schedule alignment remains unresolved. |
| Duplicate Job Claim broken baseline | The frozen report records 8 fair paths and 1 invariant violation. Both workers returned `Claimed`. | The captured schedule violated the at-most-one-successful-claim invariant. No separate replay against a pinned baseline source revision is claimed. |
| Duplicate Job Claim repaired bounded run | 500 fair paths, 0 unfair paths, 0 reported bugs. | No violation appeared in those explored schedules. |
| Duplicate Job Claim repaired trace attempt | The frozen `.trace` was supplied to Coyote and the test reported 0 bugs. The run output included “Failed to reproduce the bug.” | Zero reported bugs for that attempt. Exact schedule alignment remains unresolved. |

Coyote 1.7.11's captured report exposes no consumed-step count or equivalent
alignment diagnostic. The reason for “Failed to reproduce the bug” is not
established and is not inferred.

## What replay does not prove

- A zero-bug bounded run is not exhaustive verification of every possible
  thread schedule.
- A repaired replay fact passing on zero bugs does not prove Coyote followed
  the exact original schedule.
- A hash manifest detects changed evidence files; it does not authenticate the
  original runner or pin the source revision by itself.
- The project has not implemented a normalized Schedule Capsule pipeline.
