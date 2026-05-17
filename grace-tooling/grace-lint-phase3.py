#!/usr/bin/env python3
"""
GRACE Phase-3 Linter — Validates Doxygen annotations, semantic block markup,
and file header contracts in C# source files.

Usage:
    python3 grace-lint-phase3.py <src-dir> --output json|text
    python3 grace-lint-phase3.py src --output json
    python3 grace-lint-phase3.py src --output text

Phase-3 Lean Contract Format rules:
    - 4-tag module header: @contract, @purpose, @invariant, @verification-ref
    - Method-level: @contract-action, @param, @return, @throws (optional: @log-event, @trace-span)
    - Every // START_BLOCK_XXX needs // END_BLOCK_XXX
    - FILE: VERSION: MODULE: PURPOSE: comment headers required
"""

import argparse
import json
import os
import re
import sys
from pathlib import Path
from typing import List, Dict, Tuple

# ---- Configuration ----

SKIP_DIRS = frozenset({"bin", "obj", ".vs", ".vscode"})
SKIP_FILES_PATTERNS = (
    re.compile(r"\.AssemblyInfo\.cs$"),
    re.compile(r"\.GlobalUsings\.g\.cs$"),
    re.compile(r"\.csproj\."),
    re.compile(r"^obj/"),
    re.compile(r"AssemblyAttributes\.cs$"),
)

# Valid module ID patterns (from grace-init-config.json)
MODULE_ID_PATTERN = re.compile(r"^M-[A-Z][A-Z0-9]*(-[A-Z][A-Z0-9]*)*$")
VERIFICATION_REF_PATTERN = re.compile(r"^V-M-[A-Z][A-Z0-9]*(-[A-Z][A-Z0-9]*)*$")
VERSION_PATTERN = re.compile(r"^\d+\.\d+\.\d+$")

# Required file header fields
REQUIRED_HEADER_FIELDS = {"FILE", "VERSION", "MODULE", "PURPOSE"}

# Phase-3 required module-level Doxygen tags
REQUIRED_CLASS_TAGS = {"@contract", "@purpose", "@invariant", "@verification-ref"}

# ---- Violation Model ----


class Violation:
    def __init__(self, severity: str, vtype: str, file: str, line: int, issue: str):
        self.severity = severity
        self.type = vtype
        self.file = file
        self.line = line
        self.issue = issue

    def to_dict(self) -> Dict:
        return {
            "severity": self.severity,
            "type": self.type,
            "file": self.file,
            "line": self.line,
            "issue": self.issue,
        }

    def __str__(self) -> str:
        return f"  [{self.severity}] {self.type}\n    File: {self.file}:{self.line}\n    Issue: {self.issue}\n"


# ---- Helpers ----


def should_skip(filepath: str) -> bool:
    """Check if a file should be skipped (auto-generated or non-source)."""
    name = Path(filepath).name
    for pattern in SKIP_FILES_PATTERNS:
        if pattern.search(name):
            return True
    return False


def walk_source_files(src_dir: str) -> List[str]:
    """Walk the source directory and return all .cs files."""
    files = []
    for root, dirs, fnames in os.walk(src_dir):
        # Skip excluded directories in-place
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS]
        for fname in fnames:
            if should_skip(os.path.join(root, fname)):
                continue
            if fname.endswith(".cs"):
                files.append(os.path.join(root, fname))
    return sorted(files)


def make_relative(filepath: str, src_dir: str) -> str:
    """Make a file path relative to the src directory."""
    try:
        return os.path.relpath(filepath, src_dir)
    except ValueError:
        return filepath


# ---- Semantic Block Validator ----


def validate_blocks(lines: List[str], filepath: str, src_dir: str) -> List[Violation]:
    """
    Validate that every // START_BLOCK_XXX has a matching // END_BLOCK_XXX.
    Uses a stack to detect nesting and mismatched close tags.
    """
    violations = []
    block_stack = []  # list of (block_name, line_number)

    START_RE = re.compile(r"//\s*START_BLOCK_(\w+)")
    END_RE = re.compile(r"//\s*END_BLOCK_(\w+)")

    for i, line in enumerate(lines, 1):
        sm = START_RE.search(line)
        if sm:
            block_stack.append((sm.group(1), i))

        em = END_RE.search(line)
        if em:
            name = em.group(1)
            if not block_stack:
                violations.append(Violation(
                    "error", "orphan-end-block", filepath, i,
                    f"END_BLOCK_{name} without matching START_BLOCK"
                ))
            else:
                last_start, start_line = block_stack.pop()
                if last_start != name:
                    violations.append(Violation(
                        "error", "block-name-mismatch", filepath, i,
                        f"END_BLOCK_{name} does not match START_BLOCK_{last_start} (line {start_line})"
                    ))

    # Unclosed blocks
    for name, line in block_stack:
        violations.append(Violation(
            "error", "unclosed-block", filepath, line,
            f"START_BLOCK_{name} has no matching END_BLOCK"
        ))

    return violations


# ---- File Header Validator ----


def validate_file_header(lines: List[str], filepath: str, src_dir: str) -> List[Violation]:
    """
    Check that the file has a GRACE header with FILE, VERSION, MODULE, PURPOSE.
    The header is expected in the first 10 lines.
    """
    violations = []
    found_fields = set()
    header_line_refs: Dict[str, int] = {}

    HEADER_RE = re.compile(r"//\s*(FILE|VERSION|MODULE|PURPOSE):\s*(.*)")

    for i in range(min(20, len(lines))):
        m = HEADER_RE.search(lines[i])
        if m:
            found_fields.add(m.group(1))
            header_line_refs[m.group(1)] = i + 1

    missing = REQUIRED_HEADER_FIELDS - found_fields
    for field in sorted(missing):
        violations.append(Violation(
            "warning", "missing-header-field", filepath, 1,
            f"Missing // {field}: in file header (first 20 lines)"
        ))

    # Validate VERSION format
    if "VERSION" in found_fields:
        line_no = header_line_refs.get("VERSION", 1)
        line_text = lines[line_no - 1] if line_no <= len(lines) else ""
        vm = re.search(r"//\s*VERSION:\s*(\S+)", line_text)
        if vm and not VERSION_PATTERN.match(vm.group(1)):
            violations.append(Violation(
                "warning", "invalid-version-format", filepath, line_no,
                f"VERSION should be semver (e.g. 1.0.0), got '{vm.group(1)}'"
            ))

    return violations


# ---- Doxygen Annotation Validator ----


def validate_doxygen(lines: List[str], filepath: str, src_dir: str) -> List[Violation]:
    """
    Validate Doxygen annotations for Phase-3 Lean Contract Format.
    Works on both /// XML-style and /** javadoc-style comments.
    Extracts tags from <para><strong>@tagname:</strong> value</para> patterns.
    """
    violations = []
    rel_path = make_relative(filepath, src_dir)

    # Pattern: <para><strong>@tag:</strong> value</para>
    TAG_PATTERN = re.compile(
        r"<para>\s*<strong>\s*@(\w[\w-]*)\s*:\s*</strong>\s*(.*?)</para>",
        re.DOTALL
    )

    i = 0
    while i < len(lines):
        line = lines[i]

        # Detect start of a Doxygen block
        if "/// <summary>" in line:
            start_idx = i
            block_end = None

            # Collect until </remarks> for class-level, or </summary> for simple blocks
            for j in range(i + 1, min(i + 200, len(lines))):
                if "/// </remarks>" in lines[j] or "</remarks>" in lines[j]:
                    block_end = j
                    break

            if block_end is not None:
                # This Doxygen has <remarks> — class/interface/method level
                # Extract all tags from lines i..block_end
                block_text = "".join(lines[start_idx:block_end + 1])
                tags = {}
                for m in TAG_PATTERN.finditer(block_text):
                    tags[m.group(1)] = m.group(2).strip()

                has_class_tags = (
                    "@contract" in tags or
                    "@purpose" in tags or
                    "@verification-ref" in tags or
                    "@invariant" in tags
                )
                HAS_METHOD_TAGS = {"@contract-action", "@param", "@return", "@throws"}
                has_method_tags = bool(HAS_METHOD_TAGS & set(tags.keys()))

                if has_class_tags:
                    # Class-level: require all 4 Phase-3 tags
                    missing = REQUIRED_CLASS_TAGS - set(tags.keys())
                    for tag in sorted(missing):
                        violations.append(Violation(
                            "error", "missing-doxygen-tag", rel_path, i + 1,
                            f"Missing required Phase-3 tag @{tag} in class-level Doxygen"
                        ))

                    # Validate @contract format
                    if "@contract" in tags:
                        cv = tags["@contract"].strip()
                        if not MODULE_ID_PATTERN.match(cv):
                            violations.append(Violation(
                                "warning", "invalid-contract-id", rel_path, i + 1,
                                f"@contract '{cv}' doesn't match M-XXX pattern"
                            ))

                    # Validate @verification-ref format
                    if "@verification-ref" in tags:
                        vv = tags["@verification-ref"].strip()
                        if not VERIFICATION_REF_PATTERN.match(vv):
                            violations.append(Violation(
                                "warning", "invalid-verification-ref", rel_path, i + 1,
                                f"@verification-ref '{vv}' doesn't match V-M-XXX pattern"
                            ))

                elif has_method_tags:
                    # Method-level: no class-level tag requirement, skip
                    pass

            i = start_idx + 1
            continue

        # Also detect javadoc-style blocks
        if "/**" in line and "///" not in line:
            for j in range(i, min(i + 200, len(lines))):
                if "*/" in lines[j]:
                    i = j + 1
                    break
            else:
                i += 1
            continue

        i += 1

    return violations


# ---- Main Lint Orchestrator ----


def lint_file(filepath: str, src_dir: str) -> List[Violation]:
    """Run all lint checks on a single file."""
    try:
        with open(filepath, "r", encoding="utf-8") as f:
            lines = f.readlines()
    except (UnicodeDecodeError, IOError) as e:
        return [Violation("error", "read-error", filepath, 0, f"Could not read file: {e}")]

    violations = []

    # 1. File header validation
    violations.extend(validate_file_header(lines, filepath, src_dir))

    # 2. Semantic block pairing
    violations.extend(validate_blocks(lines, filepath, src_dir))

    # 3. Doxygen annotation validation (Phase-3)
    violations.extend(validate_doxygen(lines, filepath, src_dir))

    return violations


def lint_src_dir(src_dir: str, output_format: str = "json") -> Dict:
    """Lint all .cs files in src_dir."""
    files = walk_source_files(src_dir)
    all_violations: List[Violation] = []

    for filepath in files:
        rel_path = make_relative(filepath, src_dir)
        try:
            violations = lint_file(filepath, src_dir)
            all_violations.extend(violations)
        except Exception as e:
            all_violations.append(Violation(
                "error", "linter-crash", rel_path, 0,
                f"Linter crashed: {e}"
            ))

    # Sort by severity then file
    severity_order = {"error": 0, "warning": 1, "info": 2}
    all_violations.sort(key=lambda v: (severity_order.get(v.severity, 99), v.file, v.line))

    result = {
        "total_violations": len(all_violations),
        "by_severity": {
            "error": sum(1 for v in all_violations if v.severity == "error"),
            "warning": sum(1 for v in all_violations if v.severity == "warning"),
            "info": sum(1 for v in all_violations if v.severity == "info"),
        },
        "files_scanned": len(files),
        "violations": [v.to_dict() for v in all_violations],
    }

    return result


# ---- Output Formatters ----


def print_text_report(result: Dict):
    """Print a human-readable text report."""
    total = result["total_violations"]
    by_severity = result["by_severity"]
    files_scanned = result["files_scanned"]

    print(f"\n{'='*60}")
    print(f"  GRACE Phase-3 Lint Report")
    print(f"{'='*60}")
    print(f"  Files scanned: {files_scanned}")
    print(f"  Total violations: {total}")
    print(f"    Errors:   {by_severity['error']}")
    print(f"    Warnings: {by_severity['warning']}")
    print(f"    Info:     {by_severity['info']}")
    print(f"{'='*60}")

    if not result["violations"]:
        print(f"\n  ✅ All Phase-3 checks passed.\n")
        return

    for v in result["violations"]:
        print(f"\n  [{v['severity']}] {v['type']}")
        print(f"    File: {v['file']}:{v['line']}")
        print(f"    Issue: {v['issue']}")

    print(f"\n{'='*60}")
    print(f"  Total: {total} violations ({by_severity['error']} errors, {by_severity['warning']} warnings)")
    print(f"{'='*60}\n")


def main():
    parser = argparse.ArgumentParser(description="GRACE Phase-3 Linter")
    parser.add_argument("src_dir", help="Source directory to lint (e.g., 'src')")
    parser.add_argument("--output", choices=["json", "text"], default="json",
                        help="Output format (default: json)")
    parser.add_argument("--json-path", default="/tmp/grace_violations.json",
                        help="Path for JSON output (default: /tmp/grace_violations.json)")

    args = parser.parse_args()

    if not os.path.isdir(args.src_dir):
        print(f"Error: '{args.src_dir}' is not a valid directory", file=sys.stderr)
        sys.exit(1)

    result = lint_src_dir(args.src_dir, args.output)

    if args.output == "json":
        with open(args.json_path, "w") as f:
            json.dump(result, f, indent=2)
        # Also print a summary to stdout
        total = result["total_violations"]
        errors = result["by_severity"]["error"]
        warnings = result["by_severity"]["warning"]
        files = result["files_scanned"]
        print(f"GRACE Phase-3 lint: {files} files scanned, {total} violations "
              f"({errors} errors, {warnings} warnings)")
        print(f"JSON report written to {args.json_path}")
    else:
        print_text_report(result)

    # Exit with error code if any violations found
    if result["total_violations"] > 0:
        sys.exit(1)


if __name__ == "__main__":
    main()
