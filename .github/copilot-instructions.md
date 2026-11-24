# GitHub Copilot Instructions

Before making changes, read and follow [AGENTS.md](../AGENTS.md) at the repository root. It is the single source of instructions for this repository and takes precedence over this file.

If you can't open `AGENTS.md`, these are its critical rules:

- Performance and memory efficiency are non-negotiable. Avoid per-frame, per-row, and per-cell allocations, LINQ, closures, boxing, uncached reflection, and temporary collections in hot paths (layout, scrolling, row and cell recycling, sorting, filtering).
- Preserve row virtualization and recycling; bound caches and pools; balance event subscriptions; clear state on recycle and unload.
- Never sacrifice correctness for performance.
- Keep the library Native AOT and trimming compatible: no runtime code generation or untracked reflection, and no unjustified warning suppressions.
- Keep changes working on both WinUI and Uno, and add tests to the shared test sources.
- Preserve public API and behavior. Never weaken tests, and never claim builds or tests passed unless you ran them.
- Public members need XML docs (CS1591 is an error). Keep code comments short, and never leave comments explaining removed code.
- Write "WinUI", never "WinUI 3".
