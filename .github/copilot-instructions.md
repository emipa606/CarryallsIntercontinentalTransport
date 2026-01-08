# GitHub Copilot Instructions for Carryalls: Intercontinental Transport (Continued)

## Mod Overview and Purpose

**Carryalls: Intercontinental Transport (Continued)** is a RimWorld mod that introduces a series of VTOL aircraft, known as carryalls, to enhance intercontinental transportation within the game. The mod aims to transform RimWorld’s caravan system, especially for late-game situations, by providing advanced, albeit overpowered, transportation options. This is targeted to make the game more engaging and dynamic in terms of global travel capabilities.

The creation enjoys visual inspirations from classic video games like Command and Conquer, with original artwork designed to fit seamlessly into the RimWorld universe.

## Key Features and Systems

1. **Variety of Carryall Models:**
   - *Standard Carryall*: A basic, non-orbital, space shuttle replica.
   - *Ox Carryall*: Mass transport with large storage and a small turret.
   - *Vertigo Carryall*: Optimized for rapid assaults, with limited storage but includes a minigun turret.
   - *Kirov Airship*: Slow but terrain-agnostic movement, sometimes triggers battlefield explosions.

2. **Gameplay Mechanics:**
   - Carryalls follow drop pod mechanics—only items in stockpile zones are recognized.
   - Onboard turrets consume significant fuel; managing fuel is crucial.
   - Landing creates a 1x1 tile dead zone, potentially damaging entities within this space.

3. **Integration and Compatibility:**
   - Works well with mods like Better Explosions and Set Up Camp.
   - Known conflicts with mods such as SRTS Expanded, Transport Shuttle due to core code similarities.

4. **Known Issues:**
   - Automatic payload drop upon landing.
   - Boarding issues need cargo reset after weight or seat denial.
   - Discontinued support for vanilla 1.6 due to similar features now existing.

## Coding Patterns and Conventions

- Class Naming: Follows PascalCase, e.g., `Public class CompProperties_Carryall`.
- Method Naming: Uses camelCase, e.g., `private void dropCarryall`.
- Code files are organized by functionality and feature distinct class groups to separate logic layers.

## XML Integration

XML files are utilized for defining in-game objects and parameters. Ensure XML includes all necessary def tags:
- `ThingDef` for carryall vehicles.
- `CompProperties` for component attributes.
- Ensure consistency across def XML files to prevent data mismatch.

## Harmony Patching

- Harmony is employed to adjust game functionality where default behaviors do not align with mod requirements.
- For developers using Harmony, make sure patches do not interfere with core methods causing compatibility issues with other mods.
- Example: Multiple classes such as `HarmonyTest` are used to manage and isolate various patches.

## Suggestions for Copilot

1. **Automate XML Definitions:**
   - Use Copilot suggestions to auto-create XML structure for new carryall models.
   - Suggest improvements or missing tags if XML validation indicates errors.

2. **Improve Harmony Debugging:**
   - Provide Copilot with context on patches to suggest meaningful debug code.
   - Automate logging for harmony patches to catch runtime exceptions.

3. **Streamline Fuel Management:**
   - Offer suggestions to optimize fuel usage routines to align with recommended gameplay mechanics.
   
4. **Enhance User Interface:**
   - Generate UI enhancements via Copilot for better interaction with the carryall menu and settings.

5. **Resolve Known Issues:**
   - Utilize Copilot’s pattern recognition to identify potential fixes for the automatic payload drop issue.
   - Assist in refactoring or suggesting alternative methods for boarding logic errors.

By following these guidelines, you can ensure that Copilot's assistance complements and enhances development efforts for this modding project.
