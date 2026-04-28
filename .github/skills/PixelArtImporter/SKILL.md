---
name: PixelArtImporter
description: 'Standardizes texture import settings for 8-bit/16-bit pixel art. Use when: importing a new sprite, configuring a texture, setting up a sprite sheet, or any operation that touches TextureImporter settings in a pixel-art Unity project.'
argument-hint: 'Asset path(s) to configure, e.g. Assets/_Sprites/Enemy/foo.png'
---

# PixelArtImporter

## When to Use

- Importing a new sprite or sprite sheet into the project.
- Re-configuring an existing texture that has incorrect filter, compression, or PPU settings.
- Setting up a multi-frame sprite sheet before slicing.
- Any `manage_texture` or `TextureImporter` operation in this project.

## Required Settings

| Property | Required Value | Override |
|---|---|---|
| `textureType` | `Sprite (2D and UI)` | Never |
| `filterMode` | `Point (no filter)` | Never — bilinear/trilinear blurs pixel art |
| `textureCompression` | `None (Uncompressed)` | Never — compression introduces artifacts on pixel art |
| `spritePixelsPerUnit` | `16` | Only when the task explicitly states a different PPU |
| `spriteImportMode` | `Single` for single sprites, `Multiple` for sheets | Set per task |
| `generateMipMaps` | `false` | Never enable for 2D pixel art |

## Procedure

### Step 1 — Locate the Asset

```csharp
// Verify the asset exists and get its current import settings
string path = "Assets/_Sprites/YourSprite.png";
var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
return "Current: filterMode=" + imp.filterMode + " PPU=" + imp.spritePixelsPerUnit
     + " compression=" + imp.textureCompression + " spriteMode=" + imp.spriteImportMode;
```

### Step 2 — Apply Standard Settings

Use `manage_texture` (preferred) or `execute_code` as fallback:

```csharp
string path = "Assets/_Sprites/YourSprite.png";
var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);

imp.textureType           = UnityEditor.TextureImporterType.Sprite;
imp.filterMode            = UnityEngine.FilterMode.Point;
imp.textureCompression    = UnityEditor.TextureImporterCompression.Uncompressed;
imp.spritePixelsPerUnit   = 16f;
imp.mipmapEnabled         = false;
// imp.spriteImportMode   = UnityEditor.SpriteImportMode.Single; // or Multiple for sheets

UnityEditor.EditorUtility.SetDirty(imp);
imp.SaveAndReimport();
return "Reimported: " + path;
```

### Step 3 — Verify

```csharp
string path = "Assets/_Sprites/YourSprite.png";
var imp = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Texture2D>(path);
return "filter=" + imp.filterMode
     + " | PPU=" + imp.spritePixelsPerUnit
     + " | compression=" + imp.textureCompression
     + " | size=" + tex.width + "x" + tex.height
     + " | mips=" + imp.mipmapEnabled;
```

Expected output: `filter=Point | PPU=16 | compression=None | mips=False`

### Step 4 — Sprite Sheet (if Multiple mode)

After applying settings, slice the sheet using `ISpriteEditorDataProvider` via reflection (Unity 6 requirement — `TextureImporter.spritesheet` API is silently ignored):

> Load the `PixelArtSlicer` helper or use the reflection-based slicing pattern established in this project.

## Quality Criteria

- [ ] `filterMode` is `Point` — **never** Bilinear or Trilinear.
- [ ] `textureCompression` is `None` — no DXT/ETC artifacts.
- [ ] `spritePixelsPerUnit` matches task spec (default **16**).
- [ ] `mipmapEnabled` is `false`.
- [ ] Asset reimported successfully (no console errors after `SaveAndReimport`).
- [ ] Sub-assets verified if sprite sheet (`Multiple` mode).
