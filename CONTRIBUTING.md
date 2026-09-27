# Contributing

## Requirements

- .NET 8 SDK
- Microsoft Coyote CLI 1.7.11 for the systematic concurrency suite

Run the following from the repository root:

```sh
dotnet restore
dotnet build --configuration Debug
dotnet test tests/Interleave.Tests.Sequential/ --no-build --configuration Debug
coyote rewrite coyote.rewrite.json

export INTERLEAVE_WITNESS_TRACE="$PWD/witnesses/frozen/last-seat-20260927082356.trace"
export INTERLEAVE_JOB_CLAIM_TRACE="$PWD/witnesses/frozen/duplicate-job-claim-20260927140504.trace"
dotnet test tests/Interleave.Tests.Concurrency/ --no-build --configuration Debug
```

## Evidence changes

Do not edit files under `witnesses/frozen/` to make a test pass. When adding a
scenario, capture a genuine failing invariant before the repair, preserve the
raw Coyote report and trace, update `SHA256SUMS`, and document the engine
version and exploration bounds. A replay test must fail if no trace was
supplied. Distinguish a zero-bug trace attempt from proof of exact schedule
alignment.

Keep changes focused and update README, architecture, limitations, milestones,
and the release deck whenever verified project scope or evidence changes. Do
not report bounded exploration as exhaustive proof.
