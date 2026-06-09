# Horizon Sentinel Project Guide

This file is the working memory for future AI/code assistants working on Horizon Sentinel.

## Project Location

Main folder:

```text
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Horizon Sentinel
```

Readable source:

```text
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Horizon Sentinel\Horizon Sentinel.cs
```

Paste-ready PB script:

```text
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Horizon Sentinel\Horizon Sentinel.min.cs
```

Custom Data guide:

```text
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Horizon Sentinel\Example CustomData.txt
```

Minified tool project:

```text
F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Horizon Sentinel\Minified_Tool
```

## Build / Minify Workflow

Horizon Sentinel is a Space Engineers programmable block script written as raw PB source.

Do not hand-minify unless the merge tool fails. Use the local `IngameScriptMergeTool` wrapper project:

```powershell
& "F:\Space Engineers Script\My-Personal-Space-Engineer-Script\Horizon Sentinel\Minified_Tool\BuildMinified.ps1"
```

This writes:

```text
Horizon Sentinel.min.cs
```

Current successful minified result:

```text
Chars: 78446
Braces: 226/226
```

The wrapper project is based on the same pattern as AGM and HogOS. It wraps the raw PB code into:

```csharp
namespace Script
{
    public sealed class Program : MyGridProgram
    {
        // raw PB source here
    }
}
```

Then `IngameScriptMergeTool` merges and minifies it.

## Script Purpose

Horizon Sentinel is not just a display script. It is intended to be a spaceship safety, status, and automation controller.

Core goals:

- Cargo safety and weight warnings.
- Planet entry / descent safety.
- Power status and reactor charging automation.
- Fuel status, hydrogen tank status, oxygen tank status.
- Jump drive group status and range/charge information.
- Turret status and ammunition detail.
- Ship pressurization and life support monitoring.
- Damage status.
- Thruster status for lift/cruise/brake groups.
- Future automation for ascent/descent, broadcast warnings, reactor charging, tank balancing, and ammo balancing.

The user wants this usable on any large ship. Do not make it tied to Manta/Ray naming.

## Visual Identity

Name:

```text
Horizon Sentinel
```

Meaning:

- "Sentinel" means guard/watchkeeper.
- The branding should feel like a ship guardian system.

Splash screen design:

- Ocean/horizon/dawn style color bands.
- Shield logo centered.
- Crosshair inside shield.
- Small red pulsing center dot.
- Title "Horizon Sentinel" under shield.
- Orange underline with small downward center notch under title.

The user likes the current splash version when it shows the shield/crosshair/title correctly. Do not randomly redesign it. Fix broken placement only.

Preferred style:

- Cyan HUD frame lines.
- Dark teal/blue background.
- Orange accent line.
- Green for OK/ready.
- Yellow for caution.
- Red for critical/pulse only.
- Avoid big empty panels with tiny text.
- Normal LCD pages should be detailed.
- Corner LCDs should be compact but must still show title/status text.

Recent font targets from user:

```text
Big title: about 1.0
Status text: about 0.8
Normal row text: about 0.65
Small row value text: about 0.7
```

## Current Name Tags

These go in block names, not Custom Data:

```text
{HSLCD}
{HSCLCD}
```

`{HSLCD}` is for normal LCDs, cockpits, control seats, PB screens, and larger display surfaces.

`{HSCLCD}` is for corner LCDs and compact status screens.

The PB should detect the screen shape and choose a sensible layout, but the user may force `Layout=Full`, `Layout=Bar`, or `Layout=Stack`.

## Current Page Tags

These go in LCD/cockpit/control seat Custom Data:

```text
[HS:Splash]
[HS:Pilot]
[HS:Jump]
[HS:Cargo]
[HS:Descent]
[HS:Interplanetary]
[HS:Combat]
[HS:Damage]
[HS:Pressure]
[HS:Thrusters]
[HS:Power]
[HS:Fuel]
[HS:LifeSupport]
[HS:Battery]
[HS:Hydrogen]
[HS:Oxygen]
[HS:Ammo]
```

Preferred modern tags:

```text
[HS:Power]
[HS:Fuel]
[HS:LifeSupport]
```

Legacy aliases accepted:

```text
[HS:Battery] -> Power
[HS:Hydrogen] -> Fuel
[HS:Oxygen] -> LifeSupport
[HS:Descent] -> Interplanetary
[HS:Ammo] -> compact Combat/ammo view
```

## Block Custom Data Tags

These go on actual functional blocks, not LCDs:

```text
[HS:CargoBlock]
[HS:PressureVent]
```

Cargo behavior:

1. If any inventory blocks have `[HS:CargoBlock]`, cargo pages use only those tagged blocks.
2. Otherwise use `Group Cargo`.
3. Otherwise fall back to same-grid inventory blocks.

Pressure behavior:

1. If any vents have `[HS:PressureVent]`, life support/pressure pages use only those vents.
2. Otherwise use `Group Air Vents`.
3. Otherwise fall back to same-grid vents.

Do not include depressurization vents, hangar drain vents, or airlock drain vents in `[HS:PressureVent]`.

## PB Custom Data

The PB Custom Data uses section:

```text
[Horizon Sentinel]
```

Important settings:

```text
Auto Reactor Charging=false
Auto Hydrogen Balancing=false
Auto Ammo Balancing=false

Group Batteries=HS Batteries
Group Reactors=HS Reactors
Group Solar Panels=HS Solar Panels
Group Hydrogen Engines=HS Hydrogen Engines

Group Hydrogen Tanks=HS Hydrogen Tanks
Group Oxygen Tanks=HS Oxygen Tanks
Group Jump Drives=HS Jump Drives
Group Turrets=HS Turrets
Group Cargo=HS Cargo
Group Air Vents=HS Air Vents

Group Thrusters=HS Thrusters
Group Lifting Thrusters=HS Lifting Thrusters
Group Cruising Thrusters=HS Cruising Thrusters
Group Braking Thrusters=HS Braking Thrusters
Group Gyros=HS Gyros
```

If a configured group is missing or empty, the script should not wipe out auto-detected blocks. This was fixed for power group detection and should be preserved.

## Recommended Screen Layout

PB screen:

```text
[HS:Splash]
```

Control seat with 5 screens should show important operational details:

```text
Surface 0: [HS:Pilot]
Surface 1: [HS:Combat]
Surface 2: [HS:Jump]
Surface 3: [HS:Power] or [HS:Fuel]
Surface 4: [HS:LifeSupport] or [HS:Interplanetary]
```

Normal LCDs:

```text
[HS:Splash]
[HS:Jump]
[HS:Cargo]
[HS:Interplanetary]
[HS:Damage]
[HS:Thrusters]
[HS:Power]
[HS:Fuel]
[HS:LifeSupport]
[HS:Combat]
```

Corner LCDs:

```text
[HS:Jump]
[HS:Power]
[HS:Fuel]
[HS:LifeSupport]
[HS:Pressure]
[HS:Ammo]
```

Corner LCDs must show a title and status/value text. Do not draw unlabeled bars.

## Page Expectations

### Splash

Must keep the shield/crosshair/title design.

Should include a boot/loading bar when booting.

The user specifically dislikes when splash becomes a blank dark screen with only a red dot.

### Pilot

Must show the most important cockpit summary:

- Combat: turrets online/offline/damaged and ammo remaining.
- Interplanetary: safe to enter/avoid planet.
- Jump drive: charge, ready/charging, time to full.
- Power: battery status.
- Life support status.
- Fuel status.

### Jump

Full LCD:

- Gauge/charge.
- Online/offline count.
- Charging/ready/offline state.
- Time until full charge.
- Actual possible jump range in km, not only "percent of configured jump".

Corner LCD:

- Charging/ready state.
- Time until full charge.
- Title visible.

Damage belongs on Damage page, not Jump page.

Potential compile risk:

- Current code has used `MaxJumpDistanceMeters`. If Space Engineers PB rejects it, replace with a safer approach.

### Cargo

Must show detailed cargo containers, not only a single vague bar.

Expected:

- Each cargo container name.
- Fill bar and percent per cargo container.
- Total cargo mass.
- Cargo block count.
- Cargo status: OK/HEAVY/FULL.

Use auto-scroll if there are more containers than fit.

### Interplanetary / Descent

Preferred naming is moving toward "Interplanetary Mode" or "Space Mode" rather than only "Descent".

Expected:

- Natural gravity.
- Ship mass.
- Cargo load.
- Hydrogen/fuel.
- Lift thruster capability.
- Vertical speed.
- Clear warnings if ship is too heavy to enter/land on planet.

Important behavior:

- If lifting thrusters are off in gravity, page must not say SAFE.
- Use `Group Lifting Thrusters` for lift capacity.

### Combat

Must show each turret, not just total ammo mass.

Expected per turret:

- Turret name.
- ONLINE/OFFLINE/DAMAGED.
- Ammo amount.
- Depleted warning.

Use auto-scroll if there are more turrets than fit. User had 8 turrets and only 4 fit, so scroll is required.

Ammo mass alone is not useful to the user.

### Damage

Damage page should show damage only.

Do not include leak/pressure details here.

Expected:

- Integrity summary.
- Damaged block count.
- Non-functional block count.
- List of damaged blocks.
- Severity/how bad if possible.

Use auto-scroll when damage list is long.

Known limitation:

- PB can report terminal block damage more easily than plain armor block damage.

### Life Support

Must explain why status is partial.

Expected:

- Oxygen tank percentage.
- Pressurization percentage.
- Monitored vent count.
- Which vent is leaking/not sealed.
- Depressurize vent count.
- O2/H2 generator status.

Do not say vague nonsense like "6 vent not sealed" if the actual test case has one leaking vent. Make the page show which vent is the problem.

### Pressure

Pressure should be corner LCD only or compact status only.

Do not make a full normal pressure page unless explicitly requested.

Expected:

- Title.
- Pressurization OK/PARTIAL/LEAK.
- Percent/bar.

### Power

Expected:

- Battery percent and stored power.
- Input/output.
- Batteries online/total.
- Reactors online/total.
- Reactor uranium ingots, at least summary and ideally per reactor.
- Solar panel status.

Hydrogen engines should not dominate the Power page; they belong more naturally on Fuel. If absent, do not show misleading H2 engine rows.

Solar panel detection bug was fixed by not letting empty/missing groups override auto-detect. Preserve that behavior.

### Fuel

This is really O2/H2 status, but user still likes "Fuel Status" for the page title.

Expected:

- Hydrogen tanks, each with bar and percent.
- Oxygen tanks, each with bar and percent.
- O2/H2 generators.
- Generator ice.
- Hydrogen engine count/status if installed.
- Tank imbalance warning.

Use auto-scroll because user may have 30+ tanks.

### Thrusters

Expected:

- Lifting thrusters: enabled/disabled/working count.
- Cruising thrusters: enabled/disabled/working count.
- Braking thrusters: enabled/disabled/working count.
- All thrusters summary.

Avoid confusing labels like "Cruise group what that?" without explaining status. The screen should say enabled/disabled and online/total.

## Automation Roadmap

The user wants automation, not just displays.

Planned/desired:

- Auto ascend.
- Auto descend from space to planetary gravity.
- Broadcast warnings.
- Warn if too heavy to enter planet gravity.
- Auto alignment/leveling for descent using gyros.
- Reactor management:
  - If batteries under 25%, turn reactors on.
  - Charge batteries to 100%.
  - Turn reactors off and let solar maintain when possible.
- Hydrogen tank balancing:
  - If some tanks are full and others are low, use stockpile settings to equalize.
- Ammo balancing:
  - If some turrets fire more and have low ammo, move/balance ammo so turret ammo levels are similar.

Be careful adding automation. It must be optional and controlled by PB Custom Data flags.

## Recent Fixes / Current State

Recent work already done:

- Added the real `Minified_Tool` project and build script.
- Regenerated `Horizon Sentinel.min.cs` through `IngameScriptMergeTool`.
- Reduced paste size to about 78k chars.
- Applied user font targets.
- Added title/status text to corner LCD compact layouts.
- Added auto-scroll helper logic for long pages.
- Fuel page scrolls hydrogen tanks.
- Cargo page scrolls cargo containers.
- Combat page scrolls all turrets.
- Damage page scrolls damaged terminal blocks.
- Fixed power group detection so empty/missing configured groups do not erase auto-detected reactors/solar/H2 engines.
- Moved H2 engine reporting out of Power and toward Fuel.
- Damage title is now "DAMAGE", not "DAMAGE / LEAK".
- Combat rows now show each turret status and ammo amount.

## Testing Notes From User Screenshots

Problems user has seen and cares about:

- Jump gauge looked bad when oversized/squashed.
- Splash screen once broke into mostly blank screen with just a red dot.
- Control seat/wall LCD orientation and layout looked wrong on some screens.
- Corner LCD bars lacked titles/status text.
- Combat page only showed 4 of 8 turrets before scrolling.
- Fuel/hydrogen pages must handle many tanks.
- Damage page showed filler rows instead of useful damage.
- Life support said partial without identifying leak.
- Power page did not detect solar panels even when user had them.
- Interplanetary page said SAFE while lifting thrusters were off and ship was falling.

Avoid reintroducing these.

## Coding Rules For This Project

- Edit `Horizon Sentinel.cs`, not `Horizon Sentinel.min.cs`.
- After every source change, run `Minified_Tool\BuildMinified.ps1`.
- Keep paste size under the Space Engineers PB limit.
- Keep the script PB compatible, C# 6 style.
- Avoid modern syntax that Space Engineers PB may reject.
- Use `GridTerminalSystem.GetBlocksOfType(list, predicate)` style, not invalid overloads.
- Do not use `GetBlocks` with two arguments. That caused compile errors before.
- Preserve user tags and aliases.
- Do not remove the splash design unless explicitly asked.
- Prefer clear useful rows over decorative filler.
- Use auto-scroll for any page that can have more rows than fit.
- Do not tie the script to one ship name.

## External References User Asked To Learn

User referenced:

```text
https://spaceengineers.wiki.gg/wiki/Scripting
https://github.com/malforge/mdk2
https://malforge.github.io/spaceengineers/pbapi/
https://malforge.github.io/spaceengineers/pbapi/List-Of-Terminal-Properties-And-Actions.html
https://malforge.github.io/spaceengineers/pbapi/Type-Definition-Listing.html
https://malforge.github.io/spaceengineers/pbapi/Sprite-Listing
```

When unsure about PB API compatibility, check official/primary PB API references or known Space Engineers script examples.
