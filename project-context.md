# Project Context

## Current Phase

Week 1 - Motor Foundations

## Active Task

Implementing the Player Controller

## Roadmap Alignment

8-week structural integration

## Requirement Confirmation

Purger is the game's cybernetic super-soldier protagonist: a century-old living weapon whose synthetic body and modular warfare systems justify systemic upgrades, physical augmentation, and weapon transmutation during the run.

The week-one controller must preserve the core GDD constraint of independent 360-degree aim. Movement remains side-view and lateral, while a dedicated Pivot rotates toward the mouse cursor in world space without coupling to locomotion direction, facing, or jump state.

## Progress Update

- Git repository initialized in the project root.
- Unity repository scaffolding created with the requested Assets subdirectories.
- Week 1 PlayerController implementation added with precision movement, jump forgiveness, variable gravity, and mouse-driven Pivot aiming.
- Unity .meta files were added for the new folders and PlayerController so the first commit preserves valid asset identity.
- Week 1 implementation keeps firing and pooling out of scope so the controller stays aligned with the roadmap and ready for later ScriptableObject stat injection.
- All current files are staged for the first commit.

## Current Status

Week 1 player motor foundations are in place and ready to be wired into the Purger prefab and scene objects.