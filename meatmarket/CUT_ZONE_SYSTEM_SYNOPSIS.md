# Cut Zone and Body Part Tree System - Synopsis

## Overview

The dismemberment system uses a **virtual hierarchy** of body segments tracked by `BodyPartTree`, with physical `CutZone` components placed on 3D models at joint locations. When all required cuts are made around a segment, it becomes "fully isolated" and is automatically deposited to the tray.

**Key Concept**: The system does NOT physically separate mesh geometry. Instead, it tracks which joints have been cut and deposits parts when they're logically separated from the body.

---

## Core Components

### 1. **BodyPartTree** (MonoBehaviour)
- **Location**: Attached to the **root GameObject** of the plushie model
- **Purpose**: Manages the virtual segment hierarchy, tracks cut states, detects isolation
- **Required Setup**:
  - Set `species` field (Cat, Dog, or Bunny)
  - Auto-finds `DepositTrayController` at runtime (or assign manually)

### 2. **CutZone** (MonoBehaviour)
- **Location**: Attached to **child GameObjects** at joint locations (shoulders, elbows, wrists, hips, knees, ankles, neck, torso middle)
- **Purpose**: Defines where cuts can be made and what joint they represent
- **Required Components**: Must have a `Collider` (used for raycast detection)
- **Required Fields**:
  - `limb`: Which limb this cut belongs to (LeftArm, RightArm, LeftLeg, RightLeg, None)
  - `section`: Which joint type (ShoulderOrHip, ElbowOrKnee, WristOrAnkle, Neck, TorsoMiddle)
  - `precision`: Cut quality variant (Perfect, MissNorth, MissSouth)

### 3. **CutRouter** (MonoBehaviour)
- **Location**: Attached to the **root GameObject** (same as BodyPartTree)
- **Purpose**: Routes cut events from CutZones to BodyPartTree
- **Required Setup**: Wire `OnAnyCutEnter` event to `BodyPartTree.HandleCut()` in Inspector

---

## Body Segment Hierarchy

The system recognizes **15 body segments** organized in a tree structure:

```
Upper_Torso (ROOT)
├── Head
├── Lower_Torso
│   ├── L_Upper_Leg
│   │   ├── L_Lower_Leg
│   │   │   └── L_Foot
│   └── R_Upper_Leg
│       ├── R_Lower_Leg
│       │   └── R_Foot
├── L_Upper_Arm
│   ├── L_Forearm
│   │   └── L_Hand
└── R_Upper_Arm
    ├── R_Forearm
    │   └── R_Hand
```

**Important**: `Upper_Torso` is the root segment. It has no parent cut and can be isolated when all its child cuts are made.

---

## Cut Zone Mapping Rules

CutZones are automatically mapped to segments based on their `(limb, section)` combination:

| Limb | Section | Maps To Segment | Parent Segment |
|------|---------|----------------|----------------|
| `None` | `Neck` | Head | Upper_Torso |
| `None` | `TorsoMiddle` | Lower_Torso | Upper_Torso |
| `LeftArm` | `ShoulderOrHip` | L_Upper_Arm | Upper_Torso |
| `LeftArm` | `ElbowOrKnee` | L_Forearm | L_Upper_Arm |
| `LeftArm` | `WristOrAnkle` | L_Hand | L_Forearm |
| `RightArm` | `ShoulderOrHip` | R_Upper_Arm | Upper_Torso |
| `RightArm` | `ElbowOrKnee` | R_Forearm | R_Upper_Arm |
| `RightArm` | `WristOrAnkle` | R_Hand | R_Forearm |
| `LeftLeg` | `ShoulderOrHip` | L_Upper_Leg | Lower_Torso |
| `LeftLeg` | `ElbowOrKnee` | L_Lower_Leg | L_Upper_Leg |
| `LeftLeg` | `WristOrAnkle` | L_Foot | L_Lower_Leg |
| `RightLeg` | `ShoulderOrHip` | R_Upper_Leg | Lower_Torso |
| `RightLeg` | `ElbowOrKnee` | R_Lower_Leg | R_Upper_Leg |
| `RightLeg` | `WristOrAnkle` | R_Foot | R_Lower_Leg |

---

## Cut Precision Variants

Each joint location can have **multiple CutZone variants** for different cut qualities:

- **Perfect**: Ideal cut placement
- **MissNorth**: Cut slightly too high/forward
- **MissSouth**: Cut slightly too low/backward

**Important**: When ANY variant of a joint is cut, ALL variants are marked as cut. The system matches CutZones by `(limb, section)` only, not precision. However, the precision of the actual cut hit determines the part quality.

**Quality Rules**:
- If ANY cut on a segment was Perfect → part quality = Perfect
- Otherwise → part quality = Normal

---

## Isolation Rules

A segment becomes **fully isolated** (ready for deposit) when:

1. **Root Segment (Upper_Torso)**:
   - ✅ All child cuts are made (no parent cut required)

2. **Non-Root Segments**:
   - ✅ Parent cut is made (separates from parent)
   - ✅ All child cuts are made (all children separated)

**Example**: To isolate `L_Hand`:
- ✅ Parent cut: `LeftArm` + `WristOrAnkle` (separates hand from forearm)
- ✅ Child cuts: None (hand has no children)
- Result: Hand is deposited

**Example**: To isolate `L_Forearm`:
- ✅ Parent cut: `LeftArm` + `ElbowOrKnee` (separates forearm from upper arm)
- ✅ Child cut: `LeftArm` + `WristOrAnkle` (hand must be separated first)
- Result: Forearm is deposited

---

## Model Setup Requirements

### Hierarchy Structure

```
PlushieRoot (GameObject)
├── BodyPartTree (Component)
├── CutRouter (Component)
├── [Visual Mesh Objects]
├── CutZone_Neck (GameObject)
│   └── Collider (Component)
│   └── CutZone (Component: limb=None, section=Neck, precision=Perfect)
├── CutZone_LeftArm_Shoulder (GameObject)
│   └── Collider (Component)
│   └── CutZone (Component: limb=LeftArm, section=ShoulderOrHip, precision=Perfect)
├── CutZone_LeftArm_Shoulder_MissNorth (GameObject)
│   └── Collider (Component)
│   └── CutZone (Component: limb=LeftArm, section=ShoulderOrHip, precision=MissNorth)
├── CutZone_LeftArm_Shoulder_MissSouth (GameObject)
│   └── Collider (Component)
│   └── CutZone (Component: limb=LeftArm, section=ShoulderOrHip, precision=MissSouth)
└── [Repeat for all joints...]
```

### Required CutZones Per Model

**Minimum Required** (one precision variant per joint):
- Neck (1): `limb=None, section=Neck`
- Torso Middle (1): `limb=None, section=TorsoMiddle`
- Left Arm: Shoulder (1), Elbow (1), Wrist (1)
- Right Arm: Shoulder (1), Elbow (1), Wrist (1)
- Left Leg: Hip (1), Knee (1), Ankle (1)
- Right Leg: Hip (1), Knee (1), Ankle (1)

**Total**: 13 CutZones minimum (one per joint)

**Recommended** (three precision variants per joint):
- Each joint has Perfect, MissNorth, and MissSouth variants
- **Total**: 39 CutZones (13 joints × 3 precisions)

### CutZone Placement Guidelines

1. **Collider Setup**:
   - Each CutZone GameObject must have a Collider (Box, Sphere, or Capsule)
   - Collider should be sized to cover the joint area
   - Collider should be positioned at the joint location
   - `isTrigger` can be true or false (system uses raycast detection)

2. **Positioning**:
   - Place CutZones at the **exact joint location** where cuts should occur
   - Perfect variant: Center of the joint
   - MissNorth: Slightly above/forward of perfect
   - MissSouth: Slightly below/backward of perfect

3. **Naming Convention** (optional, for organization):
   - `CutZone_Neck_Perfect`
   - `CutZone_LeftArm_Shoulder_Perfect`
   - `CutZone_LeftArm_Shoulder_MissNorth`
   - `CutZone_LeftArm_Shoulder_MissSouth`
   - etc.

### Component Configuration

**BodyPartTree**:
- `species`: Set to match the model (Cat, Dog, or Bunny)
- `depositTray`: Leave empty (auto-finds) or assign manually
- `logSegmentStates`: Enable for debugging

**CutRouter**:
- `bodyType`: String identifier (e.g., "CAT", "DOG", "BUNNY")
- Wire `OnAnyCutEnter` → `BodyPartTree.HandleCut()` in Inspector

**CutZone** (each instance):
- `limb`: Select from dropdown (LeftArm, RightArm, LeftLeg, RightLeg, None)
- `section`: Select from dropdown (ShoulderOrHip, ElbowOrKnee, WristOrAnkle, Neck, TorsoMiddle)
- `precision`: Select from dropdown (Perfect, MissNorth, MissSouth)
- `useTriggerCallbacks`: Usually false (system uses raycast)
- `debugLog`: Enable for debugging

---

## How Cuts Are Detected

1. Player clicks with a tool (cleaver, etc.)
2. `RaycastClickManager` performs a raycast
3. If raycast hits a CutZone's collider → `CutZone.NotifyRaycastHit()` is called
4. CutZone creates a `CutContext` and fires `OnCutEnter` event
5. `CutRouter` receives event and routes to `BodyPartTree.HandleCut()`
6. `BodyPartTree` finds all matching CutZones (by limb + section) and marks them as cut
7. System checks all segments for isolation
8. Isolated segments are automatically deposited to the tray

---

## Important Rules for Model Modifiers

1. **CutZones must be children of the root GameObject** (where BodyPartTree is attached)
2. **Each joint needs at least one CutZone** (Perfect variant recommended minimum)
3. **CutZone colliders must be positioned at joint locations** (not on mesh surfaces)
4. **CutZone fields (limb, section, precision) must be set correctly** - the system relies on these for mapping
5. **Multiple precision variants are optional** but recommended for gameplay variety
6. **The visual mesh geometry does NOT need to be separated** - the system only tracks cut states
7. **CutZones can be inactive GameObjects** - they'll be found but won't be cuttable until activated
8. **The hierarchy structure doesn't matter** - CutZones can be direct children or nested, as long as they're under the root

---

## Testing Checklist

After setting up a model:

1. ✅ BodyPartTree component on root with correct species
2. ✅ CutRouter component on root with OnAnyCutEnter wired to BodyPartTree.HandleCut
3. ✅ All 13 required joints have at least one CutZone
4. ✅ Each CutZone has correct (limb, section, precision) values
5. ✅ Each CutZone has a Collider component
6. ✅ CutZone colliders are positioned at joint locations
7. ✅ Test cutting each joint to verify isolation works
8. ✅ Verify parts are deposited to tray when isolated

---

## Common Issues

**Problem**: Parts not depositing
- **Solution**: Check that all required cuts are made (parent + all children)

**Problem**: Wrong segment being deposited
- **Solution**: Verify CutZone (limb, section) values match the mapping table

**Problem**: Cuts not registering
- **Solution**: Ensure CutRouter.OnAnyCutEnter is wired to BodyPartTree.HandleCut

**Problem**: Multiple precision variants not working
- **Solution**: All variants must have the same (limb, section) - only precision differs

---

## Summary

The system uses **metadata-driven dismemberment**: CutZones define where cuts can occur, and BodyPartTree tracks the virtual hierarchy. Models don't need to be physically separated - the system handles part isolation and deposit automatically when all required cuts are made.

