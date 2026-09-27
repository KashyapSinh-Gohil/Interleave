# IBM Bob 2.0 Hackathon: compliance and submission status

**Status: incomplete; do not treat this ledger as eligibility certification.**
This page separates facts visible on the official event/guide pages, field
limits observed in the LabLab form, and additional requirements in the project
owner's release brief.

## Official event and tool requirements checked

The [event page](https://lablab.ai/ai-hackathons/ibm-bob-2-hackathon) lists an
online, 48-hour event for September 25–27, 2026, and says participants must
register before the kickoff stream to start building with Bob. The current
page does not show an exact submission cutoff or timezone.

The [IBM Bob 2.0 hackathon guide](https://lablab-ibm-bob-2-hackathon-guide.s3.us.cloud-object-storage.appdomain.cloud/index.html)
requires IBM Bob IDE as a core part of the working prototype workflow, calls
for relevant Bob task-session summary screenshots in a `bob_sessions/` folder
in the code repository, and says watsonx is optional. It also requires
participants to follow data-use rules: do not use client, personal, social
media, confidential, or otherwise unpermitted data. INTERLEAVE uses synthetic
concurrency examples.

The [LabLab general guide](https://lablab.ai/guide) describes individual
registration for each team member and team membership (including solo
participants). These user/account checks have not been completed here. The
event-specific judge score weights were not available on the readable official
event page or guide.

## Submission form requirements observed

The LabLab form is a three-step workflow under the `interleave` team route. The
Step 1 screenshot shows these exact limits:

| Step 1 field | Visible limit |
|---|---|
| Submission title | 5–50 characters |
| Short description | 50–255 characters |
| Long description | 500 words maximum |
| IBM Bob Usage Statement | 500–4,000 characters (observed form counter) |

The Step 2 checklist previously observed in the form requires a video and PDF
slide presentation; a cover image is optional. The project release brief adds
the target of an MP4 no longer than 3 minutes with at least 90 seconds of real
working-product demonstration. The six-slide PDF and editable PPTX exist locally.
A narrated overview video has been rendered from the deck, but it is not a live
product demonstration. The project has no deployed application, so the real-demo
requirement remains unmet. The new cover artwork is local; the authentic IBM Bob
mascot mark is not included because the verified original artwork was not
available when this cover was prepared.

The exact field labels and required-star markers on Step 3 have not been
verified from the live form. The event submission checklist calls for a public
code repository, Bob session-summary screenshots, a demo platform, and an
application URL. Treat that as the expected Step 3 deliverable set; do not infer
unknown extra fields. Nothing has been saved or submitted on LabLab.

## Evidence ledger

| Requirement | Current evidence | Status |
|---|---|---|
| Build during event with IBM Bob IDE | Bob IDE was used for M1 project analysis and implementation; no more Bob work is planned after its usage limit was reached. | Partial |
| Use the hackathon-provisioned Bob account | IDE showed a us-east instance; the required account identifier `ibm-coding-challenge-uat` was not confirmed. | Open |
| Bob task-session summary screenshots in repository | No genuine session summary screenshots have been captured; `bob_sessions/` is not present. | Open |
| Individual registration, team roster, exact cutoff, and eligibility | Not checked or confirmed. | Open |
| Accurate IBM/runtime attribution | Bob 2.0 was a development tool; Microsoft Coyote supplies test verdicts. No IBM model or runtime service is integrated. | Checked |
| Project code and local verification | Two repaired in-memory scenarios; local build succeeded (0 warnings, 0 errors), sequential tests 13/13 and Coyote tests 9/9 passed with absolute paths to both frozen traces. | Verified locally |
| Public GitHub source repository | `https://github.com/KashyapSinh-Gohil/Interleave`; source commit `1fd5481` pushed to `main`. | Source published |
| Online working demo and application URL | No user-facing application or deployment exists. | Open |
| PDF deck and editable PPTX | Six-slide M2 evidence deck exported and reviewed locally; it summarizes both scenarios and bounded results. | Prepared locally |
| MP4 presentation/real product demo | 164-second narrated overview MP4 and matching MP3 are ready. They are not a live product demo; no app exists to demonstrate. | Demo requirement open |
| Cover image | New project cover background exists locally at `submission/assets/interleave-cover.png`; authentic Bob mascot mark is not included. | Prepared locally |
| Data use | Synthetic in-memory tests; no external participant data. | Checked |
| Event-specific score weights | No readable event-specific rubric found. | Unverified |

## Submission text draft

`submission/copy/draft.md` contains updated Step 1 copy with title, short
description, long-description and Bob-usage text, plus draft category and
technology tags. Exact tag picker options have not been verified. It has not
been entered into LabLab.
The entry must remain a draft until the public repository, screenshot folder,
working demo URL, final deck/PDF, video, and registration checks are complete.

The project must not be described as an IBM-powered runtime or as a general
race-freedom guarantee. Coyote's 500-path checks are bounded results; exact
post-fix trace schedule alignment is unresolved.
