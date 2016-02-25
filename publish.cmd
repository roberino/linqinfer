SET APIKEY=%1

FOR /f %%i IN ('dir artifacts\*.nupkg /o:d /b') DO set LAST=%%i

ECHO Publishing %LAST%
.nuget\nuget push artifacts\%LAST% -ApiKey %APIKEY%
PAUSE