# Get-HardwareInfo.ps1 — PS 5.1 kompatibel

try {
    $cs   = Get-CimInstance -ClassName Win32_ComputerSystem    -ErrorAction Stop
    $bios = Get-CimInstance -ClassName Win32_BIOS               -ErrorAction Stop
    $cpu  = Get-CimInstance -ClassName Win32_Processor          -ErrorAction Stop | Select-Object -First 1
    $os   = Get-CimInstance -ClassName Win32_OperatingSystem    -ErrorAction Stop
    $gpu  = Get-CimInstance -ClassName Win32_VideoController    -ErrorAction SilentlyContinue | Select-Object -First 1

    # RAM-Slots
    $ramModules = Get-CimInstance -ClassName Win32_PhysicalMemory -ErrorAction SilentlyContinue
    $ramSlots   = @()
    if ($ramModules -ne $null) {
        $ramSlots = $ramModules | ForEach-Object {
            [PSCustomObject]@{
                Slot         = [string]$_.DeviceLocator
                SizeGB       = [math]::Round($_.Capacity / 1GB, 0)
                SpeedMHz     = [string]$_.Speed
                Manufacturer = [string]$_.Manufacturer
            }
        }
    }

    # Disks
    $disks    = Get-CimInstance -ClassName Win32_LogicalDisk -Filter "DriveType=3" -ErrorAction SilentlyContinue
    $diskList = @()
    if ($disks -ne $null) {
        $diskList = $disks | ForEach-Object {
            [PSCustomObject]@{
                DriveLetter = [string]$_.DeviceID
                Label       = [string]$_.VolumeName
                TotalGB     = [math]::Round($_.Size / 1GB, 1)
                FreeGB      = [math]::Round($_.FreeSpace / 1GB, 1)
                FreePct     = [math]::Round($_.FreeSpace / $_.Size * 100, 0)
                FileSystem  = [string]$_.FileSystem
            }
        }
    }

    # Netzwerk-Adapter
    $nics    = Get-CimInstance -ClassName Win32_NetworkAdapterConfiguration -ErrorAction SilentlyContinue |
               Where-Object { $_.IPEnabled -eq $true }
    $nicList = @()
    if ($nics -ne $null) {
        $nicList = $nics | ForEach-Object {
            $ip = ""
            if ($_.IPAddress -ne $null -and $_.IPAddress.Count -gt 0) { $ip = $_.IPAddress[0] }
            [PSCustomObject]@{
                Description = [string]$_.Description
                IPAddress   = $ip
                MACAddress  = [string]$_.MACAddress
            }
        }
    }

    # UBR (Update Build Revision) aus Registry — VOR dem Hash-Literal berechnen
    $ubrReg = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion" `
                  -Name "UBR" -ErrorAction SilentlyContinue
    $osBuildFull = [string]$os.BuildNumber
    if ($ubrReg -ne $null -and $ubrReg.UBR) {
        $osBuildFull = "$($os.BuildNumber).$($ubrReg.UBR)"
    }

    # BIOS Datum
    $biosDate = ""
    if ($bios.ReleaseDate) { $biosDate = $bios.ReleaseDate.ToString("dd.MM.yyyy") }

    # OS Zeiten
    $osInstall = ""
    $lastBoot  = ""
    if ($os.InstallDate)    { $osInstall = $os.InstallDate.ToString("dd.MM.yyyy") }
    if ($os.LastBootUpTime) { $lastBoot  = $os.LastBootUpTime.ToString("dd.MM.yyyy HH:mm") }

    # GPU
    $gpuName   = ""
    $gpuVramMB = 0
    if ($gpu) { $gpuName = [string]$gpu.Name; $gpuVramMB = [math]::Round($gpu.AdapterRAM / 1MB, 0) }

    [PSCustomObject]@{
        Manufacturer = [string]$cs.Manufacturer
        Model        = [string]$cs.Model
        SerialNumber = [string]$bios.SerialNumber
        BIOSVersion  = [string]$bios.SMBIOSBIOSVersion
        BIOSDate     = $biosDate
        CPUName      = $cpu.Name.Trim()
        CPUCores     = [int]$cpu.NumberOfCores
        CPULogical   = [int]$cpu.NumberOfLogicalProcessors
        CPUSocket    = [string]$cpu.SocketDesignation
        CPUMaxMHz    = [int]$cpu.MaxClockSpeed
        TotalRAMGB   = [math]::Round($cs.TotalPhysicalMemory / 1GB, 0)
        RAMSlots     = $ramSlots
        GPUName      = $gpuName
        GPUVRAMMB    = $gpuVramMB
        OSCaption    = [string]$os.Caption
        OSBuild      = $osBuildFull
        OSArch       = [string]$os.OSArchitecture
        OSInstall    = $osInstall
        LastBoot     = $lastBoot
        Disks        = $diskList
        NICs         = $nicList
    }
}
catch {
    [PSCustomObject]@{
        Manufacturer = "Fehler"; Model = $_.Exception.Message
        SerialNumber = ""; BIOSVersion = ""; BIOSDate = ""
        CPUName = ""; CPUCores = 0; CPULogical = 0; CPUSocket = ""; CPUMaxMHz = 0
        TotalRAMGB = 0; RAMSlots = @()
        GPUName = ""; GPUVRAMMB = 0
        OSCaption = ""; OSBuild = ""; OSArch = ""; OSInstall = ""; LastBoot = ""
        Disks = @(); NICs = @()
    }
}
