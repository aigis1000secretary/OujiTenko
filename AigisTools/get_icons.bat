C:
cd C:\Linebot\AigisTools

@ECHO off
SET PATH=%~dp0Utilities\Lua 5.3;%~dp0Utilities\cURL\bin;%~dp0Utilities\GraphicsMagick;%PATH%
SET LUA_PATH=%~dp0Scripts\?.lua
SET LUA_PATH_5_3=%~dp0Scripts\?.lua

start do get_file.lua ico_00.aar
pause
start do get_file.lua ico_01.aar
pause
start do get_file.lua ico_02.aar
pause
start do get_file.lua ico_03.aar