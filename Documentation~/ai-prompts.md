# AI prompt and change history

Log of AI-assisted changes to the framework, as required by the Movement Release spec.
One entry per prompt/session: what was asked, which constraints were given, what changed, and how the result was verified.

## 2026-10-03 - Package hygiene

- **Tool:** Claude Code (Claude Opus 5.5)
- **Prompt:** Clean up the UPM template: remove example classes and template docs, modernize test asmdefs, reference Input System from the runtime asmdef, fix package.json metadata, write README install instructions.
- **Changes:** Removed template placeholders (example scripts, Samples, Documentation, Third Party Notices). Added `Unity.InputSystem` reference, `physics2d` dependency, `rootNamespace` to asmdefs. Test asmdefs moved from deprecated `optionalUnityReferences` to `UnityEngine/UnityEditor.TestRunner` + `nunit.framework.dll` + `UNITY_INCLUDE_TESTS`. Dropped `unityRelease` and placeholder URLs.
- **Verification:** Dev project re-resolves the package with the new dependencies, no console errors or warnings. Test asmdefs are not exercised yet (no test scripts); verify discovery when the first tests land.
