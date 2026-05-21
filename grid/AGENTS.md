# AGENTS.md

Purpose: quick operating guide for future coding agents working in this Godot C# project.

## Project Description
- This is a tile-based procedural world prototype built with Godot 4 + C#.
- The world is generated from noise (elevation, moisture, temperature) plus mountain shaping.
- Tile terrain (biome + forest) and resource spawning are data-driven through settings.
- Camera input selects tiles and drives UI updates.

## Core Files
- `World.cs`: world generation, exported tuning settings, tool buttons, tile selection.
- `Tile.cs`: tile data + visuals + resource markers.
- `TerrainRules.cs`: biome/forest rule evaluation and settings structs.
- `Camera2d.cs`: movement, zoom, and click-to-select.
- `UI.cs`: tile details panel.
- `tile.tscn`, `world.tscn`: scene roots and script bindings.

## How Generation Works
1. `World._Ready()` initializes UI.
2. Runtime calls `RebuildWorld()` (editor uses tool buttons).
3. Generation builds settings objects from exported properties.
4. Seeded RNG configures all noise generators and resource RNG.
5. Grid is generated tile-by-tile; each tile initializes terrain and then resources.

## Agent Editing Instructions
- Keep behavior deterministic when `Seed` is unchanged.
- Prefer extending the existing settings objects over adding hardcoded thresholds.
- Keep `World` as the single source of generation config; pass settings downward.
- Preserve `TrySelectTileAtWorld` contract (returns false on out-of-bounds and clears UI/selection).
- If changing grid dimensions logic, keep selection and coordinate conversion in sync.

## Godot Tooling Gotchas (Important)
- `World.cs` is a `[Tool]` script and uses inspector tool buttons.
- `Tile.cs` must also stay `[Tool]`; otherwise `tile.tscn` may instantiate as `Node2D` in-editor and fail cast to `Tile`.
- `[ExportToolButton]` must be an expression-bodied property in Godot C#.
- The exported tool button should return a valid callable, e.g. `new Callable(this, MethodName.YourMethod)`.

## Validation Checklist After Changes
- Run a compile check (no errors in `World.cs`, `Tile.cs`, `TerrainRules.cs`, `Camera2d.cs`).
- Verify `Rebuild World` tool button works in editor without exceptions.
- Verify `Reroll Seed` changes map layout and still rebuilds successfully.
- Verify tile selection still updates UI and out-of-bounds click clears selection.

## Style and Safety
- Keep changes small and localized.
- Avoid changing scene root node types unless explicitly required.
- Avoid introducing project-wide nullable changes unless intentionally migrating all scripts.
- Preserve current public method names/signatures unless you also update all call sites.

## Scene Safety Rules
- Never change root node type in `world.tscn` or `tile.tscn` without explicit request.
- If script paths or UIDs change, verify scene bindings still resolve.
- Avoid renaming node paths used by code (`HUD/UI`, `Sprite2D`) unless all call sites are updated.

## Determinism Contract
- Same `Seed` + same exported settings must produce the same terrain and resource layout.
- Any new RNG usage must come from the seeded generation RNG, not `Randomize()`.
- Document any intentionally nondeterministic behavior in this file.

## Rebuild Lifecycle Contract
- Rebuild must clear generated tiles before creating replacements.
- Out-of-bounds selection must clear tile visual selection and hide UI panel.
- Editor rebuild paths should avoid runtime-only assumptions.

## Inspector UX Conventions
- Keep export groups ordered as `Generation`, `Biome Thresholds`, `Forest Thresholds`, `Resource Spawning`.
- Keep threshold export ranges and step sizes consistent.
- New tool buttons must remain expression-bodied properties and return a valid `Callable`.

## Testing and Validation Matrix
- Compile checks: `World.cs`, `Tile.cs`, `TerrainRules.cs`, `Camera2d.cs`, `UI.cs`.
- Manual checks: `Rebuild World` button, `Reroll Seed` button, tile selection, out-of-bounds deselection, and UI updates.
- Manual checks should include at least one small grid and one large grid.

## Performance Budget Notes
- Avoid per-frame allocations in camera input and movement paths.
- Keep generation-time complexity proportional to `GridWidth * GridHeight`.
- Avoid repeated expensive node lookups inside tile-generation loops.

## Code Change Policy
- Prefer extending settings structs/rules over adding hardcoded thresholds.
- Keep public method signatures stable unless all call sites are updated in the same change.
- If changing coordinate conversion, update both world-to-grid and grid-to-world logic together.
