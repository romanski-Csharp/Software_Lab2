@echo off
rem Builds both programs into the "out" folder: out\MaskWords.exe and out\ProcTree.exe
dotnet publish MaskWords\MaskWords.csproj -c Release -o out
dotnet publish ProcTree\ProcTree.csproj -c Release -o out
copy /Y MaskWords\test\input.txt out\input.txt >nul
copy /Y ProcTree\spawn-test-tree.bat out\spawn-test-tree.bat >nul
copy /Y ProcTree\spawn-test-tree.ps1 out\spawn-test-tree.ps1 >nul
echo.
echo Done. Go to the "out" folder:  cd out
