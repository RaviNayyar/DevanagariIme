using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DevanagariIME
{
    /// <summary>
    /// Global keyboard hook to intercept typing and convert ITRANS to Devanagari
    /// </summary>
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private bool _isEnabled = false;
        private StringBuilder _inputBuffer = new StringBuilder();
        private ITRANSTranslator _translator;
        private bool _inConversionMode = false;
        private ConcurrentQueue<Action> _pendingActions = new ConcurrentQueue<Action>();
        private System.Windows.Forms.Timer _actionTimer;
        private volatile bool _cancelPendingActions = false;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (value && !_isEnabled)
                {
                    // Hook is already set in constructor, just enable processing
                    _isEnabled = true;
                    Console.WriteLine("IME processing enabled.");
                }
                else if (!value && _isEnabled)
                {
                    // Don't unhook, just disable processing (so toggle shortcut still works)
                    _isEnabled = false;
                    _inputBuffer.Clear(); // Clear buffer when disabling
                    Console.WriteLine("IME processing disabled.");
                }
            }
        }

        public event EventHandler<string>? StatusChanged;
        public event EventHandler? ToggleRequested;

        private SynchronizationContext? _syncContext;

        public KeyboardHook(ITRANSTranslator translator)
        {
            _translator = translator;
            _proc = HookCallback;
            
            // Capture the synchronization context (should be WindowsFormsSynchronizationContext)
            _syncContext = SynchronizationContext.Current;
            if (_syncContext == null)
            {
                // Create one if not available
                _syncContext = new WindowsFormsSynchronizationContext();
            }
            
            // Create timer to process queued actions
            _actionTimer = new System.Windows.Forms.Timer();
            _actionTimer.Interval = 10; // Check every 10ms
            _actionTimer.Tick += (s, e) => ProcessPendingActions();
            _actionTimer.Start();
            
            // Always set the hook so we can detect Ctrl+Shift+D even when IME is disabled
            try
            {
                _hookID = SetHook(_proc);
                if (_hookID == IntPtr.Zero)
                {
                    Console.WriteLine("Warning: Failed to set keyboard hook for toggle shortcut. Toggle shortcut may not work.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to set keyboard hook for toggle shortcut: {ex.Message}");
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule?.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // Must call CallNextHookEx even if we don't process the message
            if (nCode < 0)
            {
                return CallNextHookEx(_hookID, nCode, wParam, lParam);
            }
            
            if (wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys key = (Keys)vkCode;

                // Check for Ctrl+Shift+D toggle shortcut (always processed, even when disabled)
                if (key == Keys.D)
                {
                    bool ctrlPressed = IsKeyPressed(Keys.LControlKey) || IsKeyPressed(Keys.RControlKey);
                    bool shiftPressed = IsKeyPressed(Keys.LShiftKey) || IsKeyPressed(Keys.RShiftKey);
                    
                    if (ctrlPressed && shiftPressed)
                    {
                        Console.WriteLine("[DEBUG] Ctrl+Shift+D pressed - toggling IME");
                        ToggleRequested?.Invoke(this, EventArgs.Empty);
                        return (IntPtr)1; // Suppress the key
                    }
                }

                // Only process IME conversion logic if enabled
                if (!_isEnabled)
                {
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }

                // Debug: Log key presses (only for letters to avoid spam)
                if (key >= Keys.A && key <= Keys.Z)
                {
                    Console.WriteLine($"[DEBUG] Key pressed: {key}, Buffer length: {_inputBuffer.Length}");
                }

                // Trigger keys: Space, Enter, Tab
                if (key == Keys.Space || key == Keys.Enter || key == Keys.Tab)
                {
                    Console.WriteLine($"[DEBUG] Trigger key pressed: {key}, Buffer: '{_inputBuffer}'");
                    
                    // For Space or Tab: if no buffer, let the key through normally
                    if ((key == Keys.Space || key == Keys.Tab) && _inputBuffer.Length == 0)
                    {
                        Console.WriteLine($"[DEBUG] {key} pressed with empty buffer - letting through");
                        return CallNextHookEx(_hookID, nCode, wParam, lParam);
                    }
                    
                    // For Enter with no buffer: let it through to create new line
                    if (key == Keys.Enter && _inputBuffer.Length == 0)
                    {
                        Console.WriteLine($"[DEBUG] Enter pressed with empty buffer - creating new line");
                        return CallNextHookEx(_hookID, nCode, wParam, lParam);
                    }
                    
                    // We have a buffer, so convert (works for Space, Enter, and Tab)
                    if (_inputBuffer.Length > 0)
                    {
                        // Convert accumulated ITRANS text
                        string itransText = _inputBuffer.ToString();
                        string devanagari = _translator.Translate(itransText);
                        
                        Console.WriteLine($"[DEBUG] Converting: '{itransText}' -> '{devanagari}'");
                        
                        if (itransText != devanagari && !string.IsNullOrEmpty(devanagari))
                        {
                            Console.WriteLine($"[DEBUG] Conversion needed, replacing text...");
                            // Queue the replacement operation (can't call SendInput from hook)
                            string textToReplace = itransText;
                            string replacementText = devanagari;
                            bool isEnterKey = (key == Keys.Enter);
                            
                            // Capture the window handle NOW while we're in the hook (more reliable)
                            IntPtr currentWindow = GetForegroundWindow();
                            Console.WriteLine($"[DEBUG] Captured window handle: {currentWindow}");
                            
                            // Clear buffer immediately to prevent accumulation
                            _inputBuffer.Clear();
                            
                            // Capture variables for closure
                            IntPtr capturedWindow = currentWindow;
                            bool capturedIsEnter = isEnterKey;
                            
                            _pendingActions.Enqueue(() =>
                            {
                                // Check if this action was canceled
                                if (_cancelPendingActions)
                                {
                                    Console.WriteLine("[DEBUG] Conversion action canceled");
                                    return;
                                }
                                
                                try
                                {
                                    _inConversionMode = true;
                                    
                                    // Use SynchronizationContext to post to UI thread (STA required for clipboard)
                                    if (_syncContext != null)
                                    {
                                        _syncContext.Post((state) =>
                                    {
                                        // Check again after posting to UI thread
                                        if (_cancelPendingActions)
                                        {
                                            Console.WriteLine("[DEBUG] Conversion canceled in UI thread");
                                            _inConversionMode = false;
                                            return;
                                        }
                                        
                                        try
                                        {
                                            // Use the window handle captured during the hook
                                            IntPtr hWnd = capturedWindow;
                                            if (hWnd == IntPtr.Zero)
                                            {
                                                // Fallback: get current window
                                                hWnd = GetForegroundWindow();
                                            }
                                            
                                            Thread.Sleep(10); // Wait for hook to return
                                            
                                            // Ensure window is focused
                                            if (hWnd != IntPtr.Zero)
                                            {
                                                SetForegroundWindow(hWnd);
                                                Thread.Sleep(30); // Reduced wait for focus
                                            }
                                            
                                            // Re-focus window once to ensure we have the right window
                                            if (hWnd != IntPtr.Zero)
                                            {
                                                SetForegroundWindow(hWnd);
                                                Thread.Sleep(20); // Reduced wait
                                            }
                                            
                                            // CRITICAL FIX: Both Space and Enter are suppressed, so they never appear on screen
                                            // So we need to:
                                            // 1. Delete only the text (textToReplace.Length characters)
                                            // 2. Paste Devanagari text
                                            // 3. For Space: add space back after paste
                                            // 4. For Enter: add Enter back after paste to create newline
                                            
                                            int charsToDelete;
                                            bool shouldAddSpaceAfter = false;
                                            
                                            if (capturedIsEnter)
                                            {
                                                // For Enter: we suppressed it, so only delete the text
                                                // The Enter never happened, so we're still on the current line
                                                charsToDelete = textToReplace.Length;
                                                Console.WriteLine($"[DEBUG] Enter-triggered: Deleting {charsToDelete} characters (text only, Enter was suppressed)");
                                            }
                                            else
                                            {
                                                // For Space/Tab: we suppressed it, so only delete the text
                                                // We'll add the space back after pasting
                                                charsToDelete = textToReplace.Length;
                                                shouldAddSpaceAfter = true;
                                                Console.WriteLine($"[DEBUG] Space/Tab-triggered: Deleting {charsToDelete} characters (text only, space/tab was suppressed, will add back)");
                                            }
                                            
                                            Console.WriteLine($"[DEBUG] Current window: {hWnd}, ensuring focus before deletion");
                                            
                                            // Ensure focus one more time right before deletion
                                            if (hWnd != IntPtr.Zero)
                                            {
                                                SetForegroundWindow(hWnd);
                                                Thread.Sleep(15);
                                            }
                                            
                                            // Delete only the text (both Space and Enter were suppressed, so they never appeared)
                                            Console.WriteLine($"[DEBUG] Deleting {charsToDelete} characters (text only)");
                                            // Use batch deletion for speed - send all backspaces at once
                                            if (charsToDelete > 0)
                                            {
                                                // Batch backspace operations for speed
                                                string backspaces = new string('{', charsToDelete).Replace("{", "{BACKSPACE}");
                                                SendKeys.SendWait(backspaces);
                                            }
                                            else
                                            {
                                                // Fallback to individual backspaces if batch fails
                                                for (int i = 0; i < charsToDelete; i++)
                                                {
                                                    SendBackspace();
                                                    Thread.Sleep(5); // Minimal delay
                                                }
                                            }
                                            
                                            Thread.Sleep(50); // Reduced wait for backspaces to complete
                                            
                                            // Verify we're still on the right window
                                            if (hWnd != IntPtr.Zero)
                                            {
                                                IntPtr verifyWindow = GetForegroundWindow();
                                                if (verifyWindow != hWnd)
                                                {
                                                    Console.WriteLine($"[DEBUG] WARNING: Window changed after deletion! Expected {hWnd}, got {verifyWindow}");
                                                    SetForegroundWindow(hWnd);
                                                    Thread.Sleep(30);
                                                }
                                            }
                                            
                                            // Final focus check before pasting - critical for correct cursor position
                                            if (hWnd != IntPtr.Zero)
                                            {
                                                SetForegroundWindow(hWnd);
                                                Thread.Sleep(30); // Reduced wait
                                            }
                                            
                                            // Send Devanagari text
                                            Console.WriteLine($"[DEBUG] Sending Devanagari text: '{replacementText}' at cursor position");
                                            Console.WriteLine($"[DEBUG] Window handle before paste: {hWnd}");
                                            SendText(replacementText);
                                            Console.WriteLine($"[DEBUG] Paste completed");
                                            
                                            // For Space/Tab: add the space back (it was suppressed, so we need to add it)
                                            if (shouldAddSpaceAfter)
                                            {
                                                Thread.Sleep(20); // Reduced wait for paste to complete
                                                if (hWnd != IntPtr.Zero)
                                                {
                                                    SetForegroundWindow(hWnd);
                                                    Thread.Sleep(10);
                                                }
                                                SendKeys.SendWait(" "); // Add space back
                                                Console.WriteLine("[DEBUG] Added space after Devanagari text (space was suppressed)");
                                            }
                                            
                                            // For Enter: we suppressed the Enter key, so we need to explicitly add it back
                                            // to create the newline after the Devanagari text and move to the next line
                                            if (capturedIsEnter)
                                            {
                                                Thread.Sleep(30); // Reduced wait for paste to complete
                                                Console.WriteLine($"[DEBUG] Preparing to add Enter after Devanagari text");
                                                
                                                // Re-focus window to ensure Enter goes to the right place
                                                if (hWnd != IntPtr.Zero)
                                                {
                                                    IntPtr currentWindow = GetForegroundWindow();
                                                    if (currentWindow != hWnd)
                                                    {
                                                        Console.WriteLine($"[DEBUG] Window changed! Re-focusing to {hWnd}");
                                                        SetForegroundWindow(hWnd);
                                                        Thread.Sleep(30);
                                                    }
                                                    else
                                                    {
                                                        SetForegroundWindow(hWnd); // Still re-focus to be safe
                                                        Thread.Sleep(15);
                                                    }
                                                }
                                                
                                                // Send Enter to create newline and move to next line
                                                SendKeys.SendWait("{ENTER}");
                                                Console.WriteLine("[DEBUG] Enter-triggered conversion: Added Enter after Devanagari text to create newline");
                                            }
                                            
                                            // Ensure buffer is still clear after conversion
                                            _inputBuffer.Clear();
                                            _inConversionMode = false;
                                            Console.WriteLine($"[DEBUG] Conversion complete! Buffer cleared.");
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine($"[DEBUG] Error in UI thread action: {ex.Message}");
                                            _inConversionMode = false;
                                        }
                                    }, null);
                                    }
                                    else
                                    {
                                        // Fallback if no sync context
                                        Console.WriteLine("[DEBUG] No synchronization context available");
                                        _inConversionMode = false;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[DEBUG] Error in queued action: {ex.Message}");
                                    _inConversionMode = false;
                                }
                            });
                            
                            // For Enter: suppress it during conversion, but we'll add it back after paste
                            // For Space/Tab: suppress it
                            bool isEnter = (key == Keys.Enter);
                            if (isEnter)
                            {
                                // Store that we need to add Enter after conversion
                                string replacementTextFinal = replacementText;
                                // We'll handle Enter in the queued action
                            }
                            
                            // Suppress the trigger key
                            return (IntPtr)1;
                        }
                        else
                        {
                            Console.WriteLine($"[DEBUG] No conversion needed or empty result");
                            _inputBuffer.Clear();
                            
                            // For Enter with no conversion: let it through to create new line
                            if (key == Keys.Enter)
                            {
                                return CallNextHookEx(_hookID, nCode, wParam, lParam);
                            }
                            // For Space/Tab with no conversion: suppress to avoid double space
                            return (IntPtr)1;
                        }
                    }
                }
                // Backspace key - remove last character from buffer
                else if (key == Keys.Back)
                {
                    if (_inputBuffer.Length > 0)
                    {
                        _inputBuffer.Length--; // Remove last character
                        Console.WriteLine($"[DEBUG] Backspace pressed. Buffer now: '{_inputBuffer}'");
                        StatusChanged?.Invoke(this, $"Buffer: {_inputBuffer}");
                    }
                    // Let backspace through normally to delete the character on screen
                }
                // Escape key to clear buffer
                else if (key == Keys.Escape)
                {
                    _inputBuffer.Clear();
                    StatusChanged?.Invoke(this, "Buffer cleared");
                    return (IntPtr)1;
                }
                // Regular character keys
                else if (!_inConversionMode && IsPrintableKey(key))
                {
                    char ch = GetCharFromKey(key);
                    if (ch != '\0')
                    {
                        _inputBuffer.Append(ch);
                        Console.WriteLine($"[DEBUG] Added '{ch}' to buffer. Buffer now: '{_inputBuffer}'");
                        StatusChanged?.Invoke(this, $"Buffer: {_inputBuffer}");
                        // Let the key through normally - it will appear in the document
                        // We'll delete it and replace with Devanagari when Space/Enter is pressed
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] Key {key} did not produce a character");
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private bool IsPrintableKey(Keys key)
        {
            // Check if it's a printable character (letters, numbers, some punctuation)
            return (key >= Keys.A && key <= Keys.Z) ||
                   (key >= Keys.D0 && key <= Keys.D9) ||
                   key == Keys.OemPeriod || key == Keys.Oemcomma ||
                   key == Keys.OemQuestion || key == Keys.OemPipe ||
                   key == Keys.OemMinus || key == Keys.Oemplus ||
                   key == Keys.OemOpenBrackets || key == Keys.OemCloseBrackets ||
                   key == Keys.OemSemicolon || key == Keys.OemQuotes ||
                   key == Keys.OemBackslash || key == Keys.Oemtilde;
        }

        private char GetCharFromKey(Keys key)
        {
            // Get the character from the key, considering shift state
            bool shift = IsKeyPressed(Keys.LShiftKey) || IsKeyPressed(Keys.RShiftKey);
            bool capsLock = ((ushort)GetKeyState(0x14) & 0xffff) != 0; // VK_CAPITAL
            
            if (key >= Keys.A && key <= Keys.Z)
            {
                char ch = (char)('a' + (key - Keys.A));
                if ((shift && !capsLock) || (!shift && capsLock))
                    return char.ToUpper(ch);
                return ch;
            }
            else if (key >= Keys.D0 && key <= Keys.D9)
            {
                char ch = (char)('0' + (key - Keys.D0));
                if (shift)
                {
                    // Handle number row with shift
                    char[] shifted = { ')', '!', '@', '#', '$', '%', '^', '&', '*', '(' };
                    return shifted[key - Keys.D0];
                }
                return ch;
            }
            else
            {
                // Handle other keys
                return MapKeyToChar(key, shift);
            }
        }

        private char MapKeyToChar(Keys key, bool shift)
        {
            return key switch
            {
                Keys.OemPeriod => '.',
                Keys.Oemcomma => ',',
                Keys.OemQuestion => shift ? '?' : '/',
                Keys.OemPipe => shift ? '|' : '\\',
                Keys.OemMinus => shift ? '_' : '-',
                Keys.Oemplus => shift ? '+' : '=',
                Keys.OemOpenBrackets => shift ? '{' : '[',
                Keys.OemCloseBrackets => shift ? '}' : ']',
                Keys.OemSemicolon => shift ? ':' : ';',
                Keys.OemQuotes => shift ? '"' : '\'',
                Keys.OemBackslash => shift ? '|' : '\\',
                Keys.Oemtilde => shift ? '~' : '`',
                _ => '\0'
            };
        }

        private void SendBackspace()
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd != IntPtr.Zero)
            {
                // Use SendKeys for backspace - more reliable and faster
                try
                {
                    SendKeys.SendWait("{BACKSPACE}");
                }
                catch
                {
                    // Fallback to SendMessage if SendKeys fails
                    SendMessage(hWnd, WM_KEYDOWN, (IntPtr)VK_BACK, IntPtr.Zero);
                    SendMessage(hWnd, WM_KEYUP, (IntPtr)VK_BACK, IntPtr.Zero);
                }
            }
        }

        private void SendText(string text)
        {
            // Use clipboard method with SendKeys - more reliable for Windows Forms apps
            try
            {
                IntPtr hWnd = GetForegroundWindow();
                if (hWnd == IntPtr.Zero)
                {
                    Console.WriteLine("[DEBUG] Could not get foreground window");
                    return;
                }
                
                Console.WriteLine($"[DEBUG] Target window handle: {hWnd}");
                
                // Save current clipboard
                string? oldClipboard = null;
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        oldClipboard = Clipboard.GetText();
                    }
                }
                catch { }
                
                // Set new text to clipboard
                try
                {
                    Clipboard.SetText(text);
                    Console.WriteLine($"[DEBUG] Text copied to clipboard: '{text}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Failed to set clipboard: {ex.Message}");
                    return;
                }
                
                // Ensure window is focused
                SetForegroundWindow(hWnd);
                Thread.Sleep(10); // Reduced for speed
                
                // Use SendKeys class - more reliable than manual messages
                try
                {
                    SendKeys.SendWait("^v"); // Ctrl+V
                    Console.WriteLine("[DEBUG] SendKeys.SendWait executed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] SendKeys failed: {ex.Message}, trying SendMessage");
                    
                    // Fallback: Try WM_PASTE
                    IntPtr result = SendMessage(hWnd, WM_PASTE, IntPtr.Zero, IntPtr.Zero);
                    Console.WriteLine($"[DEBUG] WM_PASTE result: {result}");
                    
                    // If still not working, try keyboard messages
                    if (result == IntPtr.Zero)
                    {
                        Console.WriteLine("[DEBUG] Trying keyboard messages via SendMessage");
                        SendMessage(hWnd, WM_KEYDOWN, (IntPtr)VK_CONTROL, IntPtr.Zero);
                        Thread.Sleep(10); // Reduced from 20
                        SendMessage(hWnd, WM_KEYDOWN, (IntPtr)VK_V, IntPtr.Zero);
                        Thread.Sleep(10);
                        SendMessage(hWnd, WM_KEYUP, (IntPtr)VK_V, IntPtr.Zero);
                        Thread.Sleep(10);
                        SendMessage(hWnd, WM_KEYUP, (IntPtr)VK_CONTROL, IntPtr.Zero);
                    }
                }
                
                Thread.Sleep(50); // Reduced from 200
                
                // Restore clipboard if it existed
                if (oldClipboard != null)
                {
                    try
                    {
                        Thread.Sleep(50);
                        Clipboard.SetText(oldClipboard);
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        Clipboard.Clear();
                    }
                    catch { }
                }
                
                Console.WriteLine($"[DEBUG] Paste operation completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Clipboard method failed: {ex.Message}");
            }
        }

        private void SendChar(char c)
        {
            // Use Unicode input for Devanagari characters
            INPUT[] inputs = new INPUT[2];
            
            // Key down
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = (ushort)c,
                    dwFlags = KEYEVENTF_UNICODE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };
            
            // Key up
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = (ushort)c,
                    dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };
            
            uint result = SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
            if (result == 0)
            {
                Console.WriteLine($"[DEBUG] SendChar failed for '{c}' (U+{(int)c:X4}): {Marshal.GetLastWin32Error()}");
            }
            System.Threading.Thread.Sleep(5);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        private static extern bool GetCaretPos(out System.Drawing.Point lpPoint);

        private const uint WM_CHAR = 0x0102;
        private const uint WM_PASTE = 0x0302;
        private const int VK_BACK = 0x08;
        private const int VK_CONTROL = 0x11;
        private const int VK_V = 0x56;

        private bool IsKeyPressed(Keys key)
        {
            return (GetAsyncKeyState((int)key) & 0x8000) != 0;
        }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUT
        {
            [FieldOffset(0)]
            public int type;
            [FieldOffset(4)]
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private void ProcessPendingActions()
        {
            // Clear any canceled actions
            while (_pendingActions.TryDequeue(out Action? action))
            {
                if (!_cancelPendingActions)
                {
                    action();
                    break; // Process one at a time
                }
                // If canceled, just skip this action
            }
            _cancelPendingActions = false; // Reset flag after processing
        }

        public void Dispose()
        {
            _actionTimer?.Stop();
            _actionTimer?.Dispose();
            IsEnabled = false;
        }
    }
}


