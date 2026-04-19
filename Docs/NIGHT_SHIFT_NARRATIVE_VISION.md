# Night Shift Narrative Vision

## Core Premise

The player is a rail checkpoint inspector working permanent night shifts at a remote station.
They are not here by choice. They are paying off a personal debt.

The narrative frame must always reinforce one thing:

- this is not a heroic job;
- this is survival through disciplined work;
- every mistake costs money;
- every shift starts with orders;
- every shift ends with settlement.

## Player Fantasy

The player should feel like:

- they wake up in the same small room every night;
- they already know the routine, but the pressure keeps growing;
- the station is impersonal, cold, and procedural;
- letters are how authority speaks to them;
- debt is the true antagonist.

## Narrative Delivery Rules

Narrative should be delivered through systems the player already uses.

- Shift start: letter under the door.
- Mistakes: notification toast.
- End of shift: day summary as a settlement note.
- Campaign ending: final outcome screen.

No long exposition dumps.
No dialogue trees.
No separate story mode.

## Opening Routine

Every shift starts the same way:

1. The player wakes up near the bed.
2. A brief blink animation restores vision.
3. A knock lands on the door with a light camera shake.
4. A letter slides under the door.
5. The player reads the letter.
6. Control is returned to normal gameplay.

Only on the first night does the game explicitly direct the camera to the letter.
After that, the player is expected to understand the ritual.

## Tone

The game tone should be:

- restrained;
- dry;
- practical;
- slightly threatening;
- never melodramatic.

Orders should sound institutional, not emotional.

## Text Language Rule

All player-facing letters and formal messages should be written in English.

Recommended framing:

- "Night Orders"
- "Shift Notice"
- "Final Notice"

Avoid using "morning" in player-facing text, because the game takes place during night shifts.

## Economic Story

The summary screen should tell a story through five numbers:

1. Income from correct inspections.
2. Fines from mistakes.
3. Living costs.
4. Net result.
5. Debt after settlement.

This makes the economy feel personal instead of abstract.

## Final Notice Rule

On the last authored day, the shift letter must explicitly warn the player.

Example intent:

"This is your last shift. Be careful tonight. The debt must be cleared before dawn."

## Ending Rule

At the end of the campaign:

- if the debt is cleared, the player gets a win ending;
- if the debt remains, the player gets a lose ending.

Later this can expand into multiple ending grades, but the first implementation should stay binary.

## Scope Guardrails

Do not use Timeline for the wake-up, knock, or letter routine.
That sequence should stay controller-driven and lightweight.

Timeline is only worth revisiting later for large authored moments such as:

- campaign intro;
- campaign outro;
- rare major events.
