# Creates a small test process tree that is safe to kill:
#
#   cmd.exe            <- root (its PID is printed below)
#   ├── PING.EXE
#   ├── cmd.exe
#   │   └── PING.EXE
#   └── PING.EXE
#
# (Windows may also show conhost.exe children - that is normal for console programs.)
$commands = 'start /b ping -n 1000 127.0.0.1 & start /b cmd /c ping -n 1000 127.0.0.1 & ping -n 1000 127.0.0.1'
$root = Start-Process -FilePath 'cmd.exe' -ArgumentList "/c $commands" -WindowStyle Minimized -PassThru
Start-Sleep -Milliseconds 500
Write-Host "Test process tree started. Root PID: $($root.Id)"
Write-Host "Try:  ProcTree tree $($root.Id)"
