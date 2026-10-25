# API de Reservas para Coworking

ASP.NET Core Web API (.NET 10, controladores) + EF Core (SQL Server) + JWT + Swagger. Trabajo 2 – Construcción de Software.

## Caso de negocio
Un coworking administra salas de reuniones y escritorios. Los **clientes** consultan disponibilidad, reservan y ven su historial; el **administrador** crea/gestiona espacios y puede cancelar cualquier reserva.

## Base de datos
Se usa la base autorizada por el docente (`reservas_db_joserodriguez.sql`): `Roles`, `Usuarios`, `CategoriasEspacio`, `Espacios`, `Servicios`, `EspacioServicios`, `Reservas`, `Pagos` y `LogsAuditoria`. El modelo EF Core se mapea a ese esquema y la migración `InitialCreate` lo reproduce.
- Crear la base en el servidor: ejecutar el script del docente (en SQLEXPRESS) o, en una base ya creada, marcar la migración como aplicada.
- En esta máquina se creó en LocalDB: `Server=(localdb)\MSSQLLocalDB;Database=reservas_db_joserodriguez`.
- `RequestLoggingMiddleware` guarda cada llamada `/api` en `LogsAuditoria`.
- Flujo de reserva: el cliente crea la reserva (`Pendiente`), el administrador la aprueba (`Confirmada`) o cualquiera de los dos la cancela (`Cancelada`).

## Estructura
- `Models/`: entidades, `CoworkingDbContext`, `DbSeeder`, `Migrations/`.
- `Repositories/`: patrón Repository (`IEspacioRepository`, `IReservaRepository`, `IUsuarioRepository`).
- `Services/ReservaService`: reglas de negocio (solapamientos, máximo 8 h, fechas futuras, cálculo de total, cancelación).
- `Middleware/`: `RequestLoggingMiddleware` y `ExceptionHandlingMiddleware` (errores en formato ProblemDetails).
- `Controllers/V1/`: `UserController`, `EspaciosController` (CRUD), `ReservasController`.
- `Coworking.Tests/`: pruebas de integración (xUnit + `WebApplicationFactory`).

DI: `DbContext`, repositorios y `ReservaService` son *Scoped*; los middleware se instancian una vez por aplicación.

## Configuración (variables de entorno, no se suben al repo)
Ver `set-env.example.ps1`: `Jwt__Key` (≥ 32 caracteres), `ConnectionStrings__MiConexion`, y opcionalmente `Seed__AdminEmail` / `Seed__AdminPassword`.
Al arrancar se aplican las migraciones y se crean los roles `Administrador` y `Cliente`.

```powershell
. .\set-env.ps1
cd "evaluacion profe enso"
dotnet run
```
Swagger: `/swagger` (usar el botón *Authorize* con el token de `POST /api/v1/User/login`).

## Endpoints (v1)
| Método | Ruta | Acceso |
|---|---|---|
| POST | `/api/v1/User` (registro, rol Cliente) | Público |
| POST | `/api/v1/User/login` | Público |
| GET | `/api/v1/User/me` | Autenticado |
| GET | `/api/v1/Espacios`, `/categorias`, `/{id}`, `/{id}/disponibilidad` | Autenticado |
| POST / PUT / DELETE | `/api/v1/Espacios` | Administrador |
| POST | `/api/v1/Reservas` (queda Pendiente) | Cliente |
| PUT | `/api/v1/Reservas/{id}/confirmar` | Administrador |
| GET | `/api/v1/Reservas/mias`, `/{id}` | Dueño o Administrador |
| GET | `/api/v1/Reservas` | Administrador |
| DELETE | `/api/v1/Reservas/{id}` (cancela) | Dueño o Administrador |

## Pruebas
```powershell
dotnet test
```
