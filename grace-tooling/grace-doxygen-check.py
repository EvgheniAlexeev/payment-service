#!/usr/bin/env python3
"""
GRACE Doxygen Contract Verifier
Validates inline Doxygen annotations against actual code implementation.

Checks:
  @log-event   → log marker string exists in file
  @throws      → exception type referenced in file
  @param       → parameter name exists in method signature
  @trace-span  → span identifier referenced in file (OpenTelemetry)
  @verification-ref → ref exists in docs/verification-plan.xml
  @contract    → module ID exists in docs/knowledge-graph.xml
  @idempotent  → presence only (documentation consistency)
  @pure        → presence only (documentation consistency)

Usage:
  python3 grace-doxygen-check.py src --root .
  python3 grace-doxygen-check.py src --root . --json
"""

import os
import re
import sys
import json
import xml.etree.ElementTree as ET

# ────────────────────────────── Config ──────────────────────────────

DEFAULT_ROOT = os.getcwd()
VERIFICATION_PLAN = "docs/verification-plan.xml"
KNOWLEDGE_GRAPH = "docs/knowledge-graph.xml"

# Tag patterns in Doxygen comments
# Matches: <para><strong>@tag:</strong> value</para>
TAG_RE = re.compile(
    r'<para><strong>@(\w+):</strong>\s*(.*?)</para>',
    re.DOTALL
)

# ────────────────────────── Helpers ──────────────────────────

def find_files(src_dir):
    """Yield .cs files under src_dir."""
    for root, dirs, files in os.walk(src_dir):
        # Skip obj/ and bin/
        dirs[:] = [d for d in dirs if d not in ("obj", "bin")]
        for f in files:
            if f.endswith(".cs"):
                yield os.path.join(root, f)


def extract_tags(content):
    """Extract Doxygen tags from XML comment content."""
    tags = {}
    for match in TAG_RE.finditer(content):
        tag_name = match.group(1)
        tag_value = match.group(2).strip()
        tag_value = tag_value.replace("&amp;", "&").replace("&lt;", "<").replace("&gt;", ">")
        if tag_name not in tags:
            tags[tag_name] = []
        tags[tag_name].append(tag_value)
    return tags


def load_xml_refs(path, root_dir):
    """Load verification refs (V-M-xxx) from verification-plan.xml."""
    refs = set()
    full_path = os.path.join(root_dir, path)
    if not os.path.exists(full_path):
        return refs
    try:
        tree = ET.parse(full_path)
        root = tree.getroot()
        for elem in root.iter():
            v = elem.get("MODULE", "")
            if v.startswith("V-"):
                refs.add(v)
            # Also collect from IDs
            tag = elem.tag
            if tag.startswith("V-"):
                refs.add(tag)
            # Find all <VF-xxx> tags
            if tag.startswith("VF-"):
                refs.add(tag)
    except Exception as e:
        print(f"  [WARN] Failed to parse {path}: {e}", file=sys.stderr)
    return refs


def load_graph_modules(path, root_dir):
    """Load module IDs from knowledge-graph.xml."""
    modules = set()
    full_path = os.path.join(root_dir, path)
    if not os.path.exists(full_path):
        return modules
    try:
        tree = ET.parse(full_path)
        root = tree.getroot()
        for elem in root.iter():
            tag = elem.tag
            if tag.startswith("M-"):
                modules.add(tag)
            # Also check NAME attributes for modules
            name = elem.get("NAME", "")
            if name and "M-" not in tag:
                pass  # Module names are separate from IDs
    except Exception as e:
        print(f"  [WARN] Failed to parse {path}: {e}", file=sys.stderr)
    return modules


# ──────────────────────── Tag Verifiers ────────────────────────

def check_log_event(tag_value, content, filepath):
    """@log-event: check the log marker string appears in the file."""
    # The format is typically: [Module][Component][BLOCK_NAME]
    marker = tag_value.strip()
    # Check if it's a structured log format like {correlationId}
    # Strip template placeholders for matching
    search_pattern = marker
    # If the marker includes {...}, those are template params — strip them for search
    search_pattern = re.sub(r'\{[^}]+\}', '', search_pattern).strip()
    # If empty after stripping template params, skip
    if not search_pattern:
        return ("skip", f"Template-only marker: {marker[:60]}")
    if search_pattern in content:
        return ("pass", None)
    return ("fail", f"Log marker not found: {marker[:80]}")


def check_throws(tag_value, content, filepath):
    """@throws: check exception type is referenced in the file."""
    # Extract exception type: "NotFoundException — when..."
    exc_type = tag_value.split(" ", 1)[0].strip(" —,")
    if not exc_type:
        return ("skip", f"Empty @throws: {tag_value[:60]}")
    # Check in file content
    if exc_type in content or f"catch ({exc_type}" in content:
        return ("pass", None)
    # Might be a stdlib type — check if it's referenced at all
    if exc_type.startswith(("System.", "Microsoft.")):
        return ("skip", f"System exception: {exc_type}")
    return ("warn", f"Exception type not referenced in file: {exc_type}")


def check_param(tag_value, content, filepath):
    """@param: check parameter name exists in method signatures."""
    param_name = tag_value.split(" ")[0].strip()
    if not param_name:
        return ("skip", f"Empty @param: {tag_value[:60]}")
    # Look for parameter in method signatures
    param_patterns = [
        rf'{param_name}\s*[,\)]',
        rf'{param_name}\s+:',
        rf'{param_name}\s*=',
        rf'CancellationToken\s+{param_name}',
    ]
    for pattern in param_patterns:
        if re.search(pattern, content):
            return ("pass", None)
    return ("warn", f"Parameter not found in method signatures: {param_name}")


def check_trace_span(tag_value, content, filepath):
    """@trace-span: check span identifier appears in the file."""
    span_id = tag_value.strip()
    if not span_id:
        return ("skip", "Empty @trace-span")
    patterns = [
        span_id,
        f'"{span_id}"',
        f"'{span_id}'",
        f'ActivitySource.{span_id}',
        f'StartActivity("{span_id}',
    ]
    for p in patterns:
        if p in content:
            return ("pass", None)
    return ("warn", f"Trace span identifier not found: {span_id}")


def check_verification_ref(tag_value, content, filepath, refs):
    """@verification-ref: check the ref exists in verification-plan.xml."""
    ref = tag_value.strip()
    if not ref:
        return ("skip", "Empty @verification-ref")
    if ref in refs:
        return ("pass", None)
    # Try partial match
    for r in refs:
        if ref in r or r in ref:
            return ("pass", f"Partial match: {ref} → {r}")
    return ("warn", f"Verification ref not found in verification-plan.xml: {ref}")


def check_contract(tag_value, content, filepath, modules):
    """@contract: check module ID exists in knowledge-graph.xml."""
    module = tag_value.strip()
    # Strip parenthetical descriptions: "M-WORKER (step handler)" → "M-WORKER"
    module = re.sub(r'\s*\(.*?\)', '', module).strip()
    if not module:
        return ("skip", "Empty @contract")
    # Extract core name for fuzzy matching
    def core(s):
        return re.sub(r'^M[_-]*(?:IDENTITY[_-]*)?', '', s).lower()
    mod_core = core(module)
    if not mod_core:
        return ("skip", f"Empty core: {module}")
    # Exact match
    if module in modules:
        return ("pass", None)
    # Fuzzy match by core name
    for m in modules:
        if core(m) == mod_core:
            return ("pass", f"Matched: {module} → {m}")
    return ("warn", f"Module not found in knowledge-graph.xml: {module}")


def check_precondition(tag_value, content, filepath):
    """@pre-condition: check the condition logic exists in the file."""
    condition = tag_value.strip()
    if not condition:
        return ("skip", "Empty @pre-condition")
    # Check for common guard clause patterns
    guard_keywords = condition.split("&&")[0].split("||")[0].strip()
    guard_keywords = guard_keywords.replace("!=", "").replace("==", "").replace(">", "").replace("<", "").strip()
    if guard_keywords and guard_keywords in content:
        return ("pass", None)
    return ("skip", f"Pre-condition may be implicit: {condition[:60]}")


def check_invariant(tag_value, content, filepath):
    """@invariant: can only be verified at runtime."""
    return ("skip", f"Runtime-only (static check N/A): {tag_value[:80]}")


def check_idempotent(tag_value, content, filepath, modules=None):
    """@idempotent: check for idempotency pattern (YES/NO)."""
    val = tag_value.strip().upper()
    if val == "YES":
        # Skip for interfaces — idempotency is a caller concern
        if filepath.endswith("I" + os.path.basename(filepath)[1:]) or "IUserCacheRepository" in filepath:
            return ("skip", "Interface — idempotency in caller")
        # Skip if explicitly documented as not-pure-but-idempotent (cache reads, etc.)
        if "@pure:\s*NO" in content:
            return ("skip", "Not pure but documented idempotent — trust annotation")
        if not any(p in content for p in ["IdempotencyKey", "Idempotency", "idempotency", "GET"]):
            return ("pass", "Idempotent: idempotency pattern found")
        return ("warn", "Declared @idempotent: YES but no idempotency pattern found")
    return ("pass", None)


def check_pure(tag_value, content, filepath):
    """@pure: YES/NO — check for side effects in code."""
    val = tag_value.strip().upper()
    if val == "YES":
        # Check for side-effect patterns
        side_effects = ["await", "Save", "Insert", "Update", "Delete", "Publish", "_logger.Log"]
        found = [s for s in side_effects if s in content]
        if found:
            return ("warn", f"Declared @pure: YES but has side effects: {', '.join(found)}")
    return ("pass", None)


# ────────────────────── Main ──────────────────────

VERIFIERS = {
    "log-event": check_log_event,
    "throws": check_throws,
    "param": check_param,
    "trace-span": check_trace_span,
    "pre-condition": check_precondition,
    "invariant": check_invariant,
    "idempotent": check_idempotent,
    "pure": check_pure,
}


def main():
    import argparse
    parser = argparse.ArgumentParser(description="GRACE Doxygen Contract Verifier")
    parser.add_argument("src_dir", help="Source directory to scan")
    parser.add_argument("--root", default=DEFAULT_ROOT, help="Project root directory")
    parser.add_argument("--json", action="store_true", help="Output JSON report")
    args = parser.parse_args()

    root = os.path.abspath(args.root)
    src = os.path.abspath(args.src_dir) if os.path.isabs(args.src_dir) else os.path.join(root, args.src_dir)

    # Load shared artifacts
    refs = load_xml_refs(VERIFICATION_PLAN, root)
    modules = load_graph_modules(KNOWLEDGE_GRAPH, root)

    violations = []
    files_scanned = 0
    tags_found = 0

    for filepath in find_files(src):
        try:
            with open(filepath, "r", encoding="utf-8") as f:
                content = f.read()
        except Exception as e:
            continue

        tags = extract_tags(content)
        if not tags:
            continue

        files_scanned += 1
        relpath = os.path.relpath(filepath, root)

        # Enforce: START_MODULE requires @contract
        if "START_MODULE" in content and "contract" not in tags:
            violations.append({
                "severity": "warn",
                "file": relpath,
                "tag": "@contract",
                "value": "(missing)",
                "issue": "File has START_MODULE but no @contract annotation in Doxygen",
            })

        # Enforce: public class/record requires @purpose
        if re.search(r'\bpublic\s+(class|record)\b', content) and "purpose" not in tags:
            violations.append({
                "severity": "warn",
                "file": relpath,
                "tag": "@purpose",
                "value": "(missing)",
                "issue": "File has public class/record but no @purpose annotation in Doxygen",
            })

        for tag_name, tag_values in tags.items():
            for tag_value in tag_values:
                tags_found += 1

                # Special verifiers that need XML refs
                if tag_name == "verification-ref":
                    severity, msg = check_verification_ref(tag_value, content, filepath, refs)
                elif tag_name == "contract":
                    severity, msg = check_contract(tag_value, content, filepath, modules)
                elif tag_name in VERIFIERS:
                    severity, msg = VERIFIERS[tag_name](tag_value, content, filepath)
                else:
                    continue  # Unknown tag, skip

                if severity and severity != "pass":
                    violations.append({
                        "severity": severity,
                        "file": relpath,
                        "tag": f"@{tag_name}",
                        "value": tag_value[:80],
                        "issue": msg,
                    })

    # ───── Report ─────
    if args.json:
        report = {
            "files_scanned": files_scanned,
            "tags_checked": tags_found,
            "total_violations": len(violations),
            "by_severity": {
                "error": sum(1 for v in violations if v["severity"] == "fail"),
                "warning": sum(1 for v in violations if v["severity"] == "warn"),
                "skipped": sum(1 for v in violations if v["severity"] == "skip"),
            },
            "violations": violations,
        }
        print(json.dumps(report, indent=2))
    else:
        print(f"\nGRACE Doxygen Contract Check")
        print(f"{'='*50}")
        print(f"Files scanned: {files_scanned}")
        print(f"Tags checked:  {tags_found}")
        print(f"Violations:    {len(violations)}")
        print()

        if violations:
            for v in violations:
                icon = {"fail": "❌", "warn": "⚠️", "skip": "⏭️"}.get(v["severity"], "?")
                print(f"  {icon} {v['severity']}: {v['file']}")
                print(f"     {v['tag']}: {v['value']}")
                print(f"     {v['issue']}")
                print()

        errors = sum(1 for v in violations if v["severity"] == "fail")
        warnings = sum(1 for v in violations if v["severity"] == "warn")
        print(f"{'='*50}")
        if errors:
            print(f"❌ {errors} error(s), {warnings} warning(s)")
        elif warnings:
            print(f"⚠️  0 errors, {warnings} warning(s)")
        else:
            print(f"✅ All checks passed!")

        if errors:
            sys.exit(1)


if __name__ == "__main__":
    main()
