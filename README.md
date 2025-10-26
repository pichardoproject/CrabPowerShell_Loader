Help Desk Shell – Windows 11 Pro 24H2

A C# WinForms-based utility for managing and locking down local Windows accounts using PowerShell automation.
Designed to centralize Help Desk administrative actions (setup, restriction, and undo) inside a single signed .exe with live log output and watermark overlay.

📂 Project Structure
HelpDeskShell/
│
├─ crab.png                  ← baked-in watermark
├─ Scripts/                  ← folder copied alongside .exe
│   ├─ Setup.ps1             ← creates admin accounts
│   ├─ Undo.ps1              ← restores default state
│   ├─ Restrict.ps1          ← applies user restrictions
│   └─ Fix-OtherUser.ps1     ← restores “Other user” visibility
│
├─ Logs/                     ← created at runtime, holds execution logs
├─ MainForm.cs               ← main UI logic
├─ Program.cs                ← entry point
└─ HelpDeskShell.csproj


Note: Only crab.png is embedded as a resource.
All PowerShell scripts must remain external in the Scripts folder beside the compiled .exe.

Compilation

Open the .csproj in Visual Studio 2022 or later.
Set build configuration to Release.
Build → output appears under
bin\Release\net8.0-windows\HelpDeskShell.exe.

Copy these to the deployment folder:

HelpDeskShell.exe
crab.png
/Scripts

Example layout:

D:\HelpDeskShell\
    ├─ HelpDeskShell.exe
    ├─ crab.png
    └─ Scripts\
         Setup.ps1
         Undo.ps1
         Restrict.ps1
         Fix-OtherUser.ps1

🔑 PowerShell Script Syntax Rules
1️⃣ Use Write-Output for Logging

All script messages must use Write-Output (not Write-Host) so they appear in the EXE log window.
Use a helper:

function Write-Log {
    param([string]$Message, [string]$Color = "White")
    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    Write-Output "[$timestamp] $Message"
}


❌ Write-Host will not be captured.
✅ Write-Output is parsed live by the C# process.

2️⃣ No Interactive Input Prompts

Scripts must not use Read-Host or Pause.
If confirmation is required, use a WinForms MessageBox:

Add-Type -AssemblyName System.Windows.Forms
$dialog = [System.Windows.Forms.MessageBox]::Show(
    "Proceed with action?",
    "Confirm",
    [System.Windows.Forms.MessageBoxButtons]::YesNo,
    [System.Windows.Forms.MessageBoxIcon]::Question
)
if ($dialog -ne [System.Windows.Forms.DialogResult]::Yes) { Write-Log "Cancelled"; exit }

3️⃣ Strings and Variables

Use ASCII quotes and safe formatting:

"$var"
("Value: {0}" -f $var)

Avoid Word “smart quotes” or long dashes — they cause parse errors when run from the EXE.

4️⃣ Correct Here-String Syntax
@"
[Unicode]
Unicode=yes
[Version]
signature=`"`$CHICAGO`$`"
Revision=1
[Privilege Rights]
SeDenyNetworkLogonRight = Guest
"@ | Out-File -Encoding ASCII "$env:TEMP\user_rights.inf"

✅ Close "@ on its own line.
✅ Escape $CHICAGO with backticks.

5️⃣ Proper Block Balancing

Every try {} must have a catch {}.
Ensure all braces {}, parentheses (), and quotes " are properly closed.

💡 Execution Flow

The EXE launches PowerShell elevated if required.

Runs scripts using:

powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File "Script.ps1"

Captures StandardOutput and StandardError live.
Writes logs both to the on-screen console and /Logs/ folder.

🧾 Example Log Output
[2025-10-26 07:54:00] === APPLY RESTRICTIONS ===
[2025-10-26 07:54:02] Applying Group Policy and registry restrictions for testuser...
[2025-10-26 07:54:03] Restrictions applied successfully for testuser.
[2025-10-26 07:54:04] All selected accounts restricted successfully (auto-updates ON).

⚠️ Common Causes of Script Failure
Problem	Cause	Fix
[ERROR] Unexpected token	Smart quotes or Word formatting	Save file as UTF-8 (no BOM)
Script hangs	Read-Host or Pause used	Remove interactive input
[ERROR] The string is missing the terminator: "	Unclosed quotes or here-string	Verify matching "@
[ERROR] Missing closing '}'	Unbalanced braces	Re-indent and verify block pairs

🪟 Tested Environment

Windows 11 Pro 24H2 (Build 26100+)
PowerShell 5.1 & 7.4
.NET 8.0 Windows Desktop Runtime
Run context: Elevated Administrator

🧑‍💻 Credits

Developer: Stewart Pichardo
Frontend & Log Engine: GPT-5 Assisted Build
Automation Scripts: Help Desk Admin Toolkit
Generated 2025-10-26
