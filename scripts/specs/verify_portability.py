"""Validate the portable specification boundary, links, anchors and code fences."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
SPECS = (
    "musoq-core-language-spec.md",
    "musoq-table-couple-spec.md",
    "musoq-binary-text-spec.md",
)
HOST_TERMS = re.compile(
    r"C#|\.NET|\bCLR\b|\bSystem\.\w+|IEnumerable|IReadOnlyList|IComparable|"
    r"\b(?:GetRawConstructors|StructuralInputConstructorAttribute|SchemaTableMetadata|"
    r"CompileWithDiagnostics|CompileForExecution|CompiledQuery|WindowFunctionNode|"
    r"DiagnosticDescriptorRegistry|ErrorCatalog|ErrorMetadataCatalog|InjectGroup|InjectSpecificSource)\b"
)


def anchor(heading):
    heading = re.sub(r"[`*_]", "", heading.strip().lower())
    heading = re.sub(r"[^\w\- ]", "", heading)
    return heading.replace(" ", "-")


def anchors(text):
    return {anchor(match.group(1)) for match in re.finditer(r"^#{1,6} (.+)$", text, re.M)} | set(re.findall(r'<a id="([^"]+)"', text))


def check_links(path, text, errors):
    for link in re.findall(r"\]\(([^)]+)\)", text):
        if re.match(r"https?://", link):
            continue
        target, _, fragment = link.partition("#")
        destination = (path.parent / target).resolve() if target else path
        if not destination.is_file():
            errors.append(f"{path.name}: missing link {link}")
            continue
        if fragment and fragment not in anchors(destination.read_text(encoding="utf-8")):
            errors.append(f"{path.name}: missing anchor {link}")


def check_spec(path, text, errors):
    for number, line in enumerate(text.splitlines(), 1):
        if HOST_TERMS.search(line):
            errors.append(f"{path.name}:{number}: host-specific language: {line.strip()}")
    if text.count("```") % 2:
        errors.append(f"{path.name}: unclosed code fence")
    check_links(path, text, errors)


def check_specs(errors):
    for name in SPECS:
        path = ROOT / "specs" / name
        check_spec(path, path.read_text(encoding="utf-8"), errors)


def main():
    errors = []
    check_specs(errors)
    for error in errors:
        print(error)
    print(f"Specifications: {len(SPECS)} checked; {len(errors)} validation errors")
    return bool(errors)


if __name__ == "__main__":
    sys.exit(main())
