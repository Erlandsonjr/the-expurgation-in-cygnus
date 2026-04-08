# Project Context

## Current Phase

Week 3 - AI & Level 1 Management

## Active Task

Tuning the combat feel before full Week 3 AI & Level 1 Management implementation.

## Roadmap Alignment

8-week structural integration

- Week 1 - Motor Foundations: 100% completed and verified in the Unity Editor.
- Week 2 - Basilar Combat: completed with held-M1 firing, pooled projectile reuse, and operational weapon-data assets without regressing the completed movement motor.

## Requirement Confirmation

Purger is the game's cybernetic super-soldier protagonist: a century-old living weapon whose synthetic body and modular warfare systems justify systemic upgrades, physical augmentation, and weapon transmutation during the run.

The week-one controller must preserve the core GDD constraint of independent 360-degree aim. Movement remains side-view and lateral, while a dedicated Pivot rotates toward the mouse cursor in world space without coupling to locomotion direction, facing, or jump state.

Week 2 must build on that verified motor by keeping movement and aiming independent while layering in continuous fire, pooled projectile lifecycle management, and data-driven weapon tuning through ScriptableObjects.

With Week 2 finalized, the project can now move into enemy behavior and encounter management while reusing the completed player motor and combat stack.

## Completed Tasks

- Implementing Firing System.
- Finalizing continuous primary fire from the verified 360-degree aim pivot.
- Making projectile Object Pooling operational for runtime reuse.
- Activating the ScriptableObject architecture for weapon attributes through WeaponData and the BaseWeapon asset.

## Progress Update

- Git repository initialized in the project root.
- Unity repository scaffolding created with the requested Assets subdirectories.
- Week 1 PlayerController implementation added with precision movement, jump forgiveness, variable gravity, and mouse-driven Pivot aiming.
- Unity .meta files were added for the new folders and PlayerController so the first commit preserves valid asset identity.
- Week 1 implementation keeps firing and pooling out of scope so the controller stays aligned with the roadmap and ready for later ScriptableObject stat injection.
- Week 1 - Motor Foundations is now 100% completed and verified in the Unity Editor, including lateral movement, advanced jump forgiveness, and 360-degree independent aim.
- Week 2 combat has been finalized with continuous held-M1 firing, pooled projectile reuse, and ScriptableObject-driven weapon stats.
- The Object Pooling and ScriptableObject architecture for weapons is now fully operational.
- The Week 2 combat scripts are ready for in-editor verification and the next commit pass.
- Combat feel tuning is now in progress, with projectile spawn cleanup and architectural consistency refinements.
- Projectile collisions now rely on Ground LayerMasks instead of Ground tags for consistency with the player motor ground-check approach.

## Week 2 Implemented Architecture

### ProjectilePooler

- Type: MonoBehaviour.
- Responsibility: own a reusable pool of projectile instances so the firing system can request and return projectiles without runtime instantiate/destroy spikes.
- Core serialized fields: projectilePrefab, initialPoolSize, canExpand, poolRoot.
- Core methods: WarmPool(int count), GetProjectile(Vector3 position, Quaternion rotation), ReturnProjectile(GameObject projectile).
- Integration note: active projectiles detach from the inactive pool root, and returned projectiles rejoin the queue after self-deactivation.

### WeaponData

- Type: ScriptableObject.
- Responsibility: store weapon balance data independently from runtime firing logic.
- Core serialized fields: damage, fireRate, projectileSpeed.
- Core read-only helpers: Damage, FireRate, ProjectileSpeed, ShotInterval.
- Integration note: the future firing script should read timing and projectile speed directly from WeaponData so combat tuning stays data-driven.

## Current Status

Weeks 1 and 2 are complete. The project is now positioned for Week 3 work around simple tracking IAs and Level 1 management.

A short combat-feel tuning pass is underway to tighten projectile behavior before enemy AI implementation begins.

## Next Goal

Week 3 - AI & Level 1 Management: simple tracking IAs.