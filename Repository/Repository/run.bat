cd Debug
start ServerPrototype.exe
cd..
@echo off
cd /d %~dp0
cd "GUI\bin\x86\Debug"
start WpfApp1.exe
cd..
cd..
cd..
cd..
@echo off
cd /d %~dp0
cd "GUI2\bin\x86\Debug"
start GUI2.exe