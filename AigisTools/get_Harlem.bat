@ECHO off
SET PATH=%~dp0Utilities\Lua 5.3;%~dp0Utilities\cURL\bin;%~dp0Utilities\GraphicsMagick;%PATH%
SET LUA_PATH=%~dp0Scripts\?.lua
SET LUA_PATH_5_3=%~dp0Scripts\?.lua

lua Scripts\get_file_list.lua

lua Scripts\get_file.lua paev03.aar

move /Y .\out\files\paev03.aar .\out\Harlem\paev03.aar

pause