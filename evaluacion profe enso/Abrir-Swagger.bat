@echo off
title Coworking API
cd /d "%~dp0"
echo Iniciando la API... (cierra esta ventana para detenerla)
start "" /b cmd /c "timeout /t 12 /nobreak >nul & start chrome http://localhost:5002/swagger"
dotnet run --launch-profile http
pause
