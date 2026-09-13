#!/usr/bin/env python3
"""Simulated blind-repair boundary for REC-144.

This module is a capability-boundary test harness, not an operating-system
sandbox and not a live repair actor.  A fake actor receives a deliberately
small public envelope and a gateway with only bounded observation and submit
operations.  Denied requests are retained as contamination evidence and are
excluded from any semantic score.
"""
from __future__ import annotations

import copy
import hashlib
import json
from pathlib import Path
from typing import Any, Callable, Iterable, Mapping


PUBLIC_TASK_FIELDS = (
    "taskId",
    "taskPrompt",
    "malformedQuery",
    "deliveredDiagnostic",
    "invariant",
    "allowedOutcomes",
    "mustExecute",
)
ALLOWED_DISCOVERY = {
    "schema_shape",
    "source_methods",
    "runtime_settings_metadata",
}
ALLOWED_SUBMISSION_FIELDS = {
    "outcome",
    "repairedQuery",
    "requestedObservations",
    "columns",
    "rowCount",
    "ordering",
    "rowFingerprints",
}
PRIVATE_KEYS = {
    "answer",
    "expectedRows",
    "expectedResult",
    "expectedQuery",
    "goldSql",
    "privateJudge",
    "repairedQueryExpected",
    "seedGroup",
    "semanticFamily",
    "providerFixtureId",
    "fixtureRef",
    "partition",
}


class BoundaryFailure(ValueError):
    """A malformed actor contract or invalid harness configuration."""


def canonical_bytes(value: Any) -> bytes:
    return json.dumps(
        value,
        ensure_ascii=False,
        sort_keys=True,
        separators=(",", ":"),
        allow_nan=False,
    ).encode("utf-8")


def digest(value: Any) -> str:
    return hashlib.sha256(canonical_bytes(value)).hexdigest()


def _contains_key(value: Any, names: set[str]) -> bool:
    if isinstance(value, dict):
        return any(key in names or _contains_key(child, names) for key, child in value.items())
    if isinstance(value, list):
        return any(_contains_key(child, names) for child in value)
    return False


def build_public_envelope(task: Mapping[str, Any]) -> dict[str, Any]:
    """Select only the task contract visible to a repair actor."""
    missing = [field for field in PUBLIC_TASK_FIELDS if field not in task]
    if missing:
        raise BoundaryFailure("task is missing public fields: " + ", ".join(missing))
    public = {field: copy.deepcopy(task[field]) for field in PUBLIC_TASK_FIELDS}
    if _contains_key(public, PRIVATE_KEYS):
        raise BoundaryFailure("public task envelope contains private-answer or split data")
    return public


def _safe_excerpt(value: Any, maximum: int) -> Any:
    """Bound event details without exposing arbitrary actor data."""
    try:
        encoded = json.dumps(value, ensure_ascii=False, sort_keys=True)
    except (TypeError, ValueError):
        return "<unserializable>"
    if len(encoded.encode("utf-8")) > maximum:
        return "<redacted:bounded-output>"
    return copy.deepcopy(value)


class _CapabilityGateway:
    """The only object supplied to a fake actor.

    It intentionally has no filesystem, process, Git, judge or workspace
    operation.  The harness remains a same-process simulation; this API shape
    must not be described as proof of isolation from a malicious process.
    """

    __slots__ = ("_observe", "_request", "_submit")

    def __init__(
        self,
        observe: Callable[[str], dict[str, Any]],
        request: Callable[[str, Any], dict[str, Any]],
        submit: Callable[[Mapping[str, Any]], dict[str, Any]],
    ) -> None:
        self._observe = observe
        self._request = request
        self._submit = submit

    def observe(self, name: str) -> dict[str, Any]:
        return self._observe(name)

    def request(self, operation: str, argument: Any = None) -> dict[str, Any]:
        return self._request(operation, argument)

    def submit(self, answer: Mapping[str, Any]) -> dict[str, Any]:
        return self._submit(answer)


class BlindRepairSession:
    """Run one fake actor behind a bounded, auditable capability gateway."""

    def __init__(
        self,
        *,
        scope_id: str,
        task: Mapping[str, Any],
        discovery: Mapping[str, Any],
        judge: Callable[[Mapping[str, Any], Mapping[str, Any]], Mapping[str, Any]],
        actor_id: str,
        actor_kind: str,
        instructions: str,
        capabilities: Iterable[str],
        working_directory: str = "simulated-actor-cwd",
        max_turns: int = 8,
        max_submissions: int = 1,
        max_output_bytes: int = 16_384,
    ) -> None:
        if not scope_id.strip() or not actor_id.strip() or not instructions.strip():
            raise BoundaryFailure("scope, actor identity and instructions are required")
        if actor_kind != "fake":
            raise BoundaryFailure("REC-144 construction runs require fake actors")
        if max_turns < 1 or max_submissions < 1 or max_output_bytes < 256:
            raise BoundaryFailure("budgets must be positive and bounded")
        self._scope_id = scope_id
        self._task = build_public_envelope(task)
        self._discovery = copy.deepcopy(dict(discovery))
        if _contains_key(self._discovery, PRIVATE_KEYS):
            raise BoundaryFailure("discovery contains private-answer or split data")
        unknown_discovery = set(self._discovery) - ALLOWED_DISCOVERY
        if unknown_discovery:
            raise BoundaryFailure("discovery contains unallowlisted observations: " + ", ".join(sorted(unknown_discovery)))
        self._judge = judge
        self._actor = {
            "id": actor_id,
            "kind": actor_kind,
            "instructions": instructions,
            "instructionsSha256": digest(instructions),
            "capabilities": sorted(set(capabilities)),
        }
        self._working_directory = working_directory
        self._max_turns = max_turns
        self._max_submissions = max_submissions
        self._max_output_bytes = max_output_bytes
        self._turns = 0
        self._submissions = 0
        self._events: list[dict[str, Any]] = []
        self._contamination_attempts: list[dict[str, Any]] = []
        self._terminal: str | None = None
        self._submission: dict[str, Any] | None = None
        self._score: dict[str, Any] = {
            "status": "not_scored",
            "excluded": True,
            "liveModelScore": False,
            "humanScore": False,
        }

    def _event(self, operation: str, result: str, **details: Any) -> dict[str, Any]:
        event = {
            "turn": self._turns,
            "operation": operation,
            "result": result,
        }
        event.update({key: _safe_excerpt(value, self._max_output_bytes) for key, value in details.items()})
        self._events.append(event)
        return event

    def _consume_turn(self, operation: str) -> bool:
        if self._terminal is not None:
            self._event(operation, "ignored_after_terminal", terminal=self._terminal)
            return False
        self._turns += 1
        if self._turns > self._max_turns:
            self._terminal = "budget_exceeded"
            self._score = {
                "status": "excluded_budget",
                "excluded": True,
                "reason": "turn_budget_exceeded",
                "liveModelScore": False,
                "humanScore": False,
            }
            self._event(operation, "denied", reason="turn_budget_exceeded")
            return False
        return True

    def _observe(self, name: str) -> dict[str, Any]:
        if not self._consume_turn("observe"):
            return {"ok": False, "error": "budget_exceeded"}
        if name not in ALLOWED_DISCOVERY or name not in self._actor["capabilities"]:
            attempt = {"operation": "observe", "name": name, "reason": "observation_not_allowlisted"}
            self._contamination_attempts.append(attempt)
            self._event("observe", "denied", name=name, contamination=True)
            return {"ok": False, "error": "access_denied", "contamination": True}
        if name not in self._discovery:
            self._event("observe", "unavailable", name=name)
            return {"ok": False, "error": "observation_unavailable"}
        value = _safe_excerpt(self._discovery[name], self._max_output_bytes)
        self._event("observe", "allowed", name=name, observation=value)
        return {"ok": True, "name": name, "observation": value}

    def _request(self, operation: str, argument: Any) -> dict[str, Any]:
        if operation == "observe" and isinstance(argument, dict):
            return self._observe(str(argument.get("name", "")))
        if not self._consume_turn("request"):
            return {"ok": False, "error": "budget_exceeded"}
        attempt = {
            "operation": operation,
            "argument": _safe_excerpt(argument, self._max_output_bytes),
            "reason": "capability_not_exposed",
        }
        self._contamination_attempts.append(attempt)
        self._event("request", "denied", requestedOperation=operation, contamination=True)
        return {"ok": False, "error": "access_denied", "contamination": True}

    def _submit(self, answer: Mapping[str, Any]) -> dict[str, Any]:
        if not self._consume_turn("submit"):
            return {"ok": False, "error": "budget_exceeded"}
        if "submit" not in self._actor["capabilities"]:
            self._contamination_attempts.append({"operation": "submit", "reason": "capability_not_exposed"})
            self._event("submit", "denied", reason="capability_not_exposed", contamination=True)
            return {"ok": False, "error": "access_denied", "contamination": True}
        self._submissions += 1
        if self._submissions > self._max_submissions:
            self._terminal = "budget_exceeded"
            self._score = {
                "status": "excluded_budget",
                "excluded": True,
                "reason": "submission_budget_exceeded",
                "liveModelScore": False,
                "humanScore": False,
            }
            self._event("submit", "denied", reason="submission_budget_exceeded")
            return {"ok": False, "error": "budget_exceeded"}
        if not isinstance(answer, Mapping):
            self._terminal = "malformed_answer"
            self._score = {
                "status": "excluded_malformed",
                "excluded": True,
                "reason": "submission_is_not_an_object",
                "liveModelScore": False,
                "humanScore": False,
            }
            self._event("submit", "malformed", reason="submission_is_not_an_object")
            return {"ok": False, "error": "malformed_submission"}
        answer_copy = copy.deepcopy(dict(answer))
        encoded_size = len(canonical_bytes(answer_copy))
        if encoded_size > self._max_output_bytes:
            self._terminal = "malformed_answer"
            self._score = {
                "status": "excluded_malformed",
                "excluded": True,
                "reason": "submission_output_exceeded_bound",
                "liveModelScore": False,
                "humanScore": False,
            }
            self._event("submit", "malformed", reason="submission_output_exceeded_bound")
            return {"ok": False, "error": "malformed_submission"}
        unknown = set(answer_copy) - ALLOWED_SUBMISSION_FIELDS
        if unknown:
            self._terminal = "malformed_answer"
            self._score = {
                "status": "excluded_malformed",
                "excluded": True,
                "reason": "submission_contains_unknown_fields",
                "liveModelScore": False,
                "humanScore": False,
            }
            self._event("submit", "malformed", reason="submission_contains_unknown_fields")
            return {"ok": False, "error": "malformed_submission"}
        self._submission = {
            "received": True,
            "outcome": answer_copy.get("outcome"),
            "repairedQueryProvided": isinstance(answer_copy.get("repairedQuery"), str),
        }
        observation = {key: value for key, value in answer_copy.items() if key != "repairedQuery"}
        try:
            judged = dict(self._judge(self._task, observation))
        except Exception:
            judged = {"passed": False, "reason": "semantic_judge_failed"}
        if judged.get("passed") is True:
            self._terminal = "completed"
            self._score = {
                "status": "passed" if not self._contamination_attempts else "excluded_contaminated",
                "excluded": bool(self._contamination_attempts),
                "reason": "access_boundary_violation" if self._contamination_attempts else None,
                "liveModelScore": False,
                "humanScore": False,
            }
            self._event("submit", "accepted" if not self._contamination_attempts else "accepted_but_excluded")
            return {"ok": True, "passed": not bool(self._contamination_attempts)}
        self._terminal = "semantic_judge_failed"
        self._score = {
            "status": "failed" if not self._contamination_attempts else "excluded_contaminated",
            "excluded": bool(self._contamination_attempts),
            "reason": "semantic_judge_failed" if not self._contamination_attempts else "access_boundary_violation",
            "liveModelScore": False,
            "humanScore": False,
        }
        self._event("submit", "rejected", reason="semantic_judge_failed")
        return {"ok": False, "error": "semantic_judge_failed"}

    def run(self, actor: Callable[[Mapping[str, Any], _CapabilityGateway], Any]) -> dict[str, Any]:
        """Run one actor and return redacted, machine-readable observations."""
        public = copy.deepcopy(self._task)
        gateway = _CapabilityGateway(self._observe, self._request, self._submit)
        try:
            actor(public, gateway)
        except Exception:
            if self._terminal is None:
                self._terminal = "actor_error"
                self._score = {
                    "status": "excluded_actor_error",
                    "excluded": True,
                    "reason": "actor_error",
                    "liveModelScore": False,
                    "humanScore": False,
                }
                self._event("actor", "error", reason="actor_error")
        if self._terminal is None:
            self._terminal = "no_submission"
            self._score = {
                "status": "excluded_no_submission",
                "excluded": True,
                "reason": "actor_did_not_submit",
                "liveModelScore": False,
                "humanScore": False,
            }
            self._event("actor", "incomplete", reason="actor_did_not_submit")
        return {
            "formatVersion": 1,
            "scopeId": self._scope_id,
            "state": "simulated_run",
            "simulated": True,
            "actor": copy.deepcopy(self._actor),
            "instructions": {
                "sha256": self._actor["instructionsSha256"],
                "providedToActor": True,
                "parentReasoningProvided": False,
                "expectedMutationProvided": False,
                "goldAnswerProvided": False,
            },
            "publicEnvelope": {
                "keys": sorted(self._task),
                "digest": digest(self._task),
                "privateKeysAbsent": not _contains_key(self._task, PRIVATE_KEYS),
            },
            "capabilityBoundary": {
                "allowedDiscovery": sorted(ALLOWED_DISCOVERY & set(self._actor["capabilities"])),
                "submitExposed": "submit" in self._actor["capabilities"],
                "filesystemExposed": False,
                "repositoryExposed": False,
                "historyExposed": False,
                "judgeExposed": False,
                "workingDirectory": self._working_directory,
                "workingDirectoryIsNotIsolationBoundary": True,
            },
            "observations": copy.deepcopy(self._events),
            "submission": copy.deepcopy(self._submission),
            "contamination": {
                "detected": bool(self._contamination_attempts),
                "attempts": copy.deepcopy(self._contamination_attempts),
                "retained": True,
                "excludedFromScore": bool(self._contamination_attempts),
            },
            "budget": {
                "turnsUsed": self._turns,
                "maxTurns": self._max_turns,
                "submissionsUsed": self._submissions,
                "maxSubmissions": self._max_submissions,
                "maxOutputBytes": self._max_output_bytes,
            },
            "terminal": self._terminal,
            "score": copy.deepcopy(self._score),
        }


def validate_actor_manifest(manifest: Mapping[str, Any]) -> dict[str, Any]:
    """Validate the non-secret identity/configuration record for fake actors."""
    errors: list[str] = []
    if manifest.get("formatVersion") != 1:
        errors.append("formatVersion must be 1")
    if manifest.get("state") != "simulated_construction" or manifest.get("simulated") is not True:
        errors.append("manifest must be marked simulated_construction")
    actors = manifest.get("actors")
    if not isinstance(actors, list) or not actors:
        errors.append("actors must be nonempty")
        actors = []
    ids: set[str] = set()
    for actor in actors:
        if not isinstance(actor, dict):
            errors.append("each actor must be an object")
            continue
        actor_id = actor.get("actorId")
        if not isinstance(actor_id, str) or not actor_id.strip():
            errors.append("actor IDs must be unique nonempty strings")
        elif actor_id in ids:
            errors.append("actor IDs must be unique nonempty strings")
        else:
            ids.add(actor_id)
        if actor.get("kind") != "fake":
            errors.append(f"actor {actor_id} is not fake")
        if not isinstance(actor.get("instructions"), str) or not actor["instructions"].strip():
            errors.append(f"actor {actor_id} needs explicit instructions")
        capabilities = actor.get("capabilities")
        if not isinstance(capabilities, list) or not capabilities:
            errors.append(f"actor {actor_id} needs explicit capabilities")
        elif any(not isinstance(capability, str) or capability not in ALLOWED_DISCOVERY | {"submit"} for capability in capabilities):
            errors.append(f"actor {actor_id} has an unallowlisted capability")
        if _contains_key(actor, PRIVATE_KEYS):
            errors.append(f"actor {actor_id} contains private-answer or private-context data")
    if errors:
        raise BoundaryFailure("; ".join(errors))
    return {
        "scopeId": manifest.get("scopeId"),
        "actorCount": len(actors),
        "actorIds": sorted(actor["actorId"] for actor in actors),
        "simulated": True,
        "validated": True,
    }


def load_manifest(path: Path) -> tuple[dict[str, Any], dict[str, Any]]:
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise BoundaryFailure(f"cannot load actor manifest: {error}") from error
    return manifest, validate_actor_manifest(manifest)
