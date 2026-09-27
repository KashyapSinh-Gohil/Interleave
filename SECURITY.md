# Security policy

## Scope

The current project is a local .NET proof of concept. It has no hosted API,
authentication flow, upload endpoint, or feature that executes user-supplied
code. The sample uses synthetic in-memory data only.

## Reporting a concern

Please use GitHub's private vulnerability reporting for this repository if it
is enabled. If it is unavailable, contact the maintainers through their GitHub
profile before sharing details. Do not post credentials, personal information,
customer data, or exploit details in a public issue.

Before adding source-analysis or execution capabilities, define input limits,
file-access boundaries, process permissions, timeouts, output redaction, and
abuse tests. Do not expose arbitrary-code execution as a public service.
