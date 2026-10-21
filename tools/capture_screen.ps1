Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Stop-Process -Name GrumphySwarm -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

$proc = Start-Process -FilePath 'c:\Users\SANTHOSH\Documents\antigravity\focused-darwin\publish\win-x64\GrumphySwarm.exe' -PassThru
Start-Sleep -Seconds 2

# Move mouse to top center to trigger expand
[System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point(768, 10)
Start-Sleep -Seconds 1

# Capture screenshot of the top area (1536 x 350)
$bmp = New-Object System.Drawing.Bitmap(1536, 350)
$gfx = [System.Drawing.Graphics]::FromImage($bmp)
$gfx.CopyFromScreen(0, 0, 0, 0, $bmp.Size)
$bmp.Save('c:\Users\SANTHOSH\Documents\antigravity\focused-darwin\current_screenshot.png', [System.Drawing.Imaging.ImageFormat]::Png)
$gfx.Dispose()
$bmp.Dispose()

Write-Host "Screenshot captured successfully."
