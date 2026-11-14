"""Prepare release chart comparisons with Musoq, retaining the local query inputs and output."""

import csv
import json
import math
import shutil
import subprocess
import tempfile
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
QUERY_TEMPLATE = Path(__file__).with_name("Compare-ReadmeBenchmarks.sql")
RUNNER_PROJECT = ROOT / "src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj"
RESULT_FIELDS = {
    "Sequence", "VersionLabel", "CaseId", "MeanNanoseconds", "AllocatedBytes",
    "TimeChangePercent", "AllocationChangePercent",
}


def write_measurements(campaigns, case_ids, destination):
    with destination.open("w", encoding="utf-8", newline="") as stream:
        writer = csv.writer(stream)
        writer.writerow(["Sequence", "VersionLabel", "CaseId", "MeanNanoseconds", "AllocatedBytes"])
        for sequence, campaign in enumerate(campaigns):
            version = campaign["manifest"]["provenance"]["build"]["engineVersion"]
            for case_id in case_ids:
                measurement = campaign["measurements"][case_id]
                writer.writerow([
                    sequence, f"Musoq {version}", case_id,
                    measurement["meanNanoseconds"], measurement["allocatedBytesPerOperation"],
                ])


def validate_measurement(row, campaign):
    case_id = row["CaseId"]
    version = campaign["manifest"]["provenance"]["build"]["engineVersion"]
    if row["VersionLabel"] != f"Musoq {version}":
        raise ValueError(f"Musoq comparison version does not match the campaign: {case_id}")
    measurement = campaign["measurements"][case_id]
    for column, original in [("MeanNanoseconds", "meanNanoseconds"), ("AllocatedBytes", "allocatedBytesPerOperation")]:
        value = row[column]
        if type(value) not in (int, float) or not math.isfinite(value) or value <= 0:
            raise ValueError(f"Invalid Musoq comparison metric: {case_id}, {column}")
        if not math.isclose(value, measurement[original], rel_tol=1e-12, abs_tol=0):
            raise ValueError(f"Musoq comparison metric differs from retained evidence: {case_id}, {column}")


def validate_percentages(row):
    for column in ("TimeChangePercent", "AllocationChangePercent"):
        value = row[column]
        if row["Sequence"] == 0:
            if value is not None:
                raise ValueError(f"First version must have no preceding-version change: {row['CaseId']}")
        elif type(value) not in (int, float) or not math.isfinite(value):
            raise ValueError(f"Missing or invalid Musoq percentage: {row['CaseId']}, {column}")


def index_comparisons(rows, campaigns, case_ids):
    if not isinstance(rows, list) or len(rows) != len(campaigns) * len(case_ids):
        raise ValueError("Musoq must return exactly one comparison per selected case and version")
    indexed = {case_id: {} for case_id in case_ids}
    for row in rows:
        if not isinstance(row, dict) or set(row) != RESULT_FIELDS:
            raise ValueError("Musoq returned an unexpected comparison schema")
        sequence, case_id = row["Sequence"], row["CaseId"]
        if type(sequence) is not int or not 0 <= sequence < len(campaigns) or case_id not in indexed:
            raise ValueError("Musoq returned an unexpected case or version sequence")
        if sequence in indexed[case_id]:
            raise ValueError(f"Duplicate Musoq comparison: {case_id}, {sequence}")
        validate_measurement(row, campaigns[sequence])
        validate_percentages(row)
        indexed[case_id][sequence] = row
    expected = set(range(len(campaigns)))
    if any(set(versions) != expected for versions in indexed.values()):
        raise ValueError("Musoq comparison omitted a selected version")
    return {case_id: [versions[sequence] for sequence in range(len(campaigns))] for case_id, versions in indexed.items()}


def build_runner(executable, work_directory):
    artifacts = ROOT / ".artifacts/benchmarks/readme-charts/runner"
    result = subprocess.run(
        [executable, "build", str(RUNNER_PROJECT), "-c", "Release", "--nologo", "--verbosity", "quiet",
         "--artifacts-path", str(artifacts)],
        cwd=ROOT, capture_output=True, text=True, encoding="utf-8", timeout=180,
    )
    (work_directory / "build.log").write_text(result.stdout + result.stderr, encoding="utf-8")
    if result.returncode != 0:
        raise RuntimeError(f"Comparison runner build failed; see {work_directory / 'build.log'}")
    return artifacts / "bin/Musoq.Benchmarks/release/Musoq.Benchmarks.dll"


def run_query(executable, runner, query_path, work_directory):
    try:
        result = subprocess.run(
            [executable, str(runner), "--compare-readme", str(query_path)],
            cwd=ROOT, capture_output=True, text=True, encoding="utf-8", timeout=120,
        )
    except subprocess.TimeoutExpired as exception:
        raise RuntimeError(f"Musoq comparison exceeded 120 seconds; inputs retained in {work_directory}") from exception
    (work_directory / "musoq.stdout.json").write_text(result.stdout, encoding="utf-8")
    (work_directory / "musoq.stderr.log").write_text(result.stderr, encoding="utf-8")
    if result.returncode != 0:
        raise RuntimeError(f"Musoq comparison failed (exit {result.returncode}); see {work_directory / 'musoq.stderr.log'}")
    return json.loads(result.stdout)


def prepare_comparisons(campaigns, case_ids, work_directory=None):
    command = shutil.which("dotnet")
    if command is None:
        raise FileNotFoundError("The .NET SDK is required to build the repository comparison runner")
    if work_directory is None:
        parent = ROOT / ".artifacts/benchmarks/readme-charts"
        parent.mkdir(parents=True, exist_ok=True)
        work_directory = Path(tempfile.mkdtemp(prefix="run-", dir=parent))
    else:
        work_directory = work_directory.resolve()
        if not work_directory.is_relative_to(ROOT / ".artifacts"):
            raise ValueError("Comparison evidence must stay under the repository-root .artifacts directory")
        work_directory.mkdir(parents=True, exist_ok=False)
    csv_path = work_directory / "measurements.csv"
    write_measurements(campaigns, case_ids, csv_path)
    template = QUERY_TEMPLATE.read_text(encoding="utf-8")
    if template.count("__MEASUREMENTS_PATH__") != 1:
        raise ValueError("Comparison query must contain exactly one CSV path placeholder")
    query = template.replace("__MEASUREMENTS_PATH__", csv_path.as_posix().replace("'", "''"))
    query_path = work_directory / "comparison.sql"
    query_path.write_text(query, encoding="utf-8", newline="\r\n")
    runner = build_runner(command, work_directory)
    rows = run_query(command, runner, query_path, work_directory)
    comparisons = index_comparisons(rows, campaigns, case_ids)
    (work_directory / "comparisons.json").write_text(json.dumps(rows, indent=2) + "\n", encoding="utf-8")
    print(f"Musoq comparison evidence: {work_directory}")
    return comparisons
