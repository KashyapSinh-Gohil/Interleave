import fs from "node:fs/promises";
import path from "node:path";
import { pathToFileURL } from "node:url";

const workspaceDir = process.cwd();
const buildDir = path.join(workspaceDir, ".presentation-build");
const outputDir = path.join(workspaceDir, "submission", "media");
const skillDir = process.env.SKILL_DIR;
const runtimeModules = process.env.RUNTIME_NODE_MODULES;
if (!path.isAbsolute(skillDir ?? "") || !path.isAbsolute(runtimeModules ?? "")) {
  throw new Error("SKILL_DIR and RUNTIME_NODE_MODULES must be absolute paths.");
}
await fs.mkdir(buildDir, { recursive: true });
await fs.mkdir(outputDir, { recursive: true });
try {
  await fs.symlink(runtimeModules, path.join(buildDir, "node_modules"), "dir");
} catch (error) {
  if (error.code !== "EEXIST") throw error;
}

const { resolvePresentationFont, finalizePresentation } = await import(
  pathToFileURL(path.join(skillDir, "container_tools", "artifact_tool_utils.mjs")).href,
);
const { Presentation, PresentationFile } = await import(
  pathToFileURL(path.join(runtimeModules, "@oai", "artifact-tool", "dist", "artifact_tool.mjs")).href,
);
const { Canvas, loadImage } = await import(
  pathToFileURL(path.join(runtimeModules, "@oai", "artifact-tool", "node_modules", "skia-canvas", "lib", "index.mjs")).href,
);

const font = resolvePresentationFont();
const W = 1280;
const H = 720;
const C = {
  ink: "#0B1822",
  paper: "#F5F7F5",
  white: "#FFFFFF",
  teal: "#43D9BE",
  tealDark: "#087E72",
  amber: "#F2B35B",
  red: "#CF574B",
  text: "#10212B",
  muted: "#586B74",
  rule: "#CBD5D7",
  paleRed: "#F5E4DE",
};
const pres = Presentation.create({ slideSize: { width: W, height: H } });

function rect(slide, name, x, y, w, h, fill, radius = 0) {
  return slide.shapes.add({
    geometry: radius ? "roundRect" : "rect",
    name,
    position: { left: x, top: y, width: w, height: h },
    fill,
    line: { fill: "none", width: 0 },
    ...(radius ? { borderRadius: radius } : {}),
  });
}

function text(slide, name, value, x, y, w, h, opts = {}) {
  const shape = slide.shapes.add({
    geometry: "textbox",
    name,
    position: { left: x, top: y, width: w, height: h },
    fill: "none",
    line: { fill: "none", width: 0 },
  });
  shape.text = value;
  shape.text.style = {
    typeface: opts.typeface ?? font,
    fontSize: opts.size ?? 24,
    bold: opts.bold ?? false,
    color: opts.color ?? C.text,
    autoFit: "shrinkText",
    verticalAlignment: opts.vertical ?? "middle",
  };
  return shape;
}

function line(slide, x, y, width, color = C.rule, height = 2) {
  return rect(slide, "divider", x, y, width, height, color);
}

function note(slide, value) {
  slide.speakerNotes.textFrame.setText(value);
}

function baseSlide(title, index, dark = false) {
  const slide = pres.slides.add();
  slide.background.fill = dark ? C.ink : C.paper;
  text(slide, "section-label", "INTERLEAVE  /  M2 EVIDENCE SNAPSHOT", 64, 28, 800, 28, {
    size: 17,
    bold: true,
    color: dark ? C.teal : C.tealDark,
  });
  text(slide, "slide-title", title, 64, 70, 1152, 76, {
    size: 46,
    bold: true,
    color: dark ? C.white : C.text,
  });
  line(slide, 64, 158, 1152, dark ? "#2C414B" : C.rule);
  text(slide, "page-number", String(index).padStart(2, "0"), 1172, 666, 44, 24, {
    size: 16,
    bold: true,
    color: dark ? "#A9BDC2" : C.muted,
  });
  return slide;
}

// 1. Cover
{
  const slide = pres.slides.add();
  slide.background.fill = C.ink;
  const coverBytes = new Uint8Array(await fs.readFile(path.join(workspaceDir, "submission/assets/interleave-cover.png")));
  slide.images.add({
    blob: coverBytes,
    contentType: "image/png",
    alt: "Abstract blue execution paths split at an amber race point, with a clear dark field for the title.",
    fit: "cover",
    position: { left: 0, top: 0, width: W, height: H },
  });
  text(slide, "event-kicker", "IBM BOB 2.0 HACKATHON  /  PROJECT SNAPSHOT", 74, 76, 760, 32, {
    size: 19,
    bold: true,
    color: C.teal,
  });
  text(slide, "project-title", "INTERLEAVE", 74, 194, 740, 94, {
    size: 72,
    bold: true,
    color: C.white,
  });
  text(slide, "tagline", "Two concurrency defects,\ntraced and checked after repair.", 76, 300, 760, 144, {
    size: 37,
    bold: true,
    color: C.white,
  });
  line(slide, 78, 478, 96, C.amber, 5);
  text(slide, "summary", "A Coyote-backed .NET 8 proof of concept", 76, 505, 820, 48, {
    size: 26,
    color: "#E1EBED",
  });
  text(slide, "snapshot-date", "LOCAL PROTOTYPE STATUS   27 SEPTEMBER 2026", 76, 624, 780, 30, {
    size: 18,
    color: "#D0DEE1",
  });
  note(slide,
    "This is a local evidence snapshot, not a claim that the full competition submission is complete. The cover uses original abstract illustration, not a product screenshot. Project state is documented in README.md, docs/milestones.md, and docs/competition-compliance.md."
  );
}

// 2. Two baseline failures
{
  const slide = baseSlide("Both baselines violated a business invariant", 2);
  text(slide, "subtitle", "Coyote found schedules where concurrent requests duplicated a scarce resource.", 64, 176, 1120, 52, {
    size: 26,
    color: C.muted,
  });
  line(slide, 638, 254, 2, C.rule, 310);

  text(slide, "last-seat-label", "LAST SEAT", 84, 250, 490, 36, {
    size: 20,
    bold: true,
    color: C.tealDark,
  });
  text(slide, "last-seat-number", "2", 84, 298, 160, 104, {
    size: 84,
    bold: true,
    color: C.red,
  });
  text(slide, "last-seat-result", "successful reservations\nfor one available seat", 235, 312, 350, 96, {
    size: 29,
    bold: true,
    color: C.text,
  });
  text(slide, "last-seat-baseline", "Baseline report: 5 paths explored, 1 invariant violation.", 84, 432, 500, 56, {
    size: 21,
    color: C.muted,
  });
  rect(slide, "last-seat-violation", 84, 508, 500, 52, C.paleRed, 6);
  text(slide, "last-seat-invariant", "Invariant: reservations cannot exceed capacity.", 100, 513, 472, 42, {
    size: 19,
    bold: true,
    color: "#8F332B",
  });

  text(slide, "job-claim-label", "DUPLICATE JOB CLAIM", 690, 250, 510, 36, {
    size: 20,
    bold: true,
    color: C.tealDark,
  });
  text(slide, "job-claim-number", "2", 690, 298, 160, 104, {
    size: 84,
    bold: true,
    color: C.red,
  });
  text(slide, "job-claim-result", "workers returned\nClaimed for one job", 842, 312, 360, 96, {
    size: 29,
    bold: true,
    color: C.text,
  });
  text(slide, "job-claim-baseline", "Baseline report: 8 fair paths explored, 1 invariant violation.", 690, 432, 515, 56, {
    size: 21,
    color: C.muted,
  });
  rect(slide, "job-claim-violation", 690, 508, 515, 52, C.paleRed, 6);
  text(slide, "job-claim-invariant", "Invariant: at most one worker may claim the job.", 706, 513, 483, 42, {
    size: 19,
    bold: true,
    color: "#8F332B",
  });
  note(slide,
    "Observed baseline facts from docs/milestones.md and witnesses/frozen/. The Last Seat report records 5 execution paths and one invariant bug: two successful reservations against capacity one. The Duplicate Job Claim report records 8 fair paths and one bug: both workers returned ClaimResult.Claimed. The counts are separate experiments, not aggregate totals."
  );
}

// 3. Method
{
  const slide = baseSlide("A frozen failure makes the repair testable", 3, true);
  text(slide, "subtitle", "Coyote supplies systematic schedules and the invariant verdict for each example.", 64, 178, 1135, 52, {
    size: 26,
    color: "#C2D2D5",
  });
  const steps = [
    { x: 72, number: "01", title: "Define", body: "Write the business\ninvariant as a test.", color: C.teal },
    { x: 366, number: "02", title: "Explore", body: "Coyote controls\nconcurrent schedules.", color: C.amber },
    { x: 660, number: "03", title: "Preserve", body: "Keep the failing report,\nraw trace, and checksum.", color: C.teal },
    { x: 954, number: "04", title: "Recheck", body: "Repair the state change,\nthen run bounded checks.", color: C.amber },
  ];
  for (const step of steps) {
    text(slide, `method-number-${step.number}`, step.number, step.x, 280, 80, 40, {
      size: 22,
      bold: true,
      color: step.color,
    });
    text(slide, `method-title-${step.number}`, step.title, step.x, 330, 244, 52, {
      size: 34,
      bold: true,
      color: C.white,
    });
    line(slide, step.x, 393, 210, step.color, 4);
    text(slide, `method-body-${step.number}`, step.body, step.x, 418, 244, 92, {
      size: 23,
      color: "#D8E4E6",
    });
  }
  line(slide, 72, 548, 1136, "#2C414B");
  text(slide, "authority-boundary", "IBM Bob assisted development. Microsoft Coyote controls exploration and reports test results.", 78, 566, 1086, 55, {
    size: 21,
    color: "#CFDDE0",
  });
  note(slide,
    "Workflow summary from README.md and docs/milestones.md. IBM Bob 2.0 was a development tool. Microsoft Coyote 1.7.11 controlled systematic schedule exploration and reported invariant outcomes. The repository does not integrate Bob, watsonx, or another AI service at runtime."
  );
}

// 4. Current verification
{
  const slide = baseSlide("Both repairs passed bounded local exploration", 4);
  text(slide, "subtitle", "Coyote 1.7.11 used the portfolio fair strategy and a 1,000-step scheduling limit.", 64, 176, 1120, 52, {
    size: 25,
    color: C.muted,
  });
  line(slide, 638, 244, 2, C.rule, 282);

  text(slide, "seat-title", "LAST SEAT", 88, 250, 480, 36, {
    size: 20,
    bold: true,
    color: C.tealDark,
  });
  text(slide, "seat-paths", "500", 88, 296, 250, 90, {
    size: 74,
    bold: true,
    color: C.tealDark,
  });
  text(slide, "seat-path-label", "fair paths", 91, 382, 240, 35, {
    size: 22,
    color: C.muted,
  });
  text(slide, "seat-bugs", "0", 380, 296, 150, 90, {
    size: 74,
    bold: true,
    color: C.tealDark,
  });
  text(slide, "seat-bugs-label", "bugs found", 383, 382, 180, 35, {
    size: 22,
    color: C.muted,
  });
  text(slide, "seat-bound", "Repaired bounded run", 88, 439, 480, 42, {
    size: 20,
    bold: true,
    color: C.text,
  });

  text(slide, "claim-title", "DUPLICATE JOB CLAIM", 690, 250, 510, 36, {
    size: 20,
    bold: true,
    color: C.tealDark,
  });
  text(slide, "claim-paths", "500", 690, 296, 250, 90, {
    size: 74,
    bold: true,
    color: C.tealDark,
  });
  text(slide, "claim-path-label", "fair paths", 693, 382, 240, 35, {
    size: 22,
    color: C.muted,
  });
  text(slide, "claim-bugs", "0", 982, 296, 150, 90, {
    size: 74,
    bold: true,
    color: C.tealDark,
  });
  text(slide, "claim-bugs-label", "bugs found", 985, 382, 180, 35, {
    size: 22,
    color: C.muted,
  });
  text(slide, "claim-bound", "Repaired bounded run", 690, 439, 500, 42, {
    size: 20,
    bold: true,
    color: C.text,
  });

  line(slide, 64, 534, 1152, C.rule);
  text(slide, "suite-totals", "Local suites passed: 13 / 13 sequential tests and 9 / 9 Coyote tests.", 78, 548, 1120, 48, {
    size: 22,
    bold: true,
    color: C.text,
  });
  text(slide, "verification-limit", "Finite schedule coverage only. These runs do not prove every execution is safe.", 78, 604, 1090, 36, {
    size: 19,
    color: C.muted,
  });
  note(slide,
    "Current M2 facts in docs/milestones.md and docs/competition-compliance.md: each repaired Coyote scenario explored 500 fair paths, 0 unfair paths, and found 0 bugs within the 1,000-step scheduling limit. The complete local sequential suite passed 13/13 and the Coyote concurrency suite passed 9/9 with absolute paths to both frozen traces supplied. The solution built with zero warnings and errors. GitHub Actions has not yet run remotely. A zero-bug bounded run is evidence about explored schedules, not an exhaustive proof."
  );
}

// 5. Implementation details
{
  const slide = baseSlide("Each lock protects the complete state change", 5);
  text(slide, "subtitle", "The invariant checks the actual business outcome, not a counter that can hide a race.", 64, 176, 1120, 52, {
    size: 25,
    color: C.muted,
  });
  line(slide, 638, 254, 2, C.rule, 300);

  text(slide, "seat-repair-label", "LAST SEAT", 84, 254, 500, 36, {
    size: 20,
    bold: true,
    color: C.tealDark,
  });
  text(slide, "seat-repair-heading", "One lock spans the reservation path.", 84, 304, 500, 72, {
    size: 31,
    bold: true,
    color: C.text,
  });
  text(slide, "seat-repair-details", "Availability check\nSeat decrement\nReservation record", 84, 390, 500, 132, {
    size: 25,
    color: C.text,
  });
  text(slide, "seat-read-details", "Reads synchronize; callers receive detached reservation snapshots.", 84, 532, 510, 48, {
    size: 18,
    color: C.muted,
  });

  text(slide, "claim-repair-label", "DUPLICATE JOB CLAIM", 690, 254, 510, 36, {
    size: 20,
    bold: true,
    color: C.tealDark,
  });
  text(slide, "claim-repair-heading", "One lock spans check and claim.", 690, 304, 510, 72, {
    size: 31,
    bold: true,
    color: C.text,
  });
  text(slide, "claim-repair-details", "Check job status\nMark the job claimed\nReturn the worker outcome", 690, 390, 510, 132, {
    size: 25,
    color: C.text,
  });
  rect(slide, "oracle-callout", 690, 530, 510, 58, "#E5F0EE", 6);
  text(slide, "oracle-text", "Test counts actual ClaimResult.Claimed returns.", 708, 538, 474, 42, {
    size: 19,
    bold: true,
    color: C.tealDark,
  });
  note(slide,
    "Implementation facts from src/Interleave.Core/SeatReservationService.cs, src/Interleave.Core/JobClaimService.cs, and docs/architecture.md. The Last Seat lock covers availability check, decrement, and reservation insertion. Public reads synchronize with the state; the reservation list is a detached snapshot. Duplicate Job Claim serializes check-and-claim. Its invariant counts actual worker return values because stale writes could leave the shared counter at one when both workers returned Claimed."
  );
}

// 6. Scope and release gates
{
  const slide = baseSlide("A tested local prototype still has release gates", 6);
  text(slide, "subtitle", "Current evidence describes two in-memory .NET examples, not a hosted product.", 64, 176, 1120, 52, {
    size: 25,
    color: C.muted,
  });
  text(slide, "implemented-heading", "VERIFIED LOCALLY", 72, 254, 500, 34, {
    size: 20,
    bold: true,
    color: C.tealDark,
  });
  text(slide, "implemented-list", "Two synthetic .NET 8 concurrency scenarios\nFrozen Coyote baseline reports, traces, and checksums\n13 / 13 sequential tests and 9 / 9 Coyote tests\n500 fair paths per repaired scenario at a 1,000-step limit", 72, 299, 526, 230, {
    size: 22,
    color: C.text,
  });

  line(slide, 638, 252, 2, C.rule, 300);
  text(slide, "open-heading", "STILL OPEN", 690, 254, 500, 34, {
    size: 20,
    bold: true,
    color: "#A7671B",
  });
  text(slide, "open-list", "Verified release tag\nGenuine Bob session-summary screenshots\nHosted user-facing demo and application URL\nRecorded MP4 with a real working-product demo\nRemote CI, benchmark, and event eligibility checks", 690, 299, 516, 230, {
    size: 21,
    color: C.text,
  });
  line(slide, 64, 552, 1152, C.rule);
  text(slide, "scope-caveat", "Exact schedule alignment for both supplied post-fix traces remains unresolved. Bounded results do not guarantee race freedom.", 72, 572, 1115, 64, {
    size: 20,
    bold: true,
    color: C.text,
  });
  note(slide,
    "Release status from docs/milestones.md and docs/competition-compliance.md. The public source repository is https://github.com/KashyapSinh-Gohil/Interleave and the published source commit is 1fd5481. No verified release tag exists. A public web application, deployment URL, benchmark, remote CI result, Bob session-summary screenshot folder, and recorded demo video remain open. Exact schedule alignment remains unresolved for both frozen post-fix traces. The official event checklist asks for a public code repository, Bob session-summary screenshots, demo platform/application URL, PDF slide presentation, and MP4 video. Sources: https://lablab.ai/ai-hackathons/ibm-bob-2-hackathon#what-to-submit and https://lablab-ibm-bob-2-hackathon-guide.s3.us.cloud-object-storage.appdomain.cloud/index.html"
  );
}

const draftPath = path.join(buildDir, "candidate-m2.pptx");
await (await PresentationFile.exportPptx(pres)).save(draftPath);

for (let index = 0; index < pres.slides.items.length; index++) {
  const slide = pres.slides.items[index];
  const png = await pres.export({ slide, format: "png", scale: 2 });
  await fs.writeFile(
    path.join(buildDir, `m2-slide-${String(index + 1).padStart(2, "0")}.png`),
    new Uint8Array(await png.arrayBuffer()),
  );
  const layout = await slide.export({ format: "layout" });
  await fs.writeFile(
    path.join(buildDir, `m2-slide-${String(index + 1).padStart(2, "0")}.layout.json`),
    await layout.text(),
  );
}

const finalPath = path.join(outputDir, "Interleave_Pitch_Deck_M2.pptx");
const result = await finalizePresentation({
  explicitTotalSlideCount: 6,
  requiredNativeTableOwnerSlides: [],
  requiredNativeChartOwnerSlides: [],
  workspaceDir,
  candidatePath: draftPath,
  finalPath,
  pythonExecutable: process.env.RUNTIME_PYTHON,
  integrityValidatorPath: path.join(skillDir, "container_tools/inspect_presentation_package_integrity.py"),
  layoutValidatorPath: path.join(skillDir, "container_tools/inspect_presentation_layout_geometry.py"),
  layoutArgs: ["--expected-slide-size-emu", "12192000,6858000", "--validate-bullet-geometry", "--validate-heading-fit"],
  fontPolicy: { basis: "design", families: [font] },
  verifyArtifactToolImport: true,
  receiptPath: path.join(buildDir, "Interleave_Pitch_Deck_M2_SubmissionStatusUpdated.validation.json"),
});

const pdfPath = path.join(outputDir, "Interleave_Pitch_Deck_M2.pdf");
try { await fs.unlink(pdfPath); } catch (error) { if (error.code !== "ENOENT") throw error; }
const pdf = new Canvas(960, 540);
let pdfContext = pdf.getContext("2d");
for (let index = 0; index < pres.slides.items.length; index++) {
  if (index > 0) pdfContext = pdf.newPage();
  const pngPath = path.join(buildDir, `m2-slide-${String(index + 1).padStart(2, "0")}.png`);
  const image = await loadImage(pngPath);
  pdfContext.drawImage(image, 0, 0, 960, 540);
}
await pdf.toFile(pdfPath);
console.log(JSON.stringify({ finalPath, pdfPath, font, result }, null, 2));
