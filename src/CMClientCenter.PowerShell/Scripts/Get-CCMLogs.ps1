# Get-CCMLogs.ps1 — PS 5.1 compatible
# Parameters: $LogName (filename without path), $MaxLines (default 200)

$ccmLogPath = "$env:WinDir\CCM\Logs"
$logFile    = Join-Path $ccmLogPath $LogName

if (-not (Test-Path $logFile)) {
    # Fallback: CCMSetup logs
    $logFile = Join-Path "$env:WinDir\ccmsetup\Logs" $LogName
    if (-not (Test-Path $logFile)) {
        [PSCustomObject]@{ Error = "Log not found: $logFile" }
        return
    }
}

# Read the last N lines
$lines = Get-Content -Path $logFile -Tail $MaxLines -ErrorAction SilentlyContinue
if ($lines -eq $null) { $lines = @() }

# Parse CMTrace format: <![LOG[Message]LOG]!><time="..." date="..." component="..." type="1|2|3">
# type: 1=Info, 2=Warning, 3=Error
$entries = [System.Collections.Generic.List[PSCustomObject]]::new()

foreach ($line in $lines) {
    if (-not $line) { continue }

    $msg       = $line
    $time      = ""
    $component = ""
    $severity  = "Info"

    # CMTrace format
    if ($line -match '\<!\[LOG\[(.*)?\]LOG\]!\>.*time="([^"]+)".*date="([^"]+)".*component="([^"]+)".*type="(\d)"') {
        $msg       = $matches[1]
        $timeStr   = $matches[2]
        $dateStr   = $matches[3]
        $component = $matches[4]
        $typeNum   = $matches[5]

        # Combine date + time
        try {
            $dt   = [datetime]::ParseExact("$dateStr $($timeStr.Split('.')[0])", "MM-dd-yyyy HH:mm:ss", $null)
            $time = $dt.ToString("dd.MM.yyyy HH:mm:ss")
        } catch {
            $time = "$dateStr $timeStr"
        }

        $severity = switch ($typeNum) {
            "2" { "Warning" }
            "3" { "Error"   }
            default { "Info" }
        }
    }

    $entries.Add([PSCustomObject]@{
        Time      = $time
        Component = $component
        Severity  = $severity
        Message   = $msg.Trim()
    })
}

# Newest first
$entries.Reverse()
$entries.ToArray()
