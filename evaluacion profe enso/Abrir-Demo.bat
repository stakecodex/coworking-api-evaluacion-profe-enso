@echo off
cd /d "%~dp0"
rem Si la API no esta encendida, la enciende en otra ventana
curl -s -o nul http://localhost:5002/ || (
  echo Encendiendo la API...
  start "Coworking API" cmd /k dotnet run --launch-profile http
  timeout /t 12 /nobreak >nul
)
start chrome http://localhost:5002/demo.html
