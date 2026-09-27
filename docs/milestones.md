# INTERLEAVE release milestones

This ledger separates observed evidence from work that is still open. It is not a claim that the project is release-ready.

## M0 — Baseline and defect evidence

- [x] The .NET 8.0.131 solution build succeeds locally: 3 projects, 0 warnings, 0 errors.
- [x] Sequential baseline suite passes: 6 tests.
- [x] Coyote 1.7.11 on .NET 8.0.31 finds the Last Seat invariant violation in the broken implementation:
      2 successful reservations against capacity 1. The frozen `.txt` report records 5 explored
      execution paths (5 fair, 0 unfair, 5 unique), 1 bug found, and 1 uncontrolled invocation
      (`System.Guid.NewGuid`). No seed value is present in the frozen artifacts.
- [x] A raw Coyote trace and companion output were frozen under `witnesses/frozen/` before any repair.
- [x] Read-only frozen files are covered by `witnesses/frozen/SHA256SUMS`; every listed digest verified in this working copy.
- [x] The frozen `last-seat-20260927082356.trace` was replayed against the baseline binary (tree
      77b0749dfaf971886eead2da8716c934d54d7322) in an isolated scratch copy
      (`/private/tmp/interleave-baseline-clean.Pz8oKv`). Coyote reported exactly 1 bug:
      "2 successful reservations were made against a venue with capacity 1." The frozen witness
      is a valid, deterministically reproducible interleaving of the check-then-act gap on the
      broken source.
- [ ] Commit and independently verify the exact pre-repair source revision and frozen evidence.

The frozen `.txt` report records 5 explored execution paths (5 fair, 0 unfair, 5 unique), 1 bug,
and 1 uncontrolled invocation. No seed value appears in any frozen artifact; no seed is claimed here.
Coyote's uncontrolled-invocation report records `System.Guid.NewGuid`; reservation IDs varied
between exploration runs. The frozen evidence proves the recorded schedule violated the invariant;
it does not prove all schedules do.

## M1 — Root-cause repair with IBM Bob

- [x] Open the writable repository copy in IBM Bob and confirm the workspace root.
- [x] Bob repaired the check/decrement/record operation under one lock, synchronized both
      public reads, and made `Reservations` return a detached snapshot. Bob replaced random
      reservation IDs with a lock-protected per-service sequence.
- [x] Added sequential and Coyote regressions for capacity, availability, detached snapshots,
      reservation counts, distinct IDs, and concurrent public reads.
- [x] Build succeeds on .NET 8.0.31: 3 projects, 0 warnings, 0 errors. Sequential suite: 8/8.
      Full Coyote concurrency suite: 7/7 passed locally when run with
      `INTERLEAVE_WITNESS_TRACE=witnesses/frozen/last-seat-20260927082356.trace` explicitly set.
      The CI workflow is configured to supply this path in both jobs, but has not yet been run
      remotely; no hosted CI result is verified.
- [x] Bounded Last Seat exploration (Coyote 1.7.11, portfolio fair, 500 iterations,
      1,000 scheduling steps per iteration): 500 fair paths, 0 unfair paths, 0 bugs.
      This is bounded evidence, not an exhaustive proof. Seed is assigned at runtime
      by Coyote's portfolio strategy and is not recorded here.
- [x] Trace-based replay attempt (`ReplayAttempt_LastSeat_FrozenTrace_NoBugReported`):
      with the frozen trace supplied, Coyote printed "Failed to reproduce the bug." and
      `report.NumOfFoundBugs == 0`. Test passes on 0 bugs. A pass records the observed
      outcome; it does not establish exact schedule alignment.
- [ ] Exact schedule alignment UNRESOLVED. Observed: Coyote printed "Failed to reproduce
      the bug." and `report.NumOfFoundBugs == 0` when the frozen trace was supplied to the
      repaired binary. Coyote 1.7.11 `TestReport` exposes no consumed-step count or
      equivalent diagnostic. The cause of the observed outcome is not established by the
      available output and is not claimed. This item will remain open until schedule
      alignment can be proved (e.g. by a Coyote API exposing consumed-step counts, or by
      re-recording the trace against the repaired binary).

## M2 — Second scenario and benchmark

- [x] Add the Duplicate Job Claim scenario with an invariant over the actual worker return values. The check correctly counts `ClaimResult.Claimed`; the broken stored counter could hide two stale `0 + 1` writes.
- [x] Coyote 1.7.11 found the real baseline violation: 8 fair paths, 0 unfair paths, 1 bug. Both workers returned `Claimed`. The `.txt`, `.trace`, and `.trace.dgml` are frozen under `witnesses/frozen/` and covered by `SHA256SUMS`.
- [ ] Pin the exact broken source revision used for the Duplicate Job Claim capture and replay that trace against that pinned baseline; the current frozen report/trace is genuine, but a separate baseline replay and source commit were not captured.
- [x] Repair check-and-claim with one lock and add sequential invalid-input/idempotency coverage.
- [x] Bounded repaired exploration: 500 fair paths, 0 unfair paths, 0 bugs at 1,000 scheduling steps. The invariant also checks that stored state equals the number of successful returns.
- [x] Supply the unchanged frozen Duplicate Job Claim trace to the repaired replay test; Coyote reported no invariant bug. As with Last Seat, this zero-bug replay attempt does not prove exact schedule alignment.
- [x] After adding M2, run the complete local suites: sequential 13/13 and Coyote concurrency 9/9, with absolute paths supplied for both frozen traces. These are current local totals; M1's 8/8 and 7/7 above are milestone-specific historical totals. The build completed with 0 warnings and 0 errors. Remote CI remains unverified.
- [ ] Exact schedule alignment for either frozen post-fix trace remains unresolved; Coyote 1.7.11 exposes no consumed-step diagnostic in the current output.
- [ ] Benchmark not performed. No latency, throughput, or comparative performance claim is made.

## M3 — Product and security

- [ ] Build the usable, responsive INTERLEAVE experience specified in the release brief.
- [ ] Verify accessibility, error states, and the complete user journey.
- [ ] Review input handling, process execution, file access, secrets, and dependencies; confirm no remote-code-execution path.
- [ ] Deploy a public demo that works without login and verify the deployed flow.

## M4 — Repository and evidence package

- [x] CI workflow is configured to build the solution, run sequential and full Coyote regression suites, require bounded exploration to pass, and verify frozen witness digests before replay. The workflow has not yet been run remotely; no hosted CI result is verified.
- [x] Finish the README, architecture, limitations, contribution, and security documentation.
- [ ] Create a public GitHub repository and pin `v1.0-hackathon` to the verified release SHA.
- [ ] Capture genuine IBM Bob session summaries for the implementation and review milestones.
- [ ] Produce an editable pitch deck and PDF, plus a professionally recorded video of at most 3 minutes with at least 90 seconds of real product demonstration.
- [ ] Verify the live repository, tag, deployment, video, and deck links.

## M5 — Competition compliance and submission

- [x] Verify the readable official event dates/build window and LabLab's general submission requirements.
- [ ] Verify individual/team registration, exact cutoff/eligibility, and event-specific scoring weights.
- [ ] Confirm every competition claim matches observed evidence and the actual product/tool usage.
- [ ] Prepare the LabLab entry in draft form; final submission remains a separate action.

## Current external dependencies

- GitHub CLI authentication is invalid; a public repository cannot be created or pushed until the account is reauthenticated.
- The official event page lists September 25–27, 2026 and a 48-hour build window. LabLab's general guide requires individual registration for every team member, team membership (including solo participants), an online prototype, video, and pitch deck. The readable event page does not expose the exact cutoff, event-specific eligibility, or scoring weights; see `docs/competition-compliance.md`.
- IBM Bob is the intended development partner. No IBM model/runtime integration has been verified, so public materials must describe Bob as the development tool rather than claim IBM powers the product at runtime.
