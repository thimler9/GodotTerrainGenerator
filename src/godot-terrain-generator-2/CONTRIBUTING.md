# Contributing

## Guidelines

- Prefer minimal, focused changes.
- Preserve compatibility with the project's .NET 6 and .NET 8 targets when making framework-related changes.
- Validate affected projects after editing.
- Keep generated files and IDE-specific files out of source changes unless required.

## SDF Pipeline Conventions

- Place each SDF pipeline stage, shader runner, and shader parameter type under `src/SDFGenerator/<StageName>/`.
- Use the namespace `TerrainGeneration.Application.SDFGenerator.<StageName>` for those types.
- Do not place stage classes in `TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline`; the existing `ConstantStage` placement is incorrect and should not be copied.
- SDF stages must preserve the pipeline's five-buffer binding contract.