# Sistema de Reservas para Coworking

Trabajo 2 de ConstrucciÃ³n de Software. Es una API para administrar las salas de reuniones y los escritorios de un espacio de coworking.

## Â¿QuÃ© hace?
- Los **clientes** se registran, ven los espacios disponibles, hacen reservas y revisan su historial.
- El **administrador** crea y edita los espacios, aprueba reservas y puede cancelar cualquiera.

## CÃ³mo funcionan las reservas
- Una reserva nueva queda como *Pendiente*.
- El administrador la pasa a *Confirmada*, o se cancela (*Cancelada*).
- No se puede reservar un espacio que ya estÃ© ocupado a esa hora, ni en fechas pasadas, ni por mÃ¡s de 8 horas.
- El total se calcula segÃºn las horas y el precio por hora del espacio.

## Base de datos
Uso la base de datos que autorizÃ³ el profesor (`reservas_db_joserodriguez`). Cada llamada a la API queda anotada en la tabla de auditorÃ­a.

## CÃ³mo probarlo
1. Abrir el proyecto en Visual Studio y ejecutarlo (o hacer doble clic en `Abrir-Coworking.bat`).
2. Se abre la pÃ¡gina de Swagger, donde se pueden probar todas las funciones.
3. Iniciar sesiÃ³n con `POST /api/v1/User/login`, copiar el token y pegarlo en el botÃ³n *Authorize*.

Las claves y la conexiÃ³n a la base de datos no estÃ¡n en el repositorio. Hay un ejemplo de cÃ³mo configurarlas en `set-env.example.ps1`.

## Pruebas
Con `dotnet test` se ejecutan las 30 pruebas automÃ¡ticas.

