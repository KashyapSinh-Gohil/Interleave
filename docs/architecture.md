# Architecture

## Current scope

INTERLEAVE is a .NET 8 proof of concept containing two in-memory services and a
Microsoft Coyote test harness. It has no user-facing app, network API,
persistence, distributed coordination, or runtime AI integration.

## Scenarios

```text
SeatReservationService ── LastSeatConcurrencyTests
  lock: check capacity, decrement, record reservation
  invariant: successful reservations <= initial capacity

JobClaimService ───────── DuplicateJobClaimTests
  lock: check-and-add claimed job ID
  invariant: at most one worker returns Claimed

                  Coyote systematic scheduler
                            │
               failing baseline / bounded repair
                            │
        raw reports + traces + SHA256SUMS in witnesses/frozen/
```

Both scenarios use real `Task.Run` calls and `Task.WhenAll`. Each service owns
its lock and its state. The locks do not coordinate distinct service instances,
processes, or machines. The reservation service returns a detached snapshot of
its reservations and uses a per-instance deterministic ID sequence. The job
claim service rejects null, empty, and whitespace-only IDs and records a job at
most once per instance.

## Coyote instrumentation and replay

`coyote.rewrite.json` identifies the test and core assemblies for Coyote
instrumentation. Re-run `coyote rewrite coyote.rewrite.json` after every build
before concurrency tests. The two replay facts read absolute witness paths from
`INTERLEAVE_WITNESS_TRACE` and `INTERLEAVE_JOB_CLAIM_TRACE` respectively. A
missing path fails the fact rather than silently skipping replay.

The CI workflow verifies the frozen hash manifest, builds both test projects,
runs sequential tests, instruments the assemblies, and runs the systematic and
replay facts. CI has not yet been run remotely; no hosted result is claimed.

## Evidence and authority

The baseline Coyote reports, raw `.trace` files, companion `.dgml` files, and
`SHA256SUMS` are preserved under `witnesses/frozen/`. Both baseline defects were
reported by an actual `Specification.Assert` invariant. The repaired code passed
bounded Coyote exploration (500 fair paths per scenario, zero reported bugs in
the captured runs). These results are bounded evidence, not exhaustive proofs.

When either frozen baseline trace is supplied to its repaired implementation,
Coyote reports no invariant bug. This is a trace-based replay attempt; exact
schedule alignment remains unresolved because the available Coyote 1.7.11 report
does not expose a consumed-step count.

Microsoft Coyote determines whether the executable invariant failed. IBM Bob
2.0 was used as a development partner; it is not called by the runtime. The
project does not yet normalize raw traces into a Schedule Capsule or provide an
online experience.
