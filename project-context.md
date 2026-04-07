# Project Context

## Current Phase

Week 2 - Basilar Combat

## Active Task

Building the Week 2 combat foundation: continuous primary fire, projectile object pooling, and the first weapon ScriptableObject template.

## Roadmap Alignment

8-week structural integration

- Week 1 - Motor Foundations: 100% completed and verified in the Unity Editor.
- Week 2 - Basilar Combat: establish the first combat loop, projectile reuse, and weapon-data scaffolding without regressing the completed movement motor.

## Requirement Confirmation

Purger is the game's cybernetic super-soldier protagonist: a century-old living weapon whose synthetic body and modular warfare systems justify systemic upgrades, physical augmentation, and weapon transmutation during the run.

The week-one controller must preserve the core GDD constraint of independent 360-degree aim. Movement remains side-view and lateral, while a dedicated Pivot rotates toward the mouse cursor in world space without coupling to locomotion direction, facing, or jump state.

Week 2 must build on that verified motor by keeping movement and aiming independent while layering in continuous fire, pooled projectile lifecycle management, and data-driven weapon tuning through ScriptableObjects.

## Progress Update

- Git repository initialized in the project root.
- Unity repository scaffolding created with the requested Assets subdirectories.
- Week 1 PlayerController implementation added with precision movement, jump forgiveness, variable gravity, and mouse-driven Pivot aiming.
- Unity .meta files were added for the new folders and PlayerController so the first commit preserves valid asset identity.
- Week 1 implementation keeps firing and pooling out of scope so the controller stays aligned with the roadmap and ready for later ScriptableObject stat injection.
- Week 1 - Motor Foundations is now 100% completed and verified in the Unity Editor, including lateral movement, advanced jump forgiveness, and 360-degree independent aim.
- Week 2 environment prep has started with planned scaffolding for projectile pooling and the first weapon-data ScriptableObject template.
- All current files are staged for the first commit.

## Week 2 Suggested Class Structure

### ProjectilePooler

- Type: MonoBehaviour.
- Responsibility: own a reusable pool of projectile instances so the firing system can request and return projectiles without runtime instantiate/destroy spikes.
- Core serialized fields: projectilePrefab, initialPoolSize, canExpand, poolRoot.
- Core methods: WarmPool(int count), GetProjectile(Vector2 position, Quaternion rotation), ReturnProjectile(GameObject projectile).
- Integration note: the future firing script should only ask the pooler for projectiles and pass them back when their lifetime or collision ends.

### WeaponData

- Type: ScriptableObject.
- Responsibility: store weapon balance data independently from runtime firing logic.
- Core serialized fields: damage, fireRate, projectileSpeed.
- Core read-only helpers: Damage, FireRate, ProjectileSpeed, ShotInterval.
- Integration note: the future firing script should read timing and projectile speed directly from WeaponData so combat tuning stays data-driven.

## Current Status

Week 1 is complete. The project is ready to enter Week 2 with the player motor verified and the next combat architecture defined around continuous fire, projectile pooling, and weapon data assets.