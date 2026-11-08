---
name: create-sdf-pipeline-stage
description: Create and register a complete SDF pipeline stage from a natural-language description.
---

# Create SDF Pipeline Stage

Create a complete SDF pipeline stage that matches the repository's existing `Constant` and `SimplexNoise` implementations.

## Required workflow

### 1. Gather the stage description

Ask the user to describe the stage in natural language. The description should explain:

- What the stage does.
- The stage name or intended function.
- Its input parameters.
- How it transforms the SDF, biome, temperature, or output values.
- Whether it needs helper GLSL functions.

Do not generate files until the inferred design has been confirmed.

### 2. Infer the implementation

Infer and display the following before editing:

- PascalCase stage name.
- JSON function name.
- C# property names and types.
- GLSL parameter names and types.
- C# explicit-layout parameter struct offsets and sizes.
- Shader calculation.
- Required GLSL includes.
- Shader path.
- Parser registration.
- Runtime shader registration.
- Sample pipeline JSON.

Use these conventions:
src/SDFGenerator/<StageName>/ ├── <StageName>Stage.cs ├── <StageName>ShaderParameters.cs └── <StageName>Shader.cs
src/godot-terrain-generator-2/Shaders/Compute/ └── <stage_name>.glsl

Use this namespace for every generated C# type:
TerrainGeneration.Application.SDFGenerator.<StageName>

Do not place new stage classes in:
TerrainGeneration.Application.SDFGenerator.Abstractions.Pipeline


The existing `ConstantStage` placement is a legacy error and must not be copied.

### 3. Grill the design

Before asking for confirmation, check:

- Is the function name unique?
- Are all inferred parameter types representable in both C# and GLSL?
- Are C# struct alignment and GLSL uniform-buffer alignment compatible?
- Does the stage preserve the SDF pipeline's five-buffer contract?
- Does the shader read and write the correct buffer for its intended use?
- Is the stage valid in both temperature and biome pipelines?
- Are all JSON property names deserializable by `System.Text.Json`?
- Does the stage require a helper include?
- Does the shader dispatch use the existing `8 x 8 x 8` local workgroup?
- Is the output indexing consistent with the existing stages?
- Are the parser and runtime registration changes complete?
- Does it follow godot conventions?

Ask one clarification question at a time when the design is ambiguous.

### 4. Request confirmation

Present the inferred design and ask the user to confirm it.

Do not create or modify files until the user confirms.

### 5. Generate the stage

After confirmation, generate these files:

- `<StageName>Stage.cs`
- `<StageName>ShaderParameters.cs`
- `<StageName>Shader.cs`
- `<stage_name>.glsl`

The stage class must:

- Implement `ISDFPipelineStage`.
- Declare a `public const string FunctionName`.
- Return that constant through `Function`.
- Expose JSON-deserializable properties.
- Return the matching shader parameter struct from `CreateShaderParameters()`.

The shader parameter struct must:

- Implement `IShaderParameters`.
- Use `[StructLayout(LayoutKind.Explicit)]`.
- Use explicit field offsets.
- Match the GLSL parameter layout.
- Provide a constructor from `<StageName>Stage`.
- Follow the existing `ConstantShaderParameters` and `SimplexNoiseShaderParameters` patterns.

The shader runner must:

- Implement `ISDFShader`.
- Load the generated GLSL shader through `ComputeShader`.
- Allocate a parameter buffer using `Marshal.SizeOf`.
- Validate the parameter type.
- Upload changed parameters.
- Bind all five buffers.
- Dispatch using the existing pipeline dimensions.
- Dispose the shader and parameter buffer.

### 6. Preserve the five-buffer contract

Every generated shader runner must bind all five sets:
Set 0: stage parameter buffer Set 1: SDF parameter buffer Set 2: biome parameter buffer Set 3: temperature values buffer Set 4: output buffer


Every generated GLSL shader must declare compatible bindings for all five sets, even if a particular calculation does not read every buffer.

Use the existing names and layout conventions from `constant.glsl` and `simplex_noise.glsl`.

### 7. Register the stage

Automatically update `SDFPipelineParser.cs`:

- Add the stage namespace import.
- Add a `case` using `<StageName>Stage.FunctionName`.
- Add a private deserialization method for the stage.

Automatically update `SDFPipeline.cs`:

- Add the shader namespace import.
- Add a `case` using `<StageName>Stage.FunctionName`.
- Instantiate `<StageName>Shader`.

Preserve the existing switch-based registration style.

### 8. Provide sample JSON

Print a complete sample pipeline entry in the chat. The sample must include:

- The new function name.
- All required stage parameters.
- A matching shader map entry.
- A valid temperature or biome placement.

Use this shape:
{ "shaders": { "<FunctionName>": "res://Shaders/Compute/<stage_name>.glsl" }, "temperature": { "sdf": [ { "function": "<FunctionName>" } ] }, "biomes": [] }


Include all inferred stage properties in the stage object.

### 9. Validate the result

After editing:

- Confirm every generated C# file uses the correct folder and namespace.
- Confirm the parser can deserialize the sample JSON.
- Confirm the function name is identical in:
  - The stage class.
  - The parser switch.
  - The pipeline shader switch.
  - The sample JSON.
  - The shader map.
- Confirm all five bindings are present.
- Build the affected projects.
- Report any build or shader compilation issues.
- Do not create tests unless the user explicitly requests them.