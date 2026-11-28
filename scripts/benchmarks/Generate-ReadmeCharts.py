"""Generate README SVGs from retained engine-v3 campaigns (Python 3 and the .NET SDK).

Run from any directory: python scripts/benchmarks/Generate-ReadmeCharts.py
Use --run-directory to select another campaign containing the same scenarios.
Use --history-directories to select two to four ordered campaigns for release trends.
Uses this repository's engine and CSV example; no running service is required.
"""

import argparse
import json
import math
import re
import sys
from html import escape
from pathlib import Path

sys.dont_write_bytecode = True
from ReadmeBenchmarkComparisons import prepare_comparisons


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_RUN = ROOT / "benchmarks/history/engine-v3/20261127T224018042Z-6932c0f6fab1"
DEFAULT_HISTORY = [
    ROOT / "benchmarks/history/engine-v3/20261111T233043559Z-f182beae1eb5",
    ROOT / "benchmarks/history/engine-v3/20261118T231738570Z-958c20272747",
    ROOT / "benchmarks/history/engine-v3/20261122T230407017Z-4cac61673c27",
    DEFAULT_RUN,
]
WORKLOADS = [
    ("X01_SalesReporting", "Sales reporting", "Join, aggregate, rank", "#0369a1"),
    ("X02_JoinWindow", "Join + windows", "Join, running + sliding sums", "#7c3aed"),
    ("X03_Reconciliation", "Reconciliation", "Full outer join, classify, group", "#475569"),
    ("X04_TemporalAnalytics", "Temporal analysis", "ASOF join, lag, running sum", "#b45309"),
    ("X07_LogAnalytics", "Log analytics", "Parse text, filter, aggregate", "#047857"),
    ("X08_PacketAnalytics", "Packet analytics", "Decode binary + text, expand, join", "#be185d"),
]
SCALING = [WORKLOADS[index] for index in (0, 1, 4, 5)]
TREND_WORKLOADS = sorted(WORKLOADS + [
    ("X05_CollectionPivot", "Collection pivoting", "Expand collections, join, pivot", "#6d28d9"),
    ("X06_Consolidation", "Consolidation", "Union, deduplicate, page", "#0e7490"),
    ("X10_StructuredPipeline", "Structured pipeline", "Grouped input, join, windows", "#166534"),
])
RELEASE_COLUMNS = 3
RELEASE_PANEL_WIDTH = 288
RELEASE_PANEL_HEIGHT = 244
RELEASE_PANEL_GAP = 16


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def observed_time_range(campaign, case_id):
    measurement = campaign["measurements"][case_id]
    reports = [path for path in measurement["rawReports"] if path.endswith("-report-full.json")]
    context = f"{campaign['directory']}, {case_id}"
    if len(reports) != 1:
        raise ValueError(f"Expected one full timing report for {context}")
    benchmarks = read_json(campaign["directory"] / reports[0])["Benchmarks"]
    if len(benchmarks) != 1:
        raise ValueError(f"Expected one benchmark in the timing report for {context}")
    statistics = benchmarks[0]["Statistics"]
    lower, mean, upper = (statistics[key] for key in ("Min", "Mean", "Max"))
    if (
        statistics["N"] != measurement["sampleCount"]
        or mean != measurement["meanNanoseconds"]
        or not all(math.isfinite(value) for value in (lower, mean, upper))
        or not 0 <= lower <= mean <= upper
    ):
        raise ValueError(f"Timing report does not match the retained measurement for {context}")
    return lower, upper


def text(x, y, value, *, size=14, fill="#334155", anchor="start", weight="400"):
    return (
        f'<text x="{x:.2f}" y="{y:.2f}" font-size="{size}" fill="{fill}" '
        f'text-anchor="{anchor}" font-weight="{weight}">{escape(str(value))}</text>'
    )


def line(x1, y1, x2, y2, color="#e2e8f0", width=1):
    return (
        f'<line x1="{x1:.2f}" y1="{y1:.2f}" x2="{x2:.2f}" y2="{y2:.2f}" '
        f'stroke="{color}" stroke-width="{width}"/>'
    )


def vertical_whisker(x, lower, upper, color):
    return [
        line(x, lower, x, upper, color, 1.5),
        line(x - 5, lower, x + 5, lower, color, 1.5),
        line(x - 5, upper, x + 5, upper, color, 1.5),
    ]


def frame(title, description, version, *, height=560, subtitle=None):
    if subtitle is None:
        subtitle = f"Musoq {version} | Synthetic in-memory inputs | Table results fully consumed"
    return [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="960" height="{height}" '
        f'viewBox="0 0 960 {height}" role="img" aria-labelledby="title description">',
        f'<title id="title">{escape(title)}</title>',
        f'<desc id="description">{escape(description)}</desc>',
        f'<rect width="960" height="{height}" rx="16" fill="#ffffff"/>',
        '<g font-family="Segoe UI, Helvetica, Arial, sans-serif">',
        text(32, 39, title, size=23, fill="#0f172a", weight="700"),
        text(32, 65, subtitle),
    ]


def finish(parts, provenance, *, height=560):
    parts.extend([
        text(32, height - 46, "Execution only; compilation and source I/O excluded.", size=12),
        text(32, height - 26, f"{provenance['hardware']['cpuModel']} | {provenance['environment']['runtime']} | BenchmarkDotNet Official", size=12),
        "</g></svg>",
    ])
    return "\n".join(parts) + "\n"


def execution_chart(campaign):
    cases, measurements, provenance = campaign["cases"], campaign["measurements"], campaign["manifest"]["provenance"]
    rows = []
    for scenario, label, detail, color in WORKLOADS:
        case_id = f"{scenario}-execution-Table-Large"
        case, result = cases[case_id], measurements[case_id]
        assert case["inputCount"] == 100000 and case["inputUnit"] == "rows", case_id
        lower, upper = observed_time_range(campaign, case_id)
        rows.append((label, detail, color, result, lower / 1e6, upper / 1e6))
    description = "Mean execution time and cumulative managed allocations per query for 100,000 input rows. "
    description += "Timing whiskers show the observed minimum and maximum. "
    description += ". ".join(
        f"{label} averaged {result['meanNanoseconds'] / 1e6:.2f} ms "
        f"({lower:.2f}–{upper:.2f} ms observed), with {result['allocatedBytesPerOperation'] / 1048576:.2f} MiB allocated"
        for label, _, _, result, lower, upper in rows
    )
    parts = frame("Analytics workloads at 100,000 rows", description, provenance["build"]["engineVersion"])
    parts.extend([
        text(32, 96, "Mean execution time | Lower is better | Whiskers show observed min–max", size=12),
        text(912, 124, "Allocated / query", size=12, anchor="end"),
    ])
    maximum = math.ceil(max(upper for _, _, _, _, _, upper in rows) / 20) * 20
    left, width = 300, 480
    scale = lambda value: left + value / maximum * width
    for index in range(5):
        tick = maximum * index / 4
        x = scale(tick)
        parts.extend([line(x, 135, x, 461), text(x, 482, f"{tick:g} ms", size=12, anchor="middle")])
    for index, (label, detail, color, result, lower, upper) in enumerate(rows):
        y = 151 + index * 53
        mean = result["meanNanoseconds"] / 1e6
        lower, upper = scale(lower), scale(upper)
        parts.extend([
            text(32, y + 8, label, size=15, weight="600"),
            text(32, y + 26, detail, size=11),
            f'<rect x="{left}" y="{y}" width="{scale(mean) - left:.2f}" height="22" rx="3" fill="{color}"/>',
            line(lower, y + 11, upper, y + 11, "#0f172a", 1.5),
            line(lower, y + 6, lower, y + 16, "#0f172a", 1.5),
            line(upper, y + 6, upper, y + 16, "#0f172a", 1.5),
            text(upper + 9, y + 16, f"{mean:.2f}", size=12, weight="600"),
            text(912, y + 16, f"{result['allocatedBytesPerOperation'] / 1048576:.2f} MiB", anchor="end"),
        ])
    return finish(parts, provenance)


def scaling_chart(campaign):
    cases, measurements, provenance = campaign["cases"], campaign["measurements"], campaign["manifest"]["provenance"]
    description = "Mean execution time at 1,000, 10,000 and 100,000 input rows. Both axes use logarithmic scales. "
    description += "Timing whiskers show the observed minimum and maximum. "
    series = []
    for scenario, label, _, color in SCALING:
        points = []
        for size in ("Small", "Medium", "Large"):
            case_id = f"{scenario}-execution-Table-{size}"
            count = cases[case_id]["inputCount"]
            mean = measurements[case_id]["meanNanoseconds"] / 1e6
            assert count in (1000, 10000, 100000) and mean > 0, case_id
            lower, upper = observed_time_range(campaign, case_id)
            if lower == 0:
                raise ValueError(f"Logarithmic scaling requires a positive observed minimum for {case_id}")
            points.append((count, mean, lower / 1e6, upper / 1e6))
        description += label + " averaged " + ", ".join(
            f"{mean:.3f} ms at {count:,} rows ({lower:.3f}–{upper:.3f} ms observed)"
            for count, mean, lower, upper in points
        ) + ". "
        series.append((label, color, points))
    parts = frame("How execution time scales with input size", description, provenance["build"]["engineVersion"])
    parts.append(text(32, 96, "Mean milliseconds | Both axes logarithmic | Whiskers show observed min–max", size=12))
    minimum = math.floor(math.log10(min(lower for _, _, points in series for _, _, lower, _ in points)))
    maximum = math.ceil(math.log10(max(upper for _, _, points in series for _, _, _, upper in points)))
    x = lambda count: 112 + (math.log10(count) - 3) * 288
    y = lambda mean: 438 - (math.log10(mean) - minimum) / (maximum - minimum) * 300
    for power in range(minimum, maximum + 1):
        value = 10 ** power
        parts.extend([line(112, y(value), 688, y(value)), text(96, y(value) + 4, f"{value:g} ms", size=12, anchor="end")])
    for count in (1000, 10000, 100000):
        parts.extend([line(x(count), 138, x(count), 438), text(x(count), 463, f"{count:,}", size=12, anchor="middle")])
    parts.append(text(400, 487, "Input rows", size=12, anchor="middle"))
    for index, (label, color, points) in enumerate(series):
        coordinates = " ".join(f"{x(count):.2f},{y(mean):.2f}" for count, mean, _, _ in points)
        parts.append(f'<polyline points="{coordinates}" fill="none" stroke="{color}" stroke-width="2.5"/>')
        for count, mean, lower, upper in points:
            parts.extend(vertical_whisker(x(count), y(lower), y(upper), color))
            parts.append(
                f'<circle cx="{x(count):.2f}" cy="{y(mean):.2f}" r="4.5" fill="{color}">'
                f'<title>{escape(label)} averaged {mean:.3f} ms at {count:,} rows '
                f'({lower:.3f}–{upper:.3f} ms observed)</title></circle>'
            )
        legend_y = 177 + index * 67
        parts.extend([
            line(734, legend_y - 5, 754, legend_y - 5, color, 3),
            text(766, legend_y, label, size=13, weight="600"),
            text(766, legend_y + 20, f"{points[-1][1]:.2f} ms at 100k", size=12),
        ])
    return finish(parts, provenance)


def load_campaign(directory):
    manifest = read_json(directory / "manifest.json")
    completion = read_json(directory / "complete.json")
    if manifest["schemaVersion"] != "3" or manifest["status"] != "complete":
        raise ValueError(f"Campaign must be complete and schema-v3: {directory}")
    if completion["completed"] != completion["expected"]:
        raise ValueError(f"Incomplete campaign: {directory}")
    return {
        "directory": directory,
        "manifest": manifest,
        "cases": {case["id"]: case for case in manifest["cases"]},
        "measurements": {result["caseId"]: result for result in read_json(directory / "measurements.json")},
    }


def comparable_definition(case):
    definition = case.copy()
    sql = definition["sql"]
    # Historical exports used CRCRLF. Do not normalize whitespace or SQL literals.
    for literal in re.findall(r"'(?:''|[^'])*'", sql):
        if "\r" in literal or "\n" in literal:
            raise ValueError(f"Cannot normalize SQL with multiline literals: {case['id']}")
    definition["sql"] = re.sub(r"\r+\n", "\n", sql).replace("\r", "\n")
    return definition


def validate_history(campaigns):
    if not 2 <= len(campaigns) <= 4:
        raise ValueError("Release panels require two to four ordered campaigns")
    timestamps = [campaign["manifest"]["startedUtc"] for campaign in campaigns]
    if timestamps != sorted(timestamps) or len(set(timestamps)) != len(timestamps):
        raise ValueError("Campaigns must be ordered by their recording time")
    baseline = campaigns[0]
    reference = baseline["manifest"]["provenance"]
    versions = []
    for campaign in campaigns:
        provenance = campaign["manifest"]["provenance"]
        version = provenance["build"]["engineVersion"]
        if version in versions:
            raise ValueError(f"Duplicate engine version: {version}")
        versions.append(version)
        if provenance["hardware"] != reference["hardware"] or provenance["environment"] != reference["environment"]:
            raise ValueError(f"Hardware or environment changed for {version}")
        if provenance["environment"]["mode"] != "Official" or provenance["source"]["dirty"]:
            raise ValueError(f"Require an Official campaign from a clean source tree: {version}")
        for scenario, *_ in TREND_WORKLOADS:
            case_id = f"{scenario}-execution-Table-Large"
            case = campaign["cases"][case_id]
            if comparable_definition(case) != comparable_definition(baseline["cases"][case_id]):
                raise ValueError(f"Workflow definition changed: {version}, {case_id}")
            if case["inputCount"] != 100000 or case["inputUnit"] != "rows":
                raise ValueError(f"Require 100,000 input rows: {case_id}")
            result = campaign["measurements"][case_id]
            if result["status"] != "complete":
                raise ValueError(f"Incomplete measurement: {version}, {case_id}")
            for metric in ("meanNanoseconds", "allocatedBytesPerOperation"):
                value = result[metric]
                if value is None or not math.isfinite(value) or value <= 0:
                    raise ValueError(f"Invalid {metric}: {version}, {case_id}")


def change_style(change):
    if abs(change) < 0.05:
        return "≈0.0%", "#475569"
    if -100 < change < -99.95:
        return f"{change:+.3f}%", "#047857"
    if change > 0:
        return f"{change:+.1f}%", "#b91c1c"
    return f"{change:+.1f}%", "#047857"


def measurement_label(value, unit):
    precision = 2
    if unit in ("s", "GiB") or abs(value) < 0.1:
        precision = 3
    return f"{value:.{precision}f} {unit}"


def trend_values(comparisons, case_id, metric):
    divisor, unit = 1e6, "ms"
    column, change_column = "MeanNanoseconds", "TimeChangePercent"
    if metric == "allocatedBytesPerOperation":
        divisor, unit = 1048576, "MiB"
        column, change_column = "AllocatedBytes", "AllocationChangePercent"
    rows = comparisons[case_id]
    values = [row[column] / divisor for row in rows]
    if metric == "meanNanoseconds" and max(values) >= 1000:
        values, unit = [value / 1000 for value in values], "s"
    if metric == "allocatedBytesPerOperation" and max(values) >= 1024:
        values, unit = [value / 1024 for value in values], "GiB"
    return {"values": values, "unit": unit, "changes": [row[change_column] for row in rows]}


def release_panel(origin, workload, series, versions):
    left, top = origin
    values, unit = series["values"], series["unit"]
    _, label, detail, color = workload
    parts = [
        f'<rect x="{left}" y="{top}" width="{RELEASE_PANEL_WIDTH}" height="{RELEASE_PANEL_HEIGHT}" rx="10" fill="#f8fafc"/>',
        text(left + 16, top + 25, label, size=15, weight="600"),
        text(left + 16, top + 43, detail, size=11),
    ]
    ranges = series.get("ranges")
    maximum = max(upper for _, upper in ranges) * 1.2 if ranges else max(values) * 1.2
    x = lambda index: left + 76 + index * 175 / (len(values) - 1)
    y = lambda value: top + 146 - value / maximum * 85
    for fraction in (0, 0.5, 1):
        value = maximum * fraction
        tick = f"{value:.1f}"
        if maximum < 1:
            tick = f"{value:.3f}"
        parts.extend([
            line(left + 50, y(value), left + 251, y(value)),
            text(left + 35, y(value) + 4, tick, size=10, anchor="end"),
        ])
    points = " ".join(f"{x(index):.2f},{y(value):.2f}" for index, value in enumerate(values))
    parts.append(f'<polyline points="{points}" fill="none" stroke="{color}" stroke-width="2.5"/>')
    for index, (value, version) in enumerate(zip(values, versions)):
        label_y = y(value) - 10
        if ranges:
            lower, upper = ranges[index]
            parts.extend(vertical_whisker(x(index), y(lower), y(upper), color))
            label_y = y(upper) - 8
        parts.extend([
            f'<circle cx="{x(index):.2f}" cy="{y(value):.2f}" r="4" fill="{color}"/>',
            text(x(index), label_y, measurement_label(value, unit), size=11, anchor="middle", weight="600"),
            text(x(index), top + 169, version.partition("-")[0], size=11, anchor="middle"),
        ])
        suffix = version.partition("-")[2]
        if suffix:
            parts.append(text(x(index), top + 183, suffix, size=10, anchor="middle"))
        if index == 0:
            continue
        change = series["changes"][index]
        change_label, change_color = change_style(change)
        midpoint = (x(index - 1) + x(index)) / 2
        parts.extend([
            text(midpoint, top + 210, change_label, size=13, fill=change_color, anchor="middle", weight="600"),
            text(midpoint, top + 227, "vs previous", size=10, anchor="middle"),
        ])
    return parts


def release_chart(campaigns, metric, comparisons):
    title = "Execution time across recorded versions"
    if metric == "allocatedBytesPerOperation":
        title = "Managed allocations across recorded versions"
    rows = math.ceil(len(TREND_WORKLOADS) / RELEASE_COLUMNS)
    height = 194 + rows * (RELEASE_PANEL_HEIGHT + RELEASE_PANEL_GAP)
    versions = [campaign["manifest"]["provenance"]["build"]["engineVersion"] for campaign in campaigns]
    description = "100,000 synthetic input rows per query. Independent zero-based panel scales. Lower is better. "
    timing = metric == "meanNanoseconds"
    if timing:
        description += "Timing whiskers show the observed minimum and maximum. "
    panels = []
    for index, workload in enumerate(TREND_WORKLOADS):
        case_id = f"{workload[0]}-execution-Table-Large"
        series = trend_values(comparisons, case_id, metric)
        values, unit = series["values"], series["unit"]
        if timing:
            divisor = 1e9 if unit == "s" else 1e6
            series["ranges"] = [
                (lower / divisor, upper / divisor)
                for lower, upper in (observed_time_range(campaign, case_id) for campaign in campaigns)
            ]
        description += workload[1] + ": "
        for position, (version, value) in enumerate(zip(versions, values)):
            description += f"{version} = {measurement_label(value, unit)}"
            if timing:
                lower, upper = series["ranges"][position]
                description += f" ({measurement_label(lower, unit)} to {measurement_label(upper, unit)} observed)"
            if position > 0:
                change = series["changes"][position]
                change_label, _ = change_style(change)
                description += f" ({change_label} vs preceding version)"
            description += ". " if timing else "; "
        origin = (
            32 + index % RELEASE_COLUMNS * (RELEASE_PANEL_WIDTH + RELEASE_PANEL_GAP),
            112 + index // RELEASE_COLUMNS * (RELEASE_PANEL_HEIGHT + RELEASE_PANEL_GAP),
        )
        panels.extend(release_panel(origin, workload, series, versions))
    provenance = campaigns[-1]["manifest"]["provenance"]
    parts = frame(title, description, versions[-1], height=height,
                  subtitle="100,000 synthetic input rows | Same workflow definitions, hardware and runtime")
    parts.append(text(32, 91, "Lower is better | Green: decrease; red: increase | Each panel has its own zero-based scale", size=12))
    parts.extend(panels)
    if metric == "allocatedBytesPerOperation":
        parts.append(text(32, height - 71, "Cumulative managed allocations per query; this does not measure peak process memory.", size=12))
    else:
        parts.append(text(32, height - 71, "Dots show means | Whiskers show observed min–max | Percentages compare means", size=12))
    return finish(parts, provenance, height=height)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--run-directory", type=Path, default=DEFAULT_RUN)
    parser.add_argument("--history-directories", type=Path, nargs="+", default=DEFAULT_HISTORY)
    parser.add_argument("--output-directory", type=Path, default=ROOT / "images/benchmarks")
    parser.add_argument("--work-directory", type=Path, help="New evidence directory; defaults under .artifacts/benchmarks/readme-charts")
    args = parser.parse_args()
    campaign = load_campaign(args.run_directory)
    measurements = campaign["measurements"]
    history = [load_campaign(directory) for directory in args.history_directories]
    validate_history(history)
    for case in [f"{scenario}-execution-Table-{size}" for scenario, *_ in WORKLOADS for size in ("Small", "Medium", "Large")]:
        assert measurements[case]["status"] == "complete", case
    comparisons = prepare_comparisons(
        history, [f"{scenario}-execution-Table-Large" for scenario, *_ in TREND_WORKLOADS],
        work_directory=args.work_directory,
    )
    charts = {
        "execution.svg": execution_chart(campaign),
        "scaling.svg": scaling_chart(campaign),
        "release-execution.svg": release_chart(history, "meanNanoseconds", comparisons),
        "release-allocations.svg": release_chart(history, "allocatedBytesPerOperation", comparisons),
    }
    args.output_directory.mkdir(parents=True, exist_ok=True)
    for filename, contents in charts.items():
        destination = args.output_directory / filename
        destination.write_text(contents, encoding="utf-8", newline="\r\n")
        print(destination)


if __name__ == "__main__":
    main()
