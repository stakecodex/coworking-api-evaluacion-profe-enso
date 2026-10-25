@echo off
title Coworking API
cd /d "%~dp0"
echo Iniciando la API... (cierra esta ventana para detenerla)
echo Cuando diga "Now listening", abre Acceso-Usuarios.bat
dotnet run --launch-profile http
pause
