# Ejemplo de variables de entorno (copiar a set-env.ps1, que NO se sube al repositorio, y ejecutar con ". .\set-env.ps1")
$env:Jwt__Key = "cambiar-por-una-clave-secreta-de-al-menos-32-caracteres"
$env:ConnectionStrings__MiConexion = "Server=(localdb)\MSSQLLocalDB;Database=CoworkingDB;Trusted_Connection=True;TrustServerCertificate=True;"
# Opcional: crea el administrador inicial al arrancar
$env:Seed__AdminEmail = "admin@coworking.cl"
$env:Seed__AdminPassword = "cambiar-esta-clave"
