# SurvivalSim — Assignment 5: Composing Functions Using Pipelining

**Course:** Introduction to Functional Programming (2026–2027)  
**Project:** SurvivalSim — a turn-based console survival game with future realistic technological progression  
**Team:** Individual project (1 student)  
**Student name:** Yerbolov Adil  
**Status:** Reference implementation prepared from the action-processing design; verify and adapt to your existing project before submission.

## 1. Topic, users, and scope

SurvivalSim is a console survival prototype inspired by Cataclysm-style action selection and future TerraFirmaCraft-style technological advancement. Its intended users are players who want to manage time, energy, hunger, and thirst. Assignment 5 implements **one complete workflow: processing a batch of player actions**. Gathering items, crafting, world exploration, and rest-based energy recovery are planned, **not implemented**.

An action has a name, duration in minutes, and energy cost. The player has energy, hunger, and thirst reserves (0–100), plus elapsed time in minutes. The workflow reports accepted/rejected actions and the resulting player state.

## 2. Data and business rules

Input: initial `GameState`, a collection of `ActionRequest` records, and two selected `Func<int,int>` survival policies. Output: `BatchResult` containing one `ActionOutcome` per request and the final `GameState`.

Rules:
- Name must be present and nonblank; duration must be positive; energy cost must be between 0 and 100 inclusive.
- If starting energy is below 30, action duration increases by half the original duration (integer division).
- An action succeeds only when available energy is at least its energy cost. Exactly enough energy is allowed.
- Failed actions do not advance time or change the player state; later batch actions still run.
- Energy, hunger, and thirst are clamped to the range 0–100. Zero-cost actions are allowed, but **rest does not yet restore energy**.
- Normal policy: hunger drain = minutes / 20; thirst drain = minutes / 15.
- Harsh policy: hunger drain = minutes / 10; thirst drain = minutes / 8.
- Integer division truncates fractions. For example, a 40-minute action costs 2 hunger under Normal and 4 under Harsh.

### Sample dataset

`SurvivalSim/Data/actions.json` contains six records:

| Action | Minutes | Energy | Purpose |
|---|---:|---:|---|
| Walk | 20 | 5 | Normal accepted case |
| Gather sticks | 40 | 10 | Different policy outcomes |
| Collect stones | 1 | 1 | Positive duration boundary |
| Chop wood | 40 | 95 | Insufficient-energy rejection after earlier actions |
| Invalid backward action | -10 | 3 | Invalid duration rejection |
| Rest | 60 | 0 | Zero energy-cost boundary (no recovery yet) |

The invalid sample is deliberately retained so that the program demonstrates rejection.

## 3. Functional pipeline

The action workflow uses three named pure stages inside `SurvivalSim/GameLogic/Main.cs`:

| Stage | Input → output | Rule | Example |
|---|---|---|---|
| `PrepareAction` | `GameState, GameAction → GameAction` | Increase duration by 50% (integer truncation) if energy < 30 | 20 minutes at 20 energy → 30 minutes |
| `CalculateEffects` | `GameAction, Func<int,int>, Func<int,int> → ActionEffects` | Invoke supplied hunger and thirst policies | 40 minutes Normal → hunger 2, thirst 2 |
| `ApplyEffects` | `GameState, ActionEffects → GameState` | Advance time and return a new player state | 100 energy, cost 10 → 90 energy |

`ExecuteAction` validates raw request fields and energy availability, then calls `PrepareAction → CalculateEffects → ApplyEffects` in that order. `ExecuteBatch` processes multiple requests and accumulates outcomes. No pure stage reads the console, files, clock, or random generator. `with` expressions return new records without changing the original inputs.

`CalculateEffects` is the required higher-order function: it **invokes** the two delegates it receives. `Program.cs` chooses the Normal or Harsh policy and passes it to the same pipeline. No mutable global setting determines the policy.

## 4. Expected results before execution

For a fresh state of energy=100, hunger=100, thirst=100, time=480 and a single `Gather sticks` action of 40 minutes, energy cost 10:

| Policy | Energy | Hunger | Thirst | Time |
|---|---:|---:|---:|---:|
| Normal | 90 | 98 | 98 | 520 |
| Harsh | 90 | 96 | 95 | 520 |

For a 20-minute `Walk` with energy cost 10 and starting energy exactly 10, the action succeeds and leaves energy 0. With starting energy 9, the action is rejected and state is unchanged.

An empty **valid** dataset returns zero outcomes and the original state. Missing dataset input is different from an empty dataset and is rejected. A negative duration is rejected rather than silently replaced.

## 5. Project structure

```text
SurvivalSim_Assignment5/
├── README.md
├── SurvivalSim/
│   ├── SurvivalSim.csproj
│   ├── Program.cs
│   ├── Models/Models.cs
│   ├── GameLogic/Main.cs
│   └── Data/actions.json
└── SurvivalSim.Tests/
    ├── SurvivalSim.Tests.csproj
    └── Program.cs
```

## 6. Requirements mapping

| Assignment 5 requirement | Location |
|---|---|
| Topic, users, rules, expected output | README sections 1–4 |
| Dataset ≥5 records, accepted/rejected/boundary/policy cases | `Data/actions.json` |
| Typed immutable models | `Models/Models.cs` |
| Missing/invalid fields rejected | `Main.ExecuteAction` |
| Three named pure stages | `Main.PrepareAction`, `CalculateEffects`, `ApplyEffects` |
| Higher-order function and two policies | `Main.CalculateEffects`, `normalHunger`, `harshHunger`, etc. |
| I/O outside pure stages | `Program.cs` |
| Automated assertion tests | `SurvivalSim.Tests/Program.cs` |

## 7. Build, run, and test

**Prerequisites:** .NET SDK 8 or newer compatible with `net8.0`. The project targets .NET 8. No NuGet test framework is needed.

**Working directory:** repository root (the folder containing this README).

```bash
dotnet --version
dotnet build SurvivalSim/SurvivalSim.csproj
dotnet run --project SurvivalSim/SurvivalSim.csproj
dotnet run --project SurvivalSim.Tests/SurvivalSim.Tests.csproj
```

The main program loads the sample JSON from the output directory (automatically copied by the project file), processes it with both policies, and displays accepted/rejected actions and final states. The test runner prints `[PASS]` or `[FAIL]` for every assertion and exits with a nonzero status if any test fails.

## 8. Automated test cases

| Test | Expected result |
|---|---|
| Normal workflow | Gather sticks: time 520, energy 90, hunger 98, thirst 98 |
| Empty dataset | Zero outcomes, unchanged state |
| Exact energy boundary | Energy 10 and cost 10 succeeds; final energy 0 |
| Insufficient energy | Energy 9 and cost 10 rejects; state unchanged |
| Invalid duration | Zero and negative duration rejected |
| Missing input | Null request and missing action name rejected |
| Two policies | Same 40-minute action: hunger 98 Normal vs 96 Harsh |
| Pure stages and unchanged state | Fatigue duration adjustment correct; original records unchanged |
| Caller collection unchanged | Batch does not modify the caller's action list |
| Invalid energy cost | Negative energy cost rejected |
| Zero energy cost | Action accepted even at zero energy |

**Latest execution:** [FILL IN AFTER RUNNING: number passed, number failed, date, SDK version]. Do not claim the tests passed until they have been executed in your environment.

## 9. Demonstration for defense

1. Run the main program and show Normal and Harsh results on the same dataset.
2. Trace `Gather sticks` through `PrepareAction`, `CalculateEffects`, and `ApplyEffects`.
3. Point to the `Func<int,int>` invocation inside `CalculateEffects`.
4. Show the invalid negative-duration record being rejected.
5. Show the empty dataset test and energy boundary test.
6. Run the automated tests and explain how they compare expected vs actual values.
7. Be ready to change a policy divisor or threshold and update a corresponding test assertion.

## 10. Individual contribution

**Student:** [NAME]  
**Group:** [GROUP]  
**Implemented logic:** [EDIT TO REFLECT WHAT YOU PERSONALLY IMPLEMENTED]  
**Tests written:** [EDIT TO REFLECT YOUR ACTUAL WORK]  
**Documentation:** [EDIT TO REFLECT YOUR ACTUAL WORK]

For an individual project, the same student owns the implementation, testing, and documentation. Replace these placeholders with truthful details before submission.

## 11. Limitations and future work

- No playable interactive menu yet: this snapshot demonstrates the action workflow on sample data.
- No inventory rewards, map, crafting, tool requirements, or technological progression yet.
- Rest currently consumes time and has zero energy cost, but does not restore energy.
- Input validation is part of the application workflow; the pure transformation stages expect validated values.
- Assignment 6 can add validated factories and explicit action/crafting states; Assignment 7 can replace simple outcomes with a reusable `Result` type.

## 12. Submission checklist

Include source, `.csproj` files, tests, sample JSON, and README in a ZIP. Exclude `bin/`, `obj/`, `.vs/`, and secrets. Verify a clean extraction builds and runs. Update personal details, test results, and actual contribution statements. During defense, explain the implementation and make a small requested modification. Follow the course syllabus rules on AI assistance and acknowledge permitted AI use.
