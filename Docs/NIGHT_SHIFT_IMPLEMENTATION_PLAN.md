# Night Shift Implementation Plan

Reference vision:
`Docs/NIGHT_SHIFT_NARRATIVE_VISION.md`

## Goal

Add a repeatable night-shift narrative loop without breaking the current station gameplay flow.

The final loop should be:

1. Wake up.
2. Receive night letter.
3. Play train inspection shift.
4. See settlement summary.
5. Advance to next shift or campaign ending.

## Block 0 - Narrative Data Foundation

Goal:
Make narrative content authorable per day.

Deliverables:

- Extend `StationDayConfig` with shift letter fields.
- Extend runtime day content with the same payload.
- Add daily living cost data for the summary.
- Add an optional last-day override for final notice wording.

Suggested data fields:

- `shift_letter_title`
- `shift_letter_body`
- `shift_letter_hint`
- `living_cost`
- `living_cost_label`
- `final_notice_title`
- `final_notice_body`

Notes:

- All player-facing authored text should be in English.
- "Night Orders" is the preferred default title for ordinary days.

## Block 1 - Wake-Up UI Layer

Goal:
Create the visual language for shift start.

Deliverables:

- `UIWakeTransitionView : UIBaseElement`
- `UIWakeTransitionController`

Responsibilities:

- Play the blink animation with two black bars or panels.
- Support a short wake-up sequence.
- Optionally allow a first-night camera nudge before releasing control.

Rules:

- No Timeline.
- Very short sequence.
- Reusable every day.

## Block 2 - Room and Letter World Setup

Goal:
Add scene anchors and a physical-looking letter routine.

Deliverables:

- Extend `SceneContext` / `SceneRuntimeSetup` with room references:
  - wake-up point
  - door point
  - letter throw start point
  - letter landing point
- Add a reusable world letter prefab.
- Add `MorningLetterInteractableView` based on `PlayerInteractableView`.

Responsibilities:

- Spawn the player at the wake-up point.
- Animate the letter sliding under the door.
- Enable interaction only after the letter lands.

Rules:

- Use tweened deterministic motion, not physics.
- The letter is a normal world interactable.

## Block 3 - Shift Intro Orchestration

Goal:
Build the full shift-start sequence as gameplay orchestration.

Deliverables:

- `DayIntroController` or `NightShiftIntroController`

Responsibilities:

- Lock player control at shift start.
- Trigger wake-up UI.
- Play knock sound.
- Trigger light camera shake.
- Spawn and animate the letter.
- On first night only: briefly direct attention to the letter.
- Wait until the player reads and closes the letter.
- Release control back to gameplay.

Architecture:

- Keep this as a project controller, not a global service.
- It should coordinate existing services plus local scene objects.

## Block 4 - Letter UI and Reading Flow

Goal:
Turn the letter into the player's main narrative delivery channel.

Deliverables:

- `UILetterView : UIBaseElement`
- `UILetterController`
- interaction flow between world letter and UI letter

Responsibilities:

- Open the full letter UI when the player presses `E`.
- Show title, body, and optional hint/footer.
- Pause normal interaction while the letter is open.
- Resume gameplay when the player closes the letter.

Text expectations:

- Formal, compact, dry English.
- The letter should teach the day if needed, not just flavor it.

## Block 5 - Station Flow Integration

Goal:
Insert the intro sequence into the current day lifecycle cleanly.

Deliverables:

- Add a "day intro pending" step to `StationFlowController`.
- Delay `AwaitingTrainCall` until intro is completed.
- Re-run the same shift intro every day.

Responsibilities:

- `StationFlowController` remains the owner of flow state.
- Intro controller only gates the start of playable work.

Important rule:

- First night includes camera guidance to the letter.
- All later nights skip camera guidance and rely on learned routine.

## Block 6 - End Of Day Settlement Upgrade

Goal:
Turn the current summary into a narrative settlement note.

Deliverables:

- Extend `DebtModel` or a day settlement model with living costs.
- Extend `UIDaySummary` and `DaySummaryController`.
- Add a short end-of-shift note line.

Required output on the summary screen:

1. Income from correct inspections.
2. Fines from mistakes.
3. Living costs.
4. Net result.
5. Debt after settlement.

Optional extra:

- one short flavor line reacting to the shift outcome.

## Block 7 - Final Notice Logic

Goal:
Give the last authored day special narrative pressure.

Deliverables:

- Detect last authored campaign day in the runtime flow.
- Replace or augment that day's standard letter with `Final Notice`.

Responsibilities:

- Warn the player that tonight must clear the debt.
- Preserve authored text override when needed.

Suggested rule:

- If this is the final authored day, default to final notice messaging automatically.

## Block 8 - Campaign Ending

Goal:
Close the campaign with a proper win or lose screen.

Deliverables:

- `UICampaignEndingView : UIBaseElement`
- `CampaignEndingController`

Responsibilities:

- Show ending after the last day summary is closed.
- Pick result by debt state:
  - debt cleared -> win
  - debt remains -> lose

Later expansion:

- grade endings by debt size, mistakes, or performance streaks.

## Block 9 - Content Pass

Goal:
Author content for all current days.

Deliverables:

- English shift letters for each authored day.
- Final notice variant for the last day.
- End-of-day flavor notes.
- Living cost values and labels.

Authoring priorities:

- Day 1: explain the job and debt.
- Middle days: escalate control pressure and hints about smuggling.
- Final day: explicit high-risk final warning.

## Recommended Implementation Order

1. Block 0 - Narrative Data Foundation
2. Block 1 - Wake-Up UI Layer
3. Block 2 - Room and Letter World Setup
4. Block 3 - Shift Intro Orchestration
5. Block 4 - Letter UI and Reading Flow
6. Block 5 - Station Flow Integration
7. Block 6 - End Of Day Settlement Upgrade
8. Block 7 - Final Notice Logic
9. Block 8 - Campaign Ending
10. Block 9 - Content Pass

## What Can Be Improved Later

- Replace simple first-night camera guidance with a more polished authored camera move.
- Add rare special-event letters between ordinary shifts.
- Add multiple ending grades instead of a binary result.
- Revisit Timeline only if the project later needs larger authored scenes.
- Add a reusable cutscene service in PlatformCore only after at least two real cutscene use cases exist.
