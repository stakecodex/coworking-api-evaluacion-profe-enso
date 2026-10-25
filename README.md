# Sistema de Reservas para Coworking

Trabajo 2 de Construcción de Software. Es una API para administrar las salas de reuniones y los escritorios de un espacio de coworking.

## ¿Qué hace?
- Los **clientes** se registran, ven los espacios disponibles, hacen reservas y revisan su historial.
- El **administrador** crea y edita los espacios, aprueba reservas y puede cancelar cualquiera.

## Cómo funcionan las reservas
- Una reserva nueva queda como *Pendiente*.
- El administrador la pasa a *Confirmada*, o se cancela (*Cancelada*).
- No se puede reservar un espacio que ya esté ocupado a esa hora, ni en fechas pasadas, ni por más de 8 horas.
- El total se calcula según las horas y el precio por hora del espacio.

## Base de datos
Uso la base de datos que autorizó el profesor (`reservas_db_joserodriguez`). Cada llamada a la API queda anotada en la tabla de auditoría.

## Cómo probarlo
1. Encender la API con doble clic en `Abrir-Coworking.bat` (o ejecutando desde Visual Studio).
2. Doble clic en `Acceso-Usuarios.bat`: abre la pantalla del coworking y Swagger en Chrome.
3. En Swagger, iniciar sesión con `POST /api/v1/User/login`, copiar el token y pegarlo en el botón *Authorize*.

Las claves y la conexión a la base de datos no están en el repositorio. Hay un ejemplo de cómo configurarlas en `set-env.example.ps1`.

## Pruebas
Con `dotnet test` se ejecutan las 30 pruebas automáticas.
