#!/usr/bin/env python3
"""Read-only structural/provenance checker; does not run or certify Musoq tests.

Python 3.10+; standard library only. All Git calls are read-only argument arrays.
Examples (from repository root):
  python campaigns/recovery-v3/tools/check_campaign.py --status
  python campaigns/recovery-v3/tools/check_campaign.py --format json
  python campaigns/recovery-v3/tools/check_campaign.py --verify-evidence --verify-commits
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
import sys
from datetime import datetime
from pathlib import Path
from typing import Any


def unique_object(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
    result: dict[str, Any] = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"Duplicate JSON key: {key}")
        result[key] = value
    return result


def load(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=unique_object)


def contained(root: Path, value: str) -> Path:
    if not isinstance(value, str) or not value:
        raise ValueError("Artifact path must be a nonempty relative path")
    result = (root / value).resolve()
    result.relative_to(root.resolve())
    return result


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def pointer(document: Any, value: str) -> Any:
    if value == "":
        return document
    if not value.startswith("/"):
        raise ValueError(f"Not a JSON pointer: {value}")
    node = document
    for part in value[1:].split("/"):
        key = part.replace("~1", "/").replace("~0", "~")
        node = node[int(key)] if isinstance(node, list) else node[key]
    return node


def git(root: Path, *args: str) -> str:
    result = subprocess.run(
        ["git", "-C", str(root), *args], capture_output=True, text=True,
        encoding="utf-8", errors="replace", timeout=30, check=False,
    )
    if result.returncode:
        raise ValueError(f"Read-only git command failed: {result.stderr.strip()}")
    return result.stdout


def inspect_campaign(path: Path, evidence: bool = False, commits: bool = False) -> tuple[dict[str, Any], list[str]]:
    root = path.parent.resolve()
    document = load(path)
    problems: list[str] = []

    def require(condition: bool, message: str) -> None:
        if not condition:
            problems.append(message)

    require(document.get("formatVersion") == 3, "Expected formatVersion=3")
    scopes = document.get("scopes", [])
    expected_ids = [f"REC-{i:03}" for i in range(84, 166)]
    ids = [s.get("id") for s in scopes]
    require(ids == expected_ids, "Scope IDs/order must be exactly REC-084..REC-165")
    require(document.get("campaign", {}).get("scopeCount") == len(scopes) == 82,
            "Manifest scopeCount must equal 82 actual scopes")
    require(document.get("activeLane") in ("engine", "product", "empirical"), "Unknown activeLane")
    require(document.get("fullGate", {}).get("requiredAfterEveryScope") is True, "Full gate must be required after every scope")
    require(document.get("fullGate", {}).get("allRequiredTestsMustExecuteAndPass") is True, "All required tests must execute and pass")
    require(document.get("commitPolicy", {}).get("completionInSameCommit") is True, "Completion must be in the scope commit")
    require(document.get("commitPolicy", {}).get("exactlyOneCompletionCommitPerScope") is True, "One completion commit per scope is required")
    goal = (root / "musoq-recovery-campaign.goal.md").read_text(encoding="utf-8")
    require(goal.rstrip() == document.get("goalPrompt"), "Standalone and embedded goal prompts differ")

    source_map = {s["id"]: s for s in document.get("sources", [])}
    require(len(source_map) == len(document.get("sources", [])), "Duplicate source IDs")
    source_content: dict[str, str] = {}
    for sid, source in source_map.items():
        file = contained(root, source["file"])
        require(file.is_file(), f"Missing source snapshot: {file}")
        if file.is_file():
            require(sha256(file) == source["sha256"], f"Source snapshot changed: {sid}")
            source_content[sid] = file.read_text(encoding="utf-8-sig")
    prior = json.loads(source_content.get("history", "{}"))
    historical_ids = {s["id"] for s in prior.get("scopes", [])}
    require(len(historical_ids) == 83, "Historical snapshot must preserve 83 unique IDs")
    if "history" in source_map:
        require(document["priorCampaign"]["expectedSha256"] == source_map["history"]["sha256"], "History digest fields disagree")

    completed: set[str] = set()
    obligation_ids: set[str] = set()
    active = 0
    for index, scope in enumerate(scopes):
        sid = scope["id"]
        require(scope.get("order") == 84 + index, f"Order mismatch: {sid}")
        lane = "engine" if index < 64 else "product" if index < 76 else "empirical"
        require(scope.get("lane") == lane, f"Lane mismatch: {sid}")
        require(scope.get("dependsOn") == ([expected_ids[index - 1]] if index else []), f"Sequential dependency mismatch: {sid}")
        require(all(p in historical_ids for p in scope.get("priorScopeIds", [])), f"Unknown historical reference: {sid}")
        require(scope.get("state") in ("pending", "in_progress", "blocked", "completed"), f"Invalid state: {sid}")
        require(isinstance(scope.get("completed"), bool) and isinstance(scope.get("testsPassed"), bool), f"Completion flags must be Boolean: {sid}")
        require((scope.get("state") == "completed") == scope.get("completed"), f"Completion state/flag disagree: {sid}")
        if scope.get("state") in ("in_progress", "blocked"):
            active += 1
            require(all(dep in completed for dep in scope.get("dependsOn", [])), f"Active scope has unfinished prerequisite: {sid}")
        if scope.get("state") == "blocked":
            require(bool(scope.get("blocker")), f"Blocked scope lacks blocker details: {sid}")
        for entry in scope.get("sources", []):
            key = entry["sourceId"]
            require(key in source_content, f"Unknown/unavailable source {key}: {sid}")
            if key not in source_content:
                continue
            for section in entry["sections"]:
                try:
                    if entry["selectorKind"] == "json-pointer":
                        pointer(json.loads(source_content[key]), section)
                    else:
                        headings = {re.sub(r"^#{1,6}\s+", "", line).strip()
                                    for line in source_content[key].splitlines() if re.match(r"^#{1,6}\s+", line)}
                        require(section in headings, f"Missing exact heading {key}/{section}: {sid}")
                except (KeyError, IndexError, TypeError, ValueError) as ex:
                    problems.append(f"Broken source selector {key}/{section}: {sid}: {ex}")
        for ob in scope.get("obligations", []):
            oid = ob["id"]
            require(oid not in obligation_ids, f"Duplicate obligation ID: {oid}")
            require(oid.startswith(sid + "-O"), f"Wrong obligation owner: {oid}")
            require(ob.get("status") in ("pending", "in_progress", "evidenced", "blocked"), f"Invalid obligation state: {oid}")
            if ob.get("status") == "evidenced":
                require(bool(ob.get("evidenceIds")), f"Evidenced obligation has no artifact IDs: {oid}")
            obligation_ids.add(oid)
        if not scope.get("completed"):
            require(scope.get("testsPassed") is False, f"Incomplete scope claims final testsPassed: {sid}")
            continue
        require(scope.get("testsPassed") is True, f"Completed scope lacks passing tests: {sid}")
        require(all(dep in completed for dep in scope.get("dependsOn", [])), f"Completed scope has incomplete predecessor: {sid}")
        require(all(ob.get("status") == "evidenced" and ob.get("evidenceIds") for ob in scope["obligations"]), f"Completed scope lacks obligation evidence: {sid}")
        record = scope.get("completion", {})
        for key in ("completedAt", "validatedFromCommit", "testedPayloadSha256", "reportPath", "reportSha256", "fullGateEvidence", "reviewEvidence", "testCommands", "testSummary", "commitMessage"):
            require(bool(record.get(key)), f"Completed scope missing {key}: {sid}")
        if record.get("completedAt"):
            try:
                require(datetime.fromisoformat(record["completedAt"].replace("Z", "+00:00")).tzinfo is not None,
                        f"Completion timestamp needs timezone: {sid}")
            except ValueError:
                problems.append(f"Invalid completion timestamp: {sid}")
        require(sid in (record.get("commitMessage") or ""), f"Commit message lacks scope ID: {sid}")
        for key in ("reportSha256", "testedPayloadSha256"):
            require(bool(re.fullmatch(r"[0-9a-f]{64}", record.get(key) or "")), f"Invalid {key}: {sid}")
        if scope.get("kind") == "diagnostic":
            require(scope.get("progress", {}).get("cleanStreak", 0) >= scope["limits"]["postCoverageCleanNovelTarget"], f"Clean streak below target: {sid}")
        if evidence:
            file = contained(root, record["reportPath"])
            require(file.is_file() and sha256(file) == record["reportSha256"], f"Report missing/hash mismatch: {sid}")
            for artifact in record["fullGateEvidence"]:
                require(isinstance(artifact, dict) and "path" in artifact and "sha256" in artifact, f"Invalid evidence reference: {sid}")
                if not isinstance(artifact, dict) or "path" not in artifact or "sha256" not in artifact:
                    continue
                ep = contained(root, artifact["path"])
                if not ep.is_file():
                    problems.append(f"Missing full gate evidence: {sid}/{ep}")
                    continue
                require(sha256(ep) == artifact["sha256"], f"Full gate digest mismatch: {sid}")
                run = load(ep)
                require(run.get("scopeId") == sid and run.get("outcome") == "passed", f"Wrong/failed evidence run: {sid}")
                require(run.get("testedPayloadSha256") == record["testedPayloadSha256"], f"Run tested payload differs: {sid}")
                results = run.get("results", [])
                expected = run.get("expectedTargets", [])
                actual = [r.get("targetId") for r in results]
                require(bool(expected) and len(expected) == len(set(expected)), f"Missing/duplicate expected targets: {sid}")
                require(len(actual) == len(set(actual)) and set(expected) == set(actual), f"Missing/duplicate test target results: {sid}")
                require(bool(run.get("commands")) and all(c.get("exitCode") == 0 for c in run["commands"]), f"Failed/missing gate commands: {sid}")
                for r in results:
                    counts = [r.get(k) for k in ("passed", "failed", "skipped", "total", "discovered")]
                    valid_counts = all(type(c) is int and c >= 0 for c in counts)
                    require(valid_counts, f"Invalid counters: {sid}/{r.get('targetId')}")
                    if valid_counts:
                        passed, failed, skipped, total, discovered = counts
                        require(failed == 0 and not r.get("aborted"), f"Failed/aborted target: {sid}/{r['targetId']}")
                        require(passed + failed + skipped == total == discovered and total > 0, f"Unreconciled/empty target counters: {sid}/{r['targetId']}")
                # Fine-grained skip identities and raw runner signatures still require
                # the repository's gate checker; this utility deliberately makes no
                # claim to authenticate independently generated test evidence.
        completed.add(sid)
    require(active <= 1, "More than one active/blocked scope")

    for lane in document.get("lanes", []):
        selected = [s for s in scopes if s.get("lane") == lane["id"]]
        require(len(selected) == lane["scopeCount"], f"Lane count mismatch: {lane['id']}")
    if commits and completed:
        repo_root = Path(git(root, "rev-parse", "--show-toplevel").strip()).resolve()
        ledger_path = path.resolve().relative_to(repo_root).as_posix()
        history = git(repo_root, "log", "--format=%H%x00%B%x00%x1e")
        matches: dict[str, list[str]] = {sid: [] for sid in completed}
        for chunk in history.split("\x1e"):
            parts = chunk.strip().split("\x00")
            if len(parts) < 2:
                continue
            commit, body = parts[0], parts[1]
            if "Recovery-Campaign: recovery-v3" not in body:
                continue
            for sid in completed:
                if re.search(rf"^Scope-ID: {re.escape(sid)}\s*$", body, re.M):
                    matches[sid].append(commit)
        for sid, hashes in matches.items():
            require(len(hashes) == 1, f"Expected one completion commit for {sid}, got {len(hashes)}")
            if len(hashes) == 1:
                committed = json.loads(git(repo_root, "show", f"{hashes[0]}:{ledger_path}"))
                item = next((s for s in committed["scopes"] if s["id"] == sid), None)
                require(bool(item and item.get("completed") and item.get("testsPassed")), f"Completion commit did not contain completed ledger: {sid}")
        head_doc = json.loads(git(repo_root, "show", f"HEAD:{ledger_path}"))
        head_done = {s["id"] for s in head_doc["scopes"] if s.get("completed")}
        require(completed <= head_done, "Working ledger contains uncommitted completed scopes")

    summary = {"valid": not problems, "campaignId": document.get("campaign", {}).get("id"),
               "activeLane": document.get("activeLane"), "scopeCount": len(scopes),
               "obligationCount": len(obligation_ids), "completedScopes": len(completed),
               "lanes": {}, "checks": {"sourceSnapshots": True, "evidenceFilesRequested": evidence,
               "gitCommitsRequested": commits, "runsMusoqTests": False}}
    for lane in ("engine", "product", "empirical"):
        selected = [s for s in scopes if s.get("lane") == lane]
        pending = next((s["id"] for s in selected if not s.get("completed")), None)
        summary["lanes"][lane] = {"completed": sum(bool(s.get("completed")) for s in selected),
                                  "total": len(selected), "nextIncomplete": pending}
    return summary, problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--campaign", type=Path, default=Path("musoq-recovery-campaign.json"))
    parser.add_argument("--status", action="store_true", help="Show concise lane progress (structure is still checked)")
    parser.add_argument("--verify-evidence", action="store_true", help="Check completed report/gate files and hashes; does not run tests")
    parser.add_argument("--verify-commits", action="store_true", help="Read Git to check completed ledger/commit mappings")
    parser.add_argument("--format", choices=("text", "json"), default="text")
    args = parser.parse_args()
    try:
        summary, problems = inspect_campaign(args.campaign.resolve(), args.verify_evidence, args.verify_commits)
    except (OSError, ValueError, KeyError, TypeError, subprocess.SubprocessError) as error:
        summary, problems = {"valid": False}, [str(error)]
    if args.format == "json":
        print(json.dumps({**summary, "problems": problems}, indent=2))
    elif problems:
        for message in problems:
            print(f"ERROR: {message}", file=sys.stderr)
    else:
        print(f"Campaign structure valid: {summary['scopeCount']} scopes; {summary['obligationCount']} obligations.")
        for lane, state in summary["lanes"].items():
            print(f"{lane}: {state['completed']}/{state['total']} completed; next={state['nextIncomplete'] or 'none'}")
        print("This checker does not execute or independently certify Musoq tests.")
    return 2 if problems else 0


if __name__ == "__main__":
    raise SystemExit(main())
