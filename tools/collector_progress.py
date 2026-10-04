#!/usr/bin/env python3
"""Validate the curated progress register and render its Markdown projection."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LABELS = {"historical_pass": "Earlier build passed", "pass_scoped": "Scoped checks passed",
          "not_tested": "Not tested", "rejected": "Assessment rejected", "failed": "Failed"}


def validate(data):
    if data["schema"] != 1:
        raise ValueError("Unsupported progress schema")
    artifacts = {a["id"]: a for a in data["artifacts"]}
    cases = {c["id"]: c for c in data["cases"]}
    adapters = {a["id"]: a for a in data["adapters"]}
    for key, rows in ((artifacts, data["artifacts"]), (cases, data["cases"]),
                      (adapters, data["adapters"])):
        if len(key) != len(rows):
            raise ValueError("Duplicate progress IDs")
    for a in artifacts.values():
        if a["audience"] not in ("public", "owner_only"):
            raise ValueError("Unknown artifact audience")
        if a["sha256"] is not None and not re.fullmatch(r"[0-9a-f]{64}", a["sha256"]):
            raise ValueError("Invalid executable digest")
    for c in cases.values():
        if c["artifact"] not in artifacts or (c["adapter"] and c["adapter"] not in adapters):
            raise ValueError("Unknown case artifact or adapter")
        if not c["observation"] or not c["gaps"] or not c["kind"]:
            raise ValueError("Cases require observations, evidence kind and gaps")
    for a in adapters.values():
        if set(a['public_stages']) != {'capture', 'stop', 'review_sanitize', 'upload_readback'} or \
           set(a['owner_stages']) != {'local_decode', 'model_advice'}:
            raise ValueError('Missing or unexpected qualification stage')
        linked = [cases[i] for i in a["evidence"]]
        if any(c["adapter"] != a["id"] for c in linked):
            raise ValueError("Evidence belongs to a different adapter")
        for group, audience in (("public_stages", "public"), ("owner_stages", "owner_only")):
            for state in a[group].values():
                if state not in LABELS:
                    raise ValueError("Unknown stage state")
                if state != "not_tested" and not any(
                    artifacts[c["artifact"]]["audience"] == audience for c in linked
                ):
                    raise ValueError("Qualified stage requires evidence for its audience")
                if state == 'pass_scoped' and not any(
                    artifacts[c['artifact']]['audience'] == audience and
                    artifacts[c['artifact']]['version'] == data[
                        'public_version' if audience == 'public' else 'owner_version'] and
                    c['result'] == 'pass_scoped' for c in linked
                ):
                    raise ValueError('Current pass requires current-artifact evidence')
                if state == "historical_pass" and not any(
                    artifacts[c["artifact"]]["audience"] == audience and
                    artifacts[c["artifact"]]["version"] != data["public_version"] and
                    c["result"] == "pass_scoped" for c in linked
                ):
                    raise ValueError("Historical pass requires earlier-build evidence")
        if not a["next_test"] or not a["completion"]:
            raise ValueError("Adapter needs next test and completion criteria")
    return data


def load(path=ROOT / "docs/COLLECTOR_PROGRESS.json"):
    return validate(json.loads(Path(path).read_text()))


def render(data):
    lines = ["# OpenSAAB Collector progress", "",
             "Generated from `COLLECTOR_PROGRESS.json`; edit that register, then run "
             "`python3 tools/collector_progress.py`. Never edit this projection directly.", "",
             f"Updated: {data['updated_pacific']}. Public preview: **{data['public_version']}**; "
             f"owner research: **{data['owner_version']}**.", "", data["scope"], "",
             "## Adapter matrix", "",
             "Public columns refer to the current preview unless explicitly labelled earlier build. "
             "General synthetic sanitizer checks do not qualify any adapter's live sanitized upload.", "",
             "| Adapter | Capture | Stop | Live review/sanitize | Upload + read-back | Owner decode | Owner model advice |",
             "|---|---|---|---|---|---|---|"]
    for a in data["adapters"]:
        values = list(a["public_stages"].values()) + list(a["owner_stages"].values())
        lines.append("| " + " | ".join([a["name"]] + [LABELS[s] for s in values]) + " |")
    for a in data["adapters"]:
        lines += ["", f"### {a['name']}", "", f"Environment: {a['platform']}. Evidence: " +
                  (", ".join(f"`{i}`" for i in a["evidence"]) or "none recorded") + ".", "",
                  f"Next test: {a['next_test']}", "", f"Complete when: {a['completion']}"]
    lines += ["", "## Next actions", ""]
    lines += [f"- **{a['priority']} · {a['state']} · {a['id']}:** {a['action']}" for a in data["next_actions"]]
    lines += ["", "## Evidence by exact artifact", ""]
    artifacts = {a["id"]: a for a in data["artifacts"]}
    for a in artifacts.values():
        lines += [f"### {a['version']} · {a['audience']}", "",
                  f"Source: `{a['source'] or 'not recorded'}`. Executable SHA-256: "
                  f"`{a['sha256'] or 'not recorded'}`.", "", f"Evidence reference: {a['evidence']}.", ""]
        for c in data["cases"]:
            if c["artifact"] == a["id"]:
                lines += [f"- **{c['id']} · {c['kind']} · {c['date_pacific']} · {c['result']}:** "
                          f"{c['observation']} Gap: {c['gaps']}"]
        lines.append("")
    lines += ["## Limits", ""] + [f"- {s}" for s in data["limits"]]
    lines += ["", "## Maintaining the record", "",
              "Append a case for every meaningful build, test, failed attempt or field receipt. "
              "Keep prior results and gaps; never replace a failed model evaluation with a later retry. "
              "A new release begins unqualified until exact-artifact evidence exists. "
              "Record unknown driver/firmware values as unknown, not inferred from a model name.", "",
              "For each adapter record exact make/model/variant, Windows edition/build/architecture, "
              "vendor driver and firmware version, Collector version/source/executable digest, "
              "USB transport and selected address, bench/field/synthetic context, Pacific arrival/test time, "
              "operation and failure stage, capture integrity/pairing, cleanup/stop, review/consent, "
              "private upload receipt and read-back, local decoding/model result, gaps, next test "
              "and completion criteria. Keep identifiers and raw evidence in a private receipt; "
              "this register contains only reviewed safe summaries.", "",
              "One family can have several profiles: do not collapse MDI 1/2 or Nano variants into "
              "a universal support claim. A new adapter starts with every stage untested. "
              "Owner-only tests cannot qualify public builds. A successful capture is useful evidence "
              "even when diagnostics failed.", "",
              "After editing, run the renderer and inspect the diff. On the Mini, run "
              "`/Users/mini4/Documents/Projects/djfremen-dashboard/.venv/bin/python "
              "/Users/mini4/Documents/Projects/djfremen-dashboard/tools/sync_local_dashboard.py` "
              "to regenerate the dedicated Collector card and dashboard JSON from this same register. "
              "This is an explicit maintenance step; it does not upload captures, publish a release, "
              "or run background model requests.", "",
              "Earlier retired shim/native-log progress is preserved in the repository history and "
              "dated documentation. It does not qualify the current USBPcap application.", ""]
    return "\n".join(lines)


if __name__ == "__main__":
    (ROOT / "docs/COLLECTOR_PROGRESS.md").write_text(render(load()))
    print("Validated and rendered Collector progress")
