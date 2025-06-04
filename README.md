# RicKit RDebug

[![openupm](https://img.shields.io/npm/v/com.rickit.rdebug?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rickit.rdebug/)

> 🌐 [中文文档](./README.zh-CN.md)

---

## Introduction

RicKit RDebug is a Unity-based debug panel utility for quickly creating custom runtime debug UIs. By inheriting from the abstract `RDebug` class, you can easily add buttons, input fields, and more for runtime debugging and parameter tweaking.

---

## Features

- One-click creation of a debug panel.
- Supports common controls like buttons and input fields.
- Flexible layout options (vertical/horizontal).
- Customizable button/input field styles (color, font, etc.).
- Designed for Unity MonoBehaviour workflow.

---

## Quick Start

1. Create a new class that inherits from `RDebug` and implement the `OnShow()` method. You can also override properties for customization.

```csharp
using RicKit.RDebug;
using UnityEngine;

public class MyDebugPanel : RDebug
{
    protected override void Awake()
    {
        // Customize styles in Awake
        TextColor = Color.yellow;
        BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        // BgSprite = ... // set a custom background image if desired
        base.Awake();
    }

    protected override void OnShow()
    {
        UsingHorizontalLayoutGroup(() =>
        {
            CreateButton("customBtn", "My Button", () => Debug.Log("Button clicked!"));
            CreateInputField("customInput", "Input", value => Debug.Log($"Input: {value}"));
        });
    }
}
```

---

## API Reference

### Inheritance Point

- `protected abstract void OnShow()`
  - Implement this to define the content of your debug panel.

### Common Methods

- `protected Button CreateButton(string key, string name, UnityAction onClick, int width = 100, int height = 100, int fontSize = 30)`
  - Add a button to the panel.
  - `key`: Unique identifier for the button.
  - `name`: Display text.
  - `onClick`: Callback when button is pressed.

- `protected InputField CreateInputField(string key, string name, UnityAction<string> onValueChanged, int width = 100, int height = 100, int fontSize = 30, string defaultValue = "")`
  - Add an input field.
  - `key`: Unique identifier.
  - `name`: Label text.
  - `onValueChanged`: Callback on text change.

- `protected GameObject CreateLabel(string key, string name, int width = 100, int height = 100, int fontSize = 30)`
  - Add a label (display-only text) to the panel.

- `protected void UsingHorizontalLayoutGroup(Action action, int height = 100)`
  - Group controls horizontally.

- `public void OnHide()`
  - Manually hide the debug panel and clear controls.

### Fields and Properties

- `protected Dictionary<string, GameObject> Components { get; }`
  - Stores references to all created UI elements (buttons, input fields, labels, etc.) with their corresponding keys.

### Style Customization

- `protected Color TextColor { get; set; }`
- `protected Color BgColor { get; set; }`
- `protected Sprite BgSprite { get; set; }`

---

## Notes

- Must be used within a Unity project.
- Attach your custom debug class to a GameObject in your scene.
- Style and layout can be freely customized.

---

## License

Apache License 2.0

---

## Links

- [GitHub Repository](https://github.com/rickytheoldtree/com.rickit.rdebug)
- [OpenUPM Page](https://openupm.com/packages/com.rickit.rdebug/)

---

## Changelog

See [`Assets/RicKit/RDebug/CHANGELOG.md`](Assets/RicKit/RDebug/CHANGELOG.md) for the latest updates.

Recent changes (v1.1.0):
- Refactored the `RDebug` class for more effective UI component management.
- API changes:  
  - All control creation methods (`CreateButton`, `CreateInputField`, etc.) now require a unique `key` parameter as the first argument.
  - Added `CreateLabel` for display-only text.
  - Improved panel clearing and layout group management.
  - Exposed `Components` dictionary for managing and accessing all created UI elements.
