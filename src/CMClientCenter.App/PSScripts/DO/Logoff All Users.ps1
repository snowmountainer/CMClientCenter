#Requires -Version 5.1
<#
.SYNOPSIS
    Logs off every interactively logged-on user session.

.DESCRIPTION
    The original version parsed quser.exe's text output by a fixed
    character position (.substring(43,2)) to pull out the session ID —
    this breaks the moment a username is longer than expected or the
    column widths shift for any other reason (locale, console width).
    This parses the actual columns by position of the header row instead,
    and skips gracefully if no one is logged on (quser exits non-zero with
    "No User exists for *" when the session list is empty).
#>

$rawOutput = quser 2>$null
if (-not $rawOutput -or $LASTEXITCODE -ne 0) {
    Write-Output 'No interactive user sessions found.'
    return
}

$header = $rawOutput[0]
$idColumnStart = $header.IndexOf('ID')

$sessions = $rawOutput | Select-Object -Skip 1 | ForEach-Object {
    $line = $_
    # SESSIONNAME is blank for disconnected sessions, which shifts every
    # column left by that field's width — so locate ID by searching from
    # where the header says it starts, not a single hardcoded offset.
    if ($line.Length -gt $idColumnStart) {
        $rest = $line.Substring($idColumnStart).TrimStart()
        $sessionId = ($rest -split '\s+')[0]
        if ($sessionId -match '^\d+$') { $sessionId }
    }
}

if (-not $sessions) {
    Write-Output 'No interactive user sessions found.'
    return
}

foreach ($sessionId in $sessions) {
    logoff $sessionId
    Write-Output "Logged off session ID $sessionId"
}
