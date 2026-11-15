# Stage 6 — Final integration and validation hardening

Stage 6 focuses on persistence reliability, regression coverage, and submission readiness. It does not add another gameplay subsystem.

## Save-file safety

`WorldSaveService.Save` now writes serialized data to a uniquely named temporary file in the destination directory. Once the write completes, it replaces the destination using an overwrite move. A failed write therefore leaves the previous save in place in the normal failure cases. Temporary files are removed on a best-effort basis.

`WorldSaveService.Load` validates the deserialized document before creating immutable dictionaries. It checks required sections and null collection entries, player/time ranges, terrain and item references, positive item quantities, object definitions and durability, non-empty/unique object IDs, unique tile positions, and that every object and the player are located on a saved tile. Malformed JSON and invalid data return typed `Result` failures.

## Additional tests

`Stage6Tests.cs` adds five tests:

1. Malformed JSON returns a controlled failure.
2. Missing required sections are rejected.
3. Unknown item references are rejected.
4. Saving a null/incomplete state returns `INVALID_STATE`.
5. A failed load does not mutate the caller's existing simulation state.

The complete custom test runner registers 76 tests after Assignment 8 integration. Run it locally using the commands in the root `README.md`. The .NET test suite has not been executed in the current build environment because the .NET SDK is unavailable.

## Submission checklist

- Include `SurvivalSim/`, `SurvivalSim.Tests/`, JSON definitions, native C source/header, build scripts, and README files.
- Build the managed projects, then build the native library so it is copied beside both executables.
- Run the complete test runner and record its actual result.
- Exclude `bin/`, `obj/`, `.vs/`, `.git/`, and user-specific IDE files.
- Confirm the Assignment 5–8 mapping against the original assignment PDFs and your instructor's rubric.


## Assignment 8 integration follow-up

The playable world is now loaded from `Data/Definitions/world.json` through `WorldDataLoader`, so the two LINQ reports operate on JSON-loaded application data. Assignment 8 tests cover report edge cases and deterministic ties, the real `yield return` world-position iterator and materialized snapshot, controlled native adapter loading failure, convenience-file handle release, and a complete native gathering/report workflow. See the root `README.md` for the full rubric mapping and exact expected examples.
