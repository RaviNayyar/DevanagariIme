using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace DevanagariIME
{
    class Program
    {
        // Global flag to control console output
        public static bool EnableConsoleOutput { get; private set; } = false;
        
        [STAThread]
        static void Main(string[] args)
        {
            // Check what mode we're running in
            bool runTests = args.Length > 0 && (args[0] == "--test" || args[0] == "-t" || args[0] == "test");
            bool runInteractive = args.Length > 0 && (args[0] == "--interactive" || args[0] == "-i" || args[0] == "interactive");
            
            // Only enable console output for interactive/test modes, not for tray app mode
            EnableConsoleOutput = runTests || runInteractive;
            
            // Only set console encoding if we have a console and want console output
            if (EnableConsoleOutput)
            {
                try
                {
                    if (!Console.IsOutputRedirected)
                    {
                        Console.OutputEncoding = Encoding.UTF8;
                    }
                }
                catch (IOException)
                {
                    // No console available - this is fine
                }
            }
            else
            {
                // For tray app mode, free the console if it was allocated
                // This prevents a console window from appearing
                try
                {
                    var handle = GetConsoleWindow();
                    if (handle != IntPtr.Zero)
                    {
                        FreeConsole();
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
            
            ITRANSTranslator translator = new ITRANSTranslator();
            
            // Only check admin privileges for IME mode (not interactive/test modes)
            if (!runTests && !runInteractive && EnableConsoleOutput)
            {
                // Check for administrator privileges
                bool isAdmin = IsRunningAsAdministrator();
                if (!isAdmin)
                {
                    Console.WriteLine("WARNING: Not running as administrator.");
                    Console.WriteLine("Keyboard hooks may not work properly without admin privileges.");
                    Console.WriteLine("If the IME doesn't work, try running as administrator.");
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("Running with administrator privileges.");
                    Console.WriteLine();
                }
            }

            if (runTests)
            {
                RunTests(translator);
            }
            else if (runInteractive)
            {
                RunInteractive(translator);
            }
            else
            {
                // Run as system tray IME
                try
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    
                    // Only show console messages if console output is enabled
                    if (EnableConsoleOutput)
                    {
                        Console.WriteLine("Devanagari IME is running.");
                        Console.WriteLine("Look for the tray icon (द) in your system tray.");
                        Console.WriteLine("Right-click the icon to enable the IME.");
                        Console.WriteLine("Press Ctrl+C or close this window to exit.");
                    }
                    
                    Application.Run(new IMETrayApp());
                }
                catch (Exception ex)
                {
                    string errorMsg = $"Error starting Devanagari IME:\n\n{ex.Message}";
                    if (ex.InnerException != null)
                    {
                        errorMsg += $"\n\nInner Exception: {ex.InnerException.Message}";
                    }
                    errorMsg += $"\n\nStack Trace:\n{ex.StackTrace}";
                    
                    MessageBox.Show(errorMsg, 
                        "Devanagari IME Error", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                }
            }
        }

        static void RunInteractive(ITRANSTranslator translator)
        {
            Console.WriteLine(new string('=', 60));
            Console.WriteLine("ITRANS to Devanagari Interactive Translator");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine("Type ITRANS text and press Enter to see Devanagari output");
            Console.WriteLine("Type 'quit', 'exit', or 'q' to stop");
            Console.WriteLine("Type 'test' to run test suite");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine();

            while (true)
            {
                try
                {
                    Console.Write("ITRANS: ");
                    string? userInput = Console.ReadLine();
                    
                    if (userInput == null)
                        break;

                    userInput = userInput.Trim();

                    if (string.IsNullOrEmpty(userInput))
                        continue;

                    if (userInput.ToLower() == "test")
                    {
                        Console.WriteLine();
                        RunTests(translator);
                        Console.WriteLine();
                        continue;
                    }

                    if (userInput.ToLower() == "quit" || userInput.ToLower() == "exit" || userInput.ToLower() == "q")
                    {
                        Console.WriteLine("Goodbye!");
                        break;
                    }

                    string result = translator.Translate(userInput);
                    Console.WriteLine($"Devanagari: {result}");
                    Console.WriteLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine();
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsole();

        private static bool IsRunningAsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        static void RunTests(ITRANSTranslator translator)
        {
            // Test cases with expected outputs
            var testCases = new Dictionary<string, string>
            {
                { "namaste", "नमस्ते" },
                { "namaskaar", "नमस्कार" },
                { "bharat", "भारत" },
                { "aaiye", "आइये" },
                { "main", "मैं" },
                { "tum", "तुम" },
                { "ham", "हम" },
                { "raam", "राम" },
                { "shri", "श्री" },
                { "devanaagarii", "देवनागरी" },
                // Wikipedia test cases
                { "vikipiiDiyaa", "विकिपीडिया" },
                { "bhaarat", "भारत" },
                { "lakShmaN", "लक्ष्मण" },
                { "iNDiyaa", "इण्डिया" },
                { "maiM hindii meM Taaip kar saktaa huuM |", "मैं हिन्दी में टाइप कर सकता हूँ।" },
                { "aao hindii meM kampyuuTar pe likheM", "आओ हिन्दी में कम्प्यूटर पे लिखें" },
                // Additional test cases
                { "maiM hindii bol saktaa huuM.", "मैं हिन्दी बोल सकता हूँ।" },
                { "tum kahaaM jaa rahe ho?", "तुम कहाँ जा रहे हो?" },
                { "yah sundar pustak hai.", "यह सुन्दर पुस्तक है।" },
                { "raamaayaNa eka mahaan mahaakaavya hai.", "रामायण एक महान महाकाव्य है।" },
                { "shikShaa sab ke lie aavashyak hai.", "शिक्षा सब के लिए आवश्यक है।" }
            };

            Console.WriteLine("ITRANS to Devanagari Translator Test\n");
            Console.WriteLine(new string('=', 70));
            Console.WriteLine($"{"Input",-40} {"Expected",-20} {"Got",-20} {"Status"}");
            Console.WriteLine(new string('=', 70));
            
            int passed = 0;
            int failed = 0;
            
            foreach (var testCase in testCases)
            {
                string input = testCase.Key;
                string expected = testCase.Value;
                string result = translator.Translate(input);
                
                // Truncate long inputs for display
                string displayInput = input.Length > 35 ? input.Substring(0, 32) + "..." : input;
                string displayExpected = expected.Length > 18 ? expected.Substring(0, 15) + "..." : expected;
                string displayGot = result.Length > 18 ? result.Substring(0, 15) + "..." : result;
                
                string status = result == expected ? "✓" : "✗";
                if (result == expected)
                    passed++;
                else
                    failed++;
                
                Console.WriteLine($"{displayInput,-40} {displayExpected,-20} {displayGot,-20} {status}");
                
                if (result != expected)
                {
                    Console.WriteLine($"  -> Mismatch! Expected: {expected}");
                    Console.WriteLine($"     Got:      {result}");
                }
            }
            
            Console.WriteLine(new string('=', 70));
            Console.WriteLine($"Passed: {passed}, Failed: {failed}, Total: {testCases.Count}");
        }
    }
}
