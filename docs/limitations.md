# Limitations

INTERLEAVE is a bounded engineering proof of concept, not a general concurrency
verifier or a production product.

- **Two synthetic in-memory scenarios:** Last Seat reservation and Duplicate
  Job Claim. They do not model persistence, multiple processes, distributed
  locks, network failures, retries, or production workloads.
- **Bounded exploration:** each captured repaired Coyote run explored 500 fair
  paths with a 1,000-step limit and found zero bugs. This is evidence about
  those explored schedules, not proof that all executions are safe.
- **Replay alignment:** Coyote was given each frozen pre-fix witness against the
  corresponding repaired code and reported no invariant bug. Its output also
  said “Failed to reproduce the bug.” No consumed-step diagnostic was available,
  so exact schedule alignment has not been established.
- **No normalized capsule:** raw Coyote reports, traces, and DGML are preserved,
  but a Schedule Capsule format and product pipeline are not implemented.
- **No benchmark:** performance, throughput, latency, and comparisons with
  other concurrency-testing tools have not been measured.
- **No app or deployment:** this release contains source, tests, CI
  configuration, documentation, and evidence. It has no public interactive
  demo or hosted application URL.
- **No runtime AI:** IBM Bob 2.0 was used during development. The code does not
  call Bob, watsonx, or another model at runtime.

Do not describe the project as proving a program is race-free or guaranteeing
correct concurrent software.
