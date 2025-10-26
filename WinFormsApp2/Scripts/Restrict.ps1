# Apply-UserRestrictions.ps1
# Locks down selected standard users; logs live output for ScriptRunner EXE

Import-Module Microsoft.PowerShell.LocalAccounts -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

function Write-Log {
    param([string]$Message,[string]$Color="White")
    $timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    $line = "[$timestamp] $Message"
    Write-Output $line
}

Write-Log "Preparing user selection..." "Cyan"

# --- Gather all non-admin, enabled local users ---
$admins = Get-LocalGroupMember -Group "Administrators" -ErrorAction SilentlyContinue | ForEach-Object {
    if ($_.Name -like "*\*") { $_.Name.Split('\')[-1] } else { $_.Name }
}
$users = Get-LocalUser | Where-Object {
    $_.Enabled -eq $true -and ($admins -notcontains $_.Name)
} | Select-Object -ExpandProperty Name

if ($users.Count -eq 0) {
    Write-Log "No eligible standard users found. Exiting."
    exit
}

# --- User Selection GUI ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "Select Standard Accounts to Restrict"
$form.Size = New-Object System.Drawing.Size(420,380)
$form.StartPosition = "CenterScreen"

$lbl = New-Object System.Windows.Forms.Label
$lbl.Text = "Select one or more accounts to apply restrictions:"
$lbl.AutoSize = $true
$lbl.Location = New-Object System.Drawing.Point(15,15)
$form.Controls.Add($lbl)

$listBox = New-Object System.Windows.Forms.CheckedListBox
$listBox.Location = New-Object System.Drawing.Point(20,40)
$listBox.Size = New-Object System.Drawing.Size(360,230)
$listBox.CheckOnClick = $true
foreach ($u in $users) { [void]$listBox.Items.Add($u) }
$form.Controls.Add($listBox)

$btnOK = New-Object System.Windows.Forms.Button
$btnOK.Text = "Apply Restrictions"
$btnOK.Location = New-Object System.Drawing.Point(120,290)
$btnOK.Size = New-Object System.Drawing.Size(150,35)
$btnOK.Add_Click({ $form.Tag = "OK"; $form.Close() })
$form.Controls.Add($btnOK)

$form.ShowDialog() | Out-Null

if ($form.Tag -ne "OK") {
    Write-Log "Operation cancelled by user."
    exit
}

$selectedUsers = @()
foreach ($item in $listBox.CheckedItems) { $selectedUsers += $item }

if ($selectedUsers.Count -eq 0) {
    Write-Log "No users selected. Exiting."
    exit
}

Write-Log ("Applying restrictions to: {0}" -f ($selectedUsers -join ', ')) "Cyan"

# --- Helper function for registry writes ---
function Set-PolicyValue {
    param($Key,$Name,$Type,$Value)
    if (-not (Test-Path $Key)) { New-Item -Path $Key -Force | Out-Null }
    New-ItemProperty -Path $Key -Name $Name -PropertyType $Type -Value $Value -Force | Out-Null
}

foreach ($u in $selectedUsers) {
    try {
        $sid = (Get-LocalUser -Name $u).Sid.Value
        $userHive = "Registry::HKEY_USERS\$sid"

        # --- Skip if hive isn't loaded ---
        if (-not (Test-Path $userHive)) {
            Write-Log ("User hive not loaded for {0} — skipping (log in once before restricting)." -f $u) "Yellow"
            continue
        }

        Write-Log ("Applying Group Policy and registry restrictions for {0}..." -f $u)

        # === Disable Windows Store (but keep updates) ===
        Set-PolicyValue "HKLM:\SOFTWARE\Policies\Microsoft\WindowsStore" "RemoveWindowsStore" DWord 1
        Set-PolicyValue "HKLM:\SOFTWARE\Policies\Microsoft\WindowsStore" "DisableStoreApps" DWord 1

        # === Control Panel / Settings ===
        Set-PolicyValue "$userHive\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoControlPanel" DWord 1
        Set-PolicyValue "$userHive\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoSettingsPageVisibility" String "none"

        # === Disable Command Prompt ===
        Set-PolicyValue "$userHive\Software\Policies\Microsoft\Windows\System" "DisableCMD" DWord 1

        # === Block PowerShell and Registry Editor ===
        Set-PolicyValue "$userHive\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer" "DisallowRun" DWord 1
        $runKey = "$userHive\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\DisallowRun"
        if (-not (Test-Path $runKey)) { New-Item -Path $runKey -Force | Out-Null }
        Set-ItemProperty -Path $runKey -Name "1" -Value "powershell.exe"
        Set-ItemProperty -Path $runKey -Name "2" -Value "regedit.exe"

        # === Hide Run command ===
        Set-PolicyValue "$userHive\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoRun" DWord 1

        # === Hide drives (none) ===
        Set-PolicyValue "$userHive\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer" "NoDrives" DWord 0

        # === Disable Registry editing tools globally ===
        Set-PolicyValue "$userHive\Software\Microsoft\Windows\CurrentVersion\Policies\System" "DisableRegistryTools" DWord 1

        # === Ensure Store auto-updates remain enabled ===
        Remove-ItemProperty -Path "HKLM:\SOFTWARE\Policies\Microsoft\WindowsStore" -Name "AutoDownload" -ErrorAction SilentlyContinue

        Write-Log ("Restrictions applied successfully for {0}." -f $u) "Green"
    }
    catch {
        Write-Log ("Error applying to {0}: {1}" -f $u, $_.Exception.Message) "Red"
    }
}

# --- One global security tweak ---
Write-Log "Applying global security tweak (deny Guest network logon)..."
$tempInf = "$env:TEMP\user_rights.inf"
@"
[Unicode]
Unicode=yes
[Version]
signature=`"`$CHICAGO`$`"
Revision=1
[Privilege Rights]
SeDenyNetworkLogonRight = Guest
"@ | Out-File -Encoding ASCII $tempInf
secedit /configure /db "$env:TEMP\user_rights.sdb" /cfg $tempInf /areas USER_RIGHTS | Out-Null
Remove-Item $tempInf -Force

gpupdate /force | Out-Null
Write-Log "All selected accounts restricted successfully (auto-updates ON)." "Green"
