#!/bin/bash
set -e

# Actors/Components — старые DCS.Gameplay
sed -i 's|^namespace DCS\.Gameplay$|namespace DCS.Actors|' \
    Actors/Components/CombatRoleComponent.cs \
    Actors/Components/CombatRoles.cs

# Actors — всё старое DCS.Core должно стать DCS.Actors
find Actors/ -name "*.cs" -exec sed -i \
    -e 's|^namespace DCS\.Core$|namespace DCS.Actors|' {} +

# Authoring — старые DCS.Interaction.Authoring и DCS.Gameplay
find Authoring/ -name "*.cs" -exec sed -i \
    -e 's|^namespace DCS\.Interaction\.Authoring$|namespace DCS.Authoring|' \
    -e 's|^namespace DCS\.Gameplay$|namespace DCS.Authoring|' {} +

# Baking — старые DCS.Gameplay.Build и DCS.Core.Packing
find Baking/ -name "*.cs" -exec sed -i \
    -e 's|^namespace DCS\.Gameplay\.Build$|namespace DCS.Baking|' \
    -e 's|^namespace DCS\.Core\.Packing$|namespace DCS.Baking|' {} +

# Spatial — там свои
find Spatial/ -name "*.cs" -exec sed -i \
    -e 's|^namespace DCS\.Spatial\..*$|namespace DCS.Spatial|' {} +

# World — там свои
find World/ -name "*.cs" -exec sed -i \
    -e 's|^namespace DCS\.Gameplay$|namespace DCS.World|' \
    -e 's|^namespace DCS\.Gameplay\.Runtime$|namespace DCS.World|' \
    -e 's|^namespace DCS\.Gameplay\.Streaming$|namespace DCS.World|' \
    -e 's|^namespace DCS\.Core$|namespace DCS.World|' {} +

# Data — там свои
find Data/ -name "*.cs" -exec sed -i \
    -e 's|^namespace DCS\.Gameplay$|namespace DCS.Data|' {} +

# Navigation
find Navigation/ -name "*.cs" -exec sed -i \
    -e 's|^namespace DCS\.Navigation\..*$|namespace DCS.Navigation|' {} +

# UnityUtils
find UnityUtils/ -name "*.cs" -exec sed -i \
    -e 's|^namespace DCS\.Core$|namespace DCS.UnityUtils|' {} +

# Editor
find Editor/ -name "*.cs" -exec sed -i \
    -e 's|^namespace DCS\.EditorTools$|namespace DCS.Editor|' \
    -e 's|^namespace DCS\.Core$|namespace DCS.Editor|' {} +

echo "=== Done ==="
find DCS/ -name "*.cs" -exec grep -H "^namespace " {} + | \
    awk -F'namespace ' '{print $2}' | sed 's/[ \t;{].*//' | sort | uniq -c

