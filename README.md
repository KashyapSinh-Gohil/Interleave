# INTERLEAVE

**Concurrency defects, captured as evidence and checked against a repair.**

**Source repository:** [KashyapSinh-Gohil/Interleave](https://github.com/KashyapSinh-Gohil/Interleave)

INTERLEAVE is a .NET proof of concept for turning rare thread interleavings into executable invariants, preserving real Coyote failure traces, and checking repaired implementations. The repository currently contains two in-memory examples: Last Seat and Duplicate Job Claim.

> Current scope: a test-focused proof of concept. There is no user-facing web application, hosted demo, Schedule Capsule pipeline, or performance benchmark in this release.

## How it works

1. Define the business invariant in a concurrent test.
2. Use [Microsoft Coyote](https://microsoft.github.io/coyote/) to explore schedules.
3. Preserve a genuine failing report and raw trace under `witnesses/frozen/`.
4. Repair the concurrency boundary and run bounded post-fix exploration.
5. Supply the frozen witness to the repaired implementation and record Coyote's observed result.

Coyote supplies the execution verdict. A zero-bug bounded run covers only the schedules explored; it is not a proof over every possible execution. A replay attempt reporting zero bugs does not, by itself, prove exact schedule alignment.

## Scenarios

### Last Seat

Two callers race to reserve one remaining seat. The invariant is that successful reservations never exceed capacity. The broken baseline's frozen Coyote report records two successful reservations against one seat. The repair protects availability check, decrement, and reservation insertion under one lock.

At the M1 milestone, the sequential suite passed 8/8 and the Coyote concurrency suite passed 7/7 with the frozen Last Seat witness explicitly supplied. Its bounded run explored 500 fair paths and reported zero bugs at a 1,000-step limit. Those are historical M1 suite totals; see the current totals below. Coyote reported “Failed to reproduce the bug” when the old trace was supplied to the repaired binary; exact schedule alignment remains unresolved and is documented as such.

### Duplicate Job Claim

Two workers attempt to claim one job. The invariant is that at most one worker receives `ClaimResult.Claimed`. The intentionally broken baseline let both workers return `Claimed`; its stored counter could remain one because both performed a stale `0 + 1` write. The invariant therefore counts actual claim results, not the defective counter.

The frozen baseline report records 8 fair paths and one invariant violation. The repair serializes check-and-claim under one lock. The repaired Coyote run explored 500 fair paths with zero bugs. The unchanged frozen trace was supplied to the repaired implementation; see [`docs/milestones.md`](docs/milestones.md) for the exact replay outcome and any remaining alignment limitation.

## Current local verification

After adding both scenarios, the full sequential suite passed **13/13** and the full Coyote concurrency suite passed **9/9** locally, with absolute paths to both frozen traces supplied. The solution built with zero warnings and zero errors. These results are local; GitHub Actions has not run remotely. Each repaired Coyote exploration is bounded to 500 fair paths and 1,000 scheduling steps, and neither result proves all executions safe.

## Repository map

```text
src/Interleave.Core/                 In-memory concurrency examples
tests/Interleave.Tests.Sequential/   Deterministic behavior and input checks
tests/Interleave.Tests.Concurrency/  Coyote schedule exploration and replay
witnesses/frozen/                    Immutable raw reports, traces, and checksums
docs/                                Architecture, limits, replay, milestones, and compliance
submission/                          Draft entry copy, cover art, and pitch deck exports
.github/workflows/                   Build and test workflow
```

The current six-slide deck is available as an [editable PowerPoint](submission/media/Interleave_Pitch_Deck_M2.pptx) and [PDF](submission/media/Interleave_Pitch_Deck_M2.pdf). The [narrated overview video](submission/media/Interleave_Narrated_Overview.mp4), [MP3 narration](submission/media/Interleave_Narration.mp3), and [transcript](submission/media/Interleave_Narration.txt) are also included. These are a project overview and evidence summary, not a live product demo. The [cover background](submission/assets/interleave-cover.png) is original artwork; it does not contain the official IBM Bob mascot mark.

## Run locally

Prerequisites: .NET 8 SDK and Microsoft Coyote CLI 1.7.11 (`dotnet tool install --global Microsoft.Coyote.CLI --version 1.7.11`).

```sh
dotnet restore
dotnet build --configuration Release
dotnet test tests/Interleave.Tests.Sequential/

# Rewrite the assemblies after building and before Coyote concurrency runs.
coyote rewrite coyote.rewrite.json

export INTERLEAVE_WITNESS_TRACE="$PWD/witnesses/frozen/last-seat-20260927082356.trace"
export INTERLEAVE_JOB_CLAIM_TRACE="$PWD/witnesses/frozen/duplicate-job-claim-20260927140504.trace"
dotnet test tests/Interleave.Tests.Concurrency/
```

The concurrency suite needs the frozen trace paths above for the two replay facts. `witnesses/frozen/SHA256SUMS` records hashes for the checked-in evidence. Verify with `sha256sum -c witnesses/frozen/SHA256SUMS` (or `shasum -a 256 -c witnesses/frozen/SHA256SUMS` on macOS).

## Authority and development tools

| Component | Role |
| --- | --- |
| Microsoft Coyote | Controls systematic schedules and reports invariant violations. |
| IBM Bob 2.0 | Development partner used during implementation work. |
| INTERLEAVE | Example code, invariant tests, trace preservation, and documentation. |

No IBM Watson model or IBM-hosted inference service powers runtime behavior. No runtime AI verdict is claimed.

## License

MIT. See [`LICENSE`](LICENSE).
