# Devanagari IME - System-Wide Input Method Editor

A Windows system-wide Input Method Editor (IME) that converts ITRANS transliteration to Devanagari script in real-time across all applications.

## Features

- **System-wide IME**: Works in any Windows application (Word, Notepad, browsers, etc.)
- **Real-time conversion**: Type ITRANS text and press Space/Enter to convert to Devanagari
- **Keyboard shortcut**: Toggle IME on/off with **Ctrl+Shift+D**
- **System tray control**: Easy enable/disable via system tray icon
- **Comprehensive ITRANS support**: Handles vowels, consonants, matras, conjuncts, and special characters
- **Auto-start on login**: Optional Task Scheduler integration for automatic startup

## Installation

### Option 1: Installer (Recommended)

1. Download `DevanagariIME-Setup.exe` from the `installer` folder
2. Run the installer and follow the setup wizard
3. The installer will:
   - Install the application to `C:\Program Files\Devanagari IME\`
   - Optionally set up Task Scheduler for automatic startup
   - Create shortcuts in the Start Menu
   - Register the application for easy uninstallation

### Option 2: Manual Installation

1. Build the application (see [Building](#building) section)
2. Run `install-task-scheduler.ps1` to set up automatic startup:
   ```powershell
   .\install-task-scheduler.ps1
   ```
   Or run the executable manually:
   ```bash
   .\bin\Release\net6.0-windows\win-x64\publish\DevanagariIME.exe
   ```

## Usage

### Starting the IME

The IME runs as a system tray application. After installation, it will:
- Start automatically if Task Scheduler was configured during installation
- Appear as a **द** icon in the system tray
- Start **disabled** by default (you must enable it first)

### Enabling the IME

You can enable the IME in two ways:

1. **System Tray Menu**:
   - Right-click the system tray icon (द icon)
   - Select "Enable IME"
   - Or double-click the tray icon to toggle

2. **Keyboard Shortcut**: Press **Ctrl+Shift+D** to toggle IME on/off

### Typing in Any Application

1. Type ITRANS text (e.g., `namaste`)
2. Press **Space**, **Enter**, or **Tab** to convert to Devanagari (नमस्ते)
3. Press **Escape** to clear the input buffer

### Other Modes

For development and testing:

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
- **About**: Show information about the IME and keyboard shortcuts
- **Exit**: Close the application

## Keyboard Shortcuts

- **Ctrl+Shift+D**: Toggle IME on/off
- **Space/Enter/Tab**: Convert ITRANS text to Devanagari
- **Escape**: Clear the input buffer

## ITRANS Examples

- `namaste` → नमस्ते
- `maiM hindii bol saktaa huuM.` → मैं हिन्दी बोल सकता हूँ।
- `tum kahaaM jaa rahe ho?` → तुम कहाँ जा रहे हो?
- `raamaayaNa` → रामायण

## Requirements

- **Windows 10/11**
- **Administrator privileges** (may be required for global keyboard hooks and Task Scheduler setup)
- **.NET 6.0 SDK** (only needed for building from source; the installer includes the runtime)

## Building

### Development Build

```bash
dotnet build
```

### Building the Installer

To create a self-contained installer:

```powershell
.\build-installer.ps1
```

This will:
1. Build and publish a self-contained application (includes .NET runtime)
2. Compile the Inno Setup installer
3. Create `installer\DevanagariIME-Setup.exe`

**Note**: Requires [Inno Setup 6](https://jrsoftware.org/isdl.php) to be installed.

## Technical Details

- Uses Windows low-level keyboard hooks (WH_KEYBOARD_LL)
- Converts ITRANS to Devanagari using Unicode input
- System tray application built with Windows Forms
- Works across all Windows applications

## Uninstallation

### Using the Installer

1. Go to **Settings** → **Apps** → **Installed apps**
2. Find "Devanagari IME"
3. Click **Uninstall**

Or use the uninstaller shortcut created during installation:
- **Start Menu** → **Devanagari IME** → **Uninstall Devanagari IME**

### Manual Uninstallation

If you installed manually:

1. Remove the Task Scheduler task:
   ```powershell
   .\install-task-scheduler.ps1 -Uninstall
   ```

2. Delete the application folder
3. Remove Start Menu shortcuts (if any)

## Troubleshooting

### IME Not Working

- Make sure the IME is **enabled** (check system tray icon)
- Some applications may block global keyboard hooks for security
- The IME requires focus in the target application to work properly
- Try toggling the IME off and on using **Ctrl+Shift+D** or the tray menu

### Multiple Instances

The application prevents multiple instances from running. If you see duplicate tray icons:
1. Close all instances from the system tray
2. Restart the application

### Task Scheduler Not Starting

If the IME doesn't start automatically:
1. Open **Task Scheduler** (`taskschd.msc`)
2. Find the "DevanagariIME" task
3. Check if it's enabled and run manually if needed
4. Verify the executable path is correct

## Notes

- The IME starts **disabled** by default - enable it from the system tray menu or using **Ctrl+Shift+D**
- For security, some applications may block global keyboard hooks
- The IME requires focus in the target application to work properly
- The installer is self-contained and includes the .NET runtime (no separate installation needed)

