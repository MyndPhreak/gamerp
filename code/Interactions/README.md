# Interaction System

A complete interaction system for S&Box with world-space UI prompts and hold-to-interact functionality, similar to GMod's 2D3D system.

## Features

- ✅ World-space UI rendering (2D3D equivalent)
- ✅ Circular "hold to interact" progress indicator
- ✅ Instant interact or timed hold interactions
- ✅ Configurable interaction distance
- ✅ Billboard rendering (UI faces camera)
- ✅ Customizable UI position and scale
- ✅ Raycast-based detection from camera
- ✅ Event-driven architecture

## Components

### 1. Interactable
Makes a GameObject interactable with a world-space UI prompt.

**Properties:**
- `InteractionText` - Text displayed on the prompt
- `InteractionDistance` - Max distance for interaction (default: 200)
- `HoldDuration` - Time to hold interact key in seconds (0 = instant)
- `PanelOffset` - Local position offset for the UI panel
- `PanelScale` - Scale multiplier for the panel
- `Billboard` - Whether panel faces the camera
- `PromptColor` - Color tint for the prompt

**Events:**
- `OnInteract` - Called when interaction completes

### 2. InteractionManager
Handles raycasting and input detection. Attach to player or camera.

**Properties:**
- `Camera` - Camera to raycast from (null = Scene.Camera)
- `MaxRaycastDistance` - Max raycast distance (default: 500)
- `InteractButton` - Input action name (default: "use")
- `ShowDebug` - Show debug visualization

### 3. InteractionPromptPanel
WorldPanel component that renders the UI in 3D space.

### 4. InteractionPrompt.razor
Razor component for the actual UI with circular progress.

## Setup Instructions

### Step 1: Add InteractionManager to Player

1. Open your scene in the S&Box editor
2. Find your player GameObject (or RPPlayer)
3. Add component: `InteractionManager`
4. **IMPORTANT:** Set the `Camera` property to your player's CameraComponent
5. Optionally enable `ShowDebug` to see raycasts

### Step 2: Create a Test Interactable Prefab

**Option A: Using the Editor (Recommended)**

1. Create a new GameObject in your scene
2. Add a `ModelRenderer` component:
   - Set Model to `models/dev/box.vmdl` or any model
   - Set Tint to a color (e.g., blue)
3. Add an `Interactable` component:
   - Set `InteractionText` to "Press E to interact"
   - Set `HoldDuration` to 1.0 (1 second hold)
   - Set `InteractionDistance` to 300
   - Adjust `PanelOffset` (try `0, 0, 80` for above the object)
   - Set `PanelScale` to 0.3
4. Add a `TestInteractable` component:
   - Set `ModelRenderer` to the ModelRenderer component
   - Set `NormalColor` to blue
   - Set `InteractedColor` to green
5. Position the GameObject in front of your spawn point
6. Save as prefab: Right-click GameObject → "Save as Prefab"

**Option B: Manually Create Prefab JSON**

Create a file `Assets/prefabs/test_interactable.prefab` with the following content:

```json
{
  "GameObjects": [
    {
      "Name": "TestInteractable",
      "Position": "0,0,50",
      "Enabled": true,
      "Components": [
        {
          "__type": "ModelRenderer",
          "Model": "models/dev/box.vmdl",
          "Tint": "0,0.5,1,1"
        },
        {
          "__type": "Interactable",
          "__enabled": true,
          "InteractionText": "Press E to interact",
          "InteractionDistance": 300,
          "HoldDuration": 1,
          "PanelOffset": "0,0,80",
          "PanelScale": 0.3,
          "Billboard": true
        },
        {
          "__type": "TestInteractable",
          "__enabled": true,
          "NormalColor": "0,0,1,1",
          "InteractedColor": "0,1,0,1"
        }
      ]
    }
  ]
}
```

### Step 3: Place Interactable in Scene

1. Drag the prefab from Assets into your scene
2. Position it in front of your player spawn (e.g., 200 units forward)
3. Save the scene

### Step 4: Test In-Game

1. Start the game
2. Walk up to the test object
3. Look at it - you should see the interaction prompt appear
4. Hold **E** key for 1 second - circular progress fills up
5. When complete, the object changes color (blue → green)
6. Interact again to toggle back

## Usage Example

### Simple Instant Interact

```csharp
using GameRP.Interactions;

public class Door : Component
{
    private Interactable _interactable;

    protected override void OnStart()
    {
        _interactable = Components.Get<Interactable>();
        _interactable.InteractionText = "Open Door";
        _interactable.HoldDuration = 0f; // Instant
        _interactable.OnInteract += OpenDoor;
    }

    private void OpenDoor()
    {
        Log.Info("Door opened!");
        // Your door logic here
    }
}
```

### Hold to Interact (e.g., Hacking)

```csharp
using GameRP.Interactions;

public class HackableTerminal : Component
{
    private Interactable _interactable;

    protected override void OnStart()
    {
        _interactable = Components.Get<Interactable>();
        _interactable.InteractionText = "Hack Terminal";
        _interactable.HoldDuration = 3f; // 3 seconds
        _interactable.PromptColor = Color.Red;
        _interactable.OnInteract += OnHacked;
    }

    private void OnHacked()
    {
        Log.Info("Terminal hacked!");
        // Your hack logic here
    }
}
```

### Dynamic Interaction Text

```csharp
private void OnInteracted()
{
    _isLocked = !_isLocked;

    if (_isLocked)
    {
        _interactable.InteractionText = "Unlock";
        _interactable.PromptColor = Color.Red;
    }
    else
    {
        _interactable.InteractionText = "Lock";
        _interactable.PromptColor = Color.Green;
    }
}
```

## Customization

### Adjust Panel Position

The `PanelOffset` is in local space relative to the GameObject:
- `(0, 0, 80)` - Above the object
- `(0, 50, 0)` - To the side
- `(0, 0, -50)` - Below

### Change Panel Size

Adjust `PanelScale`:
- `0.1` - Very small
- `0.3` - Default
- `0.5` - Large
- `1.0` - Very large

### Disable Billboard

Set `Billboard = false` to lock the panel to the object's rotation instead of facing the camera.

## Styling

Edit `code/Interactions/InteractionPrompt.razor.scss` to customize:
- Background color/opacity
- Border radius
- Key button appearance
- Progress ring color
- Text styling

## Troubleshooting

**"No interaction prompt appears"**
- Ensure InteractionManager is attached to player
- Check Camera property is set on InteractionManager
- Verify Interactable component is enabled
- Check InteractionDistance is large enough

**"Progress ring doesn't fill"**
- Ensure HoldDuration > 0
- Check that you're holding the interact key (E by default)
- Verify InteractionManager has correct InteractButton setting

**"Panel is in wrong position"**
- Adjust PanelOffset values
- Try increasing PanelScale if panel is too small to see
- Enable ShowDebug on InteractionManager to see raycast

**"Can't interact from far away"**
- Increase InteractionDistance on Interactable
- Increase MaxRaycastDistance on InteractionManager

## File Structure

```
code/Interactions/
├── Interactable.cs              - Main interactable component
├── InteractionManager.cs        - Handles raycasting/input
├── InteractionPromptPanel.cs    - WorldPanel wrapper
├── InteractionPrompt.razor      - UI component
├── InteractionPrompt.razor.scss - Styling
├── Examples/
│   └── TestInteractable.cs      - Example implementation
└── README.md                    - This file
```

## Credits

Based on S&Box WorldPanel system and inspired by GMod's 2D3D panels.

For more information, see:
- [S&Box WorldPanel API](https://sbox.game/dev/api/Sandbox.UI.WorldPanel)
- [S&Box Developer Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3595903475)
