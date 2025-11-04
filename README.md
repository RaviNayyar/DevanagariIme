# Devanagari IME - System-Wide Input Method Editor

A Windows system-wide Input Method Editor (IME) that converts ITRANS transliteration to Devanagari script in real-time across all applications.

## Features

- **System-wide IME**: Works in any Windows application (Word, Notepad, browsers, etc.)
- **Real-time conversion**: Type ITRANS text and press Space/Enter to convert to Devanagari
- **System tray control**: Easy enable/disable via system tray icon
- **Comprehensive ITRANS support**: Handles vowels, consonants, matras, conjuncts, and special characters

## Usage

### Running the IME

1. **Start the IME** (system tray mode - default):
   ```bash
   dotnet run
   ```
   Or run the compiled executable:
   ```bash
   .\bin\Debug\net6.0-windows\DevanagariIME.exe
   ```

2. **Enable the IME**:
   - Right-click the system tray icon (द icon)
   - Select "Enable IME"
   - Or double-click the tray icon to toggle

3. **Type in any application**:
   - Type ITRANS text (e.g., `namaste`)
   - Press **Space**, **Enter**, or **Tab** to convert to Devanagari (नमस्ते)
   - Press **Escape** to clear the input buffer

### Other Modes

- **Test mode**: Run test suite
  ```bash
  dotnet run --test
  ```

- **Interactive mode**: Console-based translator
  ```bash
  dotnet run --interactive
  ```

## How It Works

1. The IME uses a global keyboard hook to intercept typing
2. Typed characters are accumulated in a buffer
3. When you press Space/Enter/Tab, the buffer is converted from ITRANS to Devanagari
4. The typed text is deleted and replaced with Devanagari characters

## System Tray Menu

- **Enable IME**: Activate the input method
- **Disable IME**: Deactivate (stops intercepting keyboard)
- **About**: Show information about the IME
- **Exit**: Close the application

## ITRANS Examples

- `namaste` → नमस्ते
- `maiM hindii bol saktaa huuM.` → मैं हिन्दी बोल सकता हूँ।
- `tum kahaaM jaa rahe ho?` → तुम कहाँ जा रहे हो?
- `raamaayaNa` → रामायण

## Requirements

- Windows 10/11
- .NET 6.0 Runtime
- Administrator privileges (may be required for global keyboard hooks)

## Building

```bash
dotnet build
```

## Technical Details

- Uses Windows low-level keyboard hooks (WH_KEYBOARD_LL)
- Converts ITRANS to Devanagari using Unicode input
- System tray application built with Windows Forms
- Works across all Windows applications

## Notes

- The IME starts **disabled** by default - enable it from the system tray menu
- For security, some applications may block global keyboard hooks
- The IME requires focus in the target application to work properly

