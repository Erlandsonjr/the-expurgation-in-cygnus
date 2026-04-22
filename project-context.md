# Project Context

## Current Phase

Week 3 - AI & Level 1 Management

## Active Task

Implementing Combat Loop V1: enemy damage, player contact damage, death handling, and hit-feedback integration on top of the Week 3 enemy and spawning foundation.

## Roadmap Alignment

8-week structural integration

- Week 1 - Motor Foundations: 100% completed and verified in the Unity Editor.
- Week 2 - Basilar Combat: completed with held-M1 firing, pooled projectile reuse, and operational weapon-data assets without regressing the completed movement motor.

## Requirement Confirmation

Purger is the game's cybernetic super-soldier protagonist: a century-old living weapon whose synthetic body and modular warfare systems justify systemic upgrades, physical augmentation, and weapon transmutation during the run.

The week-one controller must preserve the core GDD constraint of independent 360-degree aim. Movement remains side-view and lateral, while a dedicated Pivot rotates toward the mouse cursor in world space without coupling to locomotion direction, facing, or jump state.

For the MVP, character presentation now uses a simplified side-flip animation system: side idle and side walk clips are driven by movement speed, and the Body sprite flips horizontally based on mouse position relative to the player. The authored 8-directional assets remain available for later polish once the broader combat and encounter loop is stable.

Week 2 must build on that verified motor by keeping movement and aiming independent while layering in continuous fire, pooled projectile lifecycle management, and data-driven weapon tuning through ScriptableObjects.

With Week 2 finalized, the project can now move into enemy behavior and encounter management while reusing the completed player motor and combat stack.

## Completed Tasks

- Implementing Firing System.
- Finalizing continuous primary fire from the verified 360-degree aim pivot.
- Making projectile Object Pooling operational for runtime reuse.
- Activating the ScriptableObject architecture for weapon attributes through WeaponData and the BaseWeapon asset.
- Refining projectile collisions to use Ground LayerMasks and resetting pooled projectile state at spawn time.

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
- Week 3 has started with the Arkano Scout AI state machine and the WaveManager spawn foundation.
- The WaveManager foundation is now correctly serialized for inspector-driven enemy prefab, spawn interval, and spawn-point assignment.
- New Week 3 scripts are ready to be staged for the next commit.
- Combat Loop V1 is now active: projectiles apply WeaponData damage through a shared damage contract, Arkano Scouts destroy on zero health through EnemyHealth, and both the player and enemies flash on hit while player contact damage uses brief iFrames and knockback.
- The Arkano Scout now operates as a flying melee/contact enemy, using kinematic Rigidbody2D movement toward the player in both axes and trigger-based contact damage for the current physics setup.
- The Arkano Scout now runs a telegraph-dash-cooldown combat loop: it warns with a bright dash color, lunges toward the player's last known position, then recovers on cooldown before the next attack window.
- Enemy pursuit is now prioritized over spawn-distance gating: Arkano Scouts immediately reacquire the Player tag when needed, chase with a normalized 360-degree direction vector from any spawn point, and use a warmer orange telegraph for better contrast against the white floor.
- The Red Arkanos are now visually locked to a red idle state, only shifting to the orange dash telegraph during attack windup and execution before explicitly returning to red.
- Purger now has a survival grace period after contact hits: iFrames trigger alpha flicker feedback and knockback so repeated contact does not immediately stack damage.
- Player movement is now temporarily disabled during knockback so the impulse window can resolve under physics before normal locomotion control resumes.
- The game now has a functional health HUD path: `PlayerController` exposes health state and broadcasts updates, and `HealthBarUI` can drive a Unity UI Slider for live health monitoring.
- Knockback is now 360-degree compliant and physics-resetting: the Purger clears existing velocity before the impulse, is pushed directly away from the attacker, and starts iFrame flicker at the impact moment.

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

The project is now in early Week 3 implementation, with enemy behavior and spawn orchestration becoming the active development focus.

Combat Loop V1 (Damage/Death/Feedback) is now active for moment-to-moment encounters.

The Arkano Scout behavior is now aligned with the authored prefab as a flying contact threat optimized for kinematic 2D physics.

Its current difficulty spike comes from a telegraphed dash attack layered on top of the flying melee chase behavior.

Pursuit reliability is now prioritized so scouts spawned from northern anchors or distant points still enter the fight immediately.

Purger survivability now includes a visible post-hit grace window with flicker-based iFrames and knockback separation.

Knockback now temporarily suppresses player-controlled movement so contact hits create real displacement instead of being overwritten by the motor.

Knockback is now fully directional and physics-resetting, so enemy contact produces cleaner separation from any angle.

Health monitoring is now supported through a functional HUD hook-up for the Purger.

The WaveManager is now ready for direct scene assignment through authored spawn points.

## Next Goal

Week 3 - expand enemy behaviors, stabilize wave spawning, and connect the first Level 1 encounter loop.