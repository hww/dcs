#!/usr/bin/env python3
"""
Flatten namespaces per top-level module.

Maps every .cs file under DCS/<Module>/... to `namespace DCS.<Module>`.
Also fixes using directives where possible.

Usage:
    python3 fix_ns.py            # dry-run, shows what would change
    python3 fix_ns.py --apply    # actually writes files
"""

import os
import re
import sys
import argparse

# Top-level module folders under DCS/
MODULES = {
    "Actors": "DCS.Actors",
    "Authoring": "DCS.Authoring",
    "Baking": "DCS.Baking",
    "Core": "DCS.Core",
    "Data": "DCS.Data",
    "Editor": "DCS.Editor",
    "Examples": "DCS.Examples",
    "Lua": "DCS.Lua",
    "Navigation": "DCS.Navigation",
    "Spatial": "DCS.Spatial",
    "Tests": "DCS.Tests",
    "UnityUtils": "DCS.UnityUtils",
    "World": "DCS.World",
}

# Old namespaces -> new namespaces.
# Order matters: longer/more specific first.
NS_MAP = [
    # Specific old sub-namespaces
    (re.compile(r'^namespace DCS\.Core\.Packing\s*$'), 'namespace DCS.Baking'),
    (re.compile(r'^namespace DCS\.Gameplay\.Build\s*$'), 'namespace DCS.Baking'),
    (re.compile(r'^namespace DCS\.Gameplay\.Runtime\s*$'), 'namespace DCS.World'),
    (re.compile(r'^namespace DCS\.Gameplay\.Streaming\s*$'), 'namespace DCS.World'),
    (re.compile(r'^namespace DCS\.Interaction\.Authoring\s*$'), 'namespace DCS.Authoring'),
    (re.compile(r'^namespace DCS\.Lua\.Bindings\s*$'), 'namespace DCS.Lua'),
    (re.compile(r'^namespace DCS\.Navigation\.Authoring\s*$'), 'namespace DCS.Navigation'),
    (re.compile(r'^namespace DCS\.Spatial\.\w+\s*$'), 'namespace DCS.Spatial'),
    (re.compile(r'^namespace DCS\.EditorTools\s*$'), 'namespace DCS.Editor'),
]

# using-directive replacements (old -> new).
USING_MAP = [
    ('using DCS.Core.Packing;', 'using DCS.Baking;'),
    ('using DCS.Gameplay.Build;', 'using DCS.Baking;'),
    ('using DCS.Gameplay.Runtime;', 'using DCS.World;'),
    ('using DCS.Gameplay.Streaming;', 'using DCS.World;'),
    ('using DCS.Interaction.Authoring;', 'using DCS.Authoring;'),
    ('using DCS.Lua.Bindings;', 'using DCS.Lua;'),
    ('using DCS.Navigation.Authoring;', 'using DCS.Navigation;'),
    ('using DCS.EditorTools;', 'using DCS.Editor;'),
]


def file_module(path: str) -> str | None:
    """Return target namespace for a file, based on its top-level module folder."""
    parts = path.replace('\\', '/').split('/')
    if len(parts) < 2 or parts[0] != 'DCS':
        return None
    module = parts[1]
    if module == 'Plugins':
        return None
    return MODULES.get(module)


def fix_file(path: str, target_ns: str, apply: bool) -> bool:
    """Rewrite namespace and using lines in a file. Returns True if changed."""
    try:
        with open(path, 'r', encoding='utf-8') as f:
            content = f.read()
    except UnicodeDecodeError:
        with open(path, 'r', encoding='utf-8-sig') as f:
            content = f.read()

    original = content
    lines = content.split('\n')
    changed = False

    for i, line in enumerate(lines):
        stripped = line.rstrip('\r')

        # --- namespace lines ---
        if stripped.startswith('namespace '):
            # Specific mappings first
            matched = False
            for pattern, repl in NS_MAP:
                if pattern.match(stripped):
                    lines[i] = repl + ('\r' if line.endswith('\r') else '')
                    changed = True
                    matched = True
                    break
            if not matched:
                # If it's a top-level old namespace that should just go
                # to the module namespace, and doesn't already match, force it.
                if not stripped.startswith(f'namespace {target_ns}'):
                    # Only rewrite if it's one of the old roots
                    if re.match(r'^namespace DCS\.(Core|Gameplay|Interaction|Lua|Navigation|Spatial)(\.\w+)*\s*$', stripped):
                        lines[i] = f'namespace {target_ns}' + ('\r' if line.endswith('\r') else '')
                        changed = True
            continue

        # --- using lines ---
        for old, new in USING_MAP:
            if stripped.strip() == old:
                lines[i] = line.replace(old, new)
                changed = True
                break

    if changed and apply:
        new_content = '\n'.join(lines)
        with open(path, 'w', encoding='utf-8') as f:
            f.write(new_content)
    return changed


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--apply', action='store_true', help='Write changes')
    ap.add_argument('--root', default='DCS', help='Root folder')
    args = ap.parse_args()

    total = 0
    changed = 0
    for dirpath, _, filenames in os.walk(args.root):
        for fname in filenames:
            if not fname.endswith('.cs'):
                continue
            path = os.path.join(dirpath, fname)
            total += 1
            target = file_module(path)
            if target is None:
                continue
            if fix_file(path, target, args.apply):
                changed += 1
                print(f'{"WROTE" if args.apply else "WOULD CHANGE"}: {path}')

    print()
    print(f'Total .cs files scanned: {total}')
    print(f'Files affected: {changed}')
    if not args.apply:
        print('Dry-run. Re-run with --apply to write.')


if __name__ == '__main__':
    main()

