# LabLab submission copy — draft only

This text is written to the visible Step 1 field limits. It is not a claim that
the end-to-end submission is complete, and it has not been entered into or saved
on LabLab.

## Submission title — 36 / 50 characters (allowed: 5–50)

INTERLEAVE: Replayable Race Evidence

## Short description — 149 / 255 characters (allowed: 50–255)

INTERLEAVE explores concurrent schedules, captures real Coyote failure traces, and checks bounded repairs across two .NET proof-of-concept scenarios.

## Long description — 293 words (maximum: 500)

Concurrency failures can depend on one rare ordering of otherwise ordinary operations. A service may pass sequential tests yet reserve the same resource twice when two callers reach a check-and-update transition together. Reproducing that timing-dependent failure can be difficult.

INTERLEAVE is a .NET 8 proof of concept for testing execution order as well as input. Microsoft Coyote systematically controls concurrent task schedules and checks explicit business invariants. When a baseline violates an invariant, the project preserves the raw Coyote report and trace with a SHA-256 manifest, repairs the synchronization boundary, and reruns bounded exploration.

The Last Seat scenario begins with one seat and two concurrent reservation requests. Coyote found the baseline invariant violation: two successful reservations against capacity one. The repair protects the capacity check, decrement, and reservation record under one lock.

The Duplicate Job Claim scenario starts two workers claiming the same job. Coyote found a baseline schedule where both workers returned `Claimed`. The invariant counts those actual return values because the broken stored counter could hide the duplicate claim. The repair makes the check-and-claim operation atomic under one lock.

The current local verification build succeeded with zero warnings and errors. The sequential suite passed 13/13; the Coyote concurrency suite passed 9/9 with both frozen traces supplied. Repaired bounded explorations each covered 500 fair paths and reported zero bugs. These are bounded results, not exhaustive proofs. Coyote reported no bug during the repaired trace attempts, but exact schedule alignment remains unresolved.

IBM Bob 2.0 was used as a development partner during project implementation. Microsoft Coyote provides the test verdict; no IBM model or service powers runtime behavior. This release is a test-focused prototype, not a hosted web app. It does not yet provide a Schedule Capsule pipeline, performance benchmark, or online demo.

## IBM Bob usage statement — 1,019 / 4,000 characters (observed 500–4,000)

IBM Bob 2.0 was used as a development tool while investigating the Last Seat race, shaping the initial test approach, and preparing the Duplicate Job Claim baseline scenario. The frozen Duplicate Job Claim report and trace preserve the baseline defect found during that work. IBM Bob contributed to the Last Seat implementation and its regression coverage. The Bob usage limit was reached before the later Duplicate Job Claim repair; that repair and final local verification were completed afterward. Microsoft Coyote controls the systematic schedules and supplies the test verdict. Both repaired explorations covered 500 fair paths at a 1,000-step bound and reported zero bugs, but these bounded results do not prove all executions safe. Exact alignment of the supplied post-fix traces remains unresolved. Bob is a development tool only: no IBM model or service powers runtime behavior. INTERLEAVE is a .NET 8 test-focused proof of concept, not a hosted application; no public demo or performance benchmark is claimed.

## Category and technology tags — draft suggestions

- Category: Developer Tools
- Technologies: IBM, Microsoft Coyote, .NET / C# (choose only exact options offered by the form)

## Remaining submission items

- Public code repository and verified release tag
- Required `bob_sessions/` task-summary screenshots
- Hosted application URL and professionally recorded MP4 demo
- Final pitch deck/PDF reflecting any verified release changes
- Registration, roster, and event cutoff confirmation
