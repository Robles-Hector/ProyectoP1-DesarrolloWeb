# Mini Sistema de Renta de Películas - Sakila (ASP.NET Core MVC & ADO.NET)

**Estudiante:** Héctor Robles  
**Asignatura:** Desarrollo Web / Programación de Aplicaciones  
**Proyecto:** Primer Parcial — Mini Sistema de Renta de Películas Sakila  
**Tecnologías:** ASP.NET Core MVC (.NET 10), C#, ADO.NET (`Microsoft.Data.SqlClient`), SQL Server, Razor, Bootstrap 5, Bootstrap Icons  

---

## 📌 Descripción del Proyecto

Aplicación web integral desarrollada bajo el patrón **Modelo-Vista-Controlador (MVC)**, conectada directamente a la base de datos relacional **Sakila** en **SQL Server** mediante **ADO.NET** puro (`Microsoft.Data.SqlClient`) con consultas estrictamente parametrizadas para evitar inyección SQL.

El sistema modela el ciclo de vida completo de un videoclub moderno:
$$\text{film} \longrightarrow \text{inventory} \longrightarrow \text{rental} \longrightarrow \text{customer}$$

---

## 👥 Roles del Sistema y Control de Acceso

El sistema implementa dos roles con interfaces diferenciadas y autorización estricta en el servidor mediante el filtro personalizado `[AuthorizeRole]`:

1. **Administrador (`staff`):**
   - Gestión integral del catálogo de películas (CRUD completo: crear, editar, listar con paginación).
   - Administración física de copias en inventario (`inventory`) por tienda.
   - **Reto 2:** Eliminación lógica de películas (`is_deleted = 1`), permitiendo ocultarlas del catálogo sin afectar alquileres pasados, y reactivarlas cuando sea necesario.
   - Auditoría y supervisión global de los alquileres realizados en el videoclub.
   - Acceso al reporte estadístico de películas más alquiladas.

2. **Cliente (`customer`):**
   - Exploración del catálogo con búsqueda por título, filtro por clasificación (*Rating*) y filtro por disponibilidad (*Disponibles* / *Agotadas*).
   - Consulta de detalles y estado en tiempo real de copias físicas.
   - Alquiler de películas disponibles (el sistema identifica automáticamente al cliente mediante `Session`, sin requerir ingreso manual).
   - Módulo **"Mis Alquileres"**: visualización exclusiva de los préstamos del cliente autenticado.
   - Devolución de películas con validación de propiedad y verificación de estado.

---

## 🚀 Retos Técnicos Implementados

### Reto 1 — Contraseña Protegida (Hashing Criptográfico con Sal)
- Las contraseñas **nunca se almacenan en texto plano**.
- Se implementó la clase `SakilaRentalApp.Utils.PasswordHelper` utilizando el algoritmo estándar de la industria **PBKDF2** (`Rfc2898DeriveBytes.Pbkdf2`) con:
  - Algoritmo pseudoaleatorio: **HMAC-SHA256**.
  - Longitud de sal única por usuario: **128 bits (16 bytes)** generada con `RandomNumberGenerator`.
  - Número de iteraciones: **100,000**.
  - Clave derivada: **256 bits (32 bytes)** codificada en Base64.
- Durante el inicio de sesión, el sistema recupera la sal y el hash almacenados del usuario y computa la derivación fija con `CryptographicOperations.FixedTimeEquals` para mitigar ataques de temporización (*Timing Attacks*).

### Reto 2 — Eliminación Lógica (*Soft Delete*)
- **Entidad seleccionada:** `film` (Película).
- **Columna agregada:** `is_deleted BIT NOT NULL DEFAULT 0`.
- **Justificación técnica:** Una película no puede eliminarse físicamente con `DELETE FROM film` porque violaría la integridad referencial con las tablas dependientes `inventory` y `rental`. Borrar físicamente una película destruiría el historial financiero, las métricas de alquileres pasados y la trazabilidad de auditoría. Con la eliminación lógica, la película permanece intacta en la base de datos pero se oculta automáticamente del flujo normal de navegación para los clientes.

---

## ⚙️ Reglas de Negocio Implementadas

1. **Disponibilidad en tiempo real:**
   Una película no puede alquilarse si todas sus copias físicas en `inventory` se encuentran actualmente en préstamo (alquileres donde `return_date IS NULL`). La aplicación calcula la disponibilidad al instante evaluando:
   $$\text{Copias Disponibles} = \text{Copias en Inventory} - \text{Copias Alquiladas sin Devolución}$$
2. **Propiedad del alquiler:**
   Un cliente únicamente puede registrar la devolución de alquileres asociados a su propio identificador (`customer_id`).
3. **Estado del alquiler:**
   Un alquiler que ya fue devuelto (`return_date IS NOT NULL`) no puede devolverse nuevamente.
4. **Identificación por Sesión:**
   El cliente no ingresa su `customer_id` en ningún formulario; el sistema lo toma de manera segura desde `HttpContext.Session`.
5. **Validación de Datos:**
   Modelos con `DataAnnotations` y validación de `ModelState.IsValid` antes de cualquier operación de persistencia.

---

## 📊 Reporte Estadístico: `/Reportes/PeliculasMasAlquiladas`

Cumpliendo con la especificación del parcial, se construyó el módulo de reporte ejecutado vía ADO.NET con:
`INNER JOIN`, `COUNT`, `GROUP BY`, `ORDER BY` y `TOP (10)` sobre `film ↓ inventory ↓ rental`:

```sql
SELECT TOP (10)
    f.film_id,
    f.title AS pelicula,
    COUNT(r.rental_id) AS cantidad_alquileres,
    f.rating,
    f.release_year,
    f.rental_rate,
    COUNT(DISTINCT i.inventory_id) AS copias_inventario,
    (COUNT(DISTINCT i.inventory_id) - COUNT(DISTINCT CASE WHEN r2.rental_id IS NOT NULL AND r2.return_date IS NULL THEN i.inventory_id END)) AS copias_disponibles
FROM film f
INNER JOIN inventory i ON f.film_id = i.film_id
INNER JOIN rental r ON i.inventory_id = r.inventory_id
LEFT JOIN rental r2 ON i.inventory_id = r2.inventory_id AND r2.return_date IS NULL
GROUP BY f.film_id, f.title, f.rating, f.release_year, f.rental_rate
ORDER BY cantidad_alquileres DESC;
```

---

## 🔑 Cuentas de Acceso Preconfiguradas

| Perfil | Nombre de Usuario | Contraseña | Rol | Asociado a |
| :--- | :--- | :--- | :--- | :--- |
| **Administrador** | `admin` | `Admin123!` | Administrador | `staff_id = 1` (Mike Hillyer) |
| **Cliente 1** | `mary.smith` | `Cliente123!` | Cliente | `customer_id = 1` (Mary Smith) |
| **Cliente 2** | `patricia.johnson` | `Cliente123!` | Cliente | `customer_id = 2` (Patricia Johnson) |

*Nota: También es posible registrar nuevos clientes desde la pantalla de Registro (`/Account/Register`).*

---

## 🛠️ Instrucciones de Ejecución

1. **Requisitos:**
   - .NET SDK 10 o superior (compatible con .NET 8 / 9).
   - SQL Server (LocalDB o SQLEXPRESS) con la base de datos **Sakila** cargada.

2. **Configuración de Conexión:**
   Verifique la cadena de conexión en `SakilaRentalApp/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "SakilaConnection": "Server=localhost\\SQLEXPRESS;Database=sakila;Integrated Security=True;TrustServerCertificate=True;"
   }
   ```

3. **Scripts SQL (Opcional):**
   La aplicación ejecuta automáticamente la clase `DbInitializer` al iniciar, la cual crea la tabla `usuarios`, añade la columna `is_deleted` y siembra los usuarios iniciales con contraseña hasheada. Si desea ejecutar los scripts manualmente en SQL Server Management Studio (SSMS), se encuentran en la carpeta `Scripts/`:
   - `Scripts/01_alter_sakila_film_softdelete.sql`
   - `Scripts/02_create_usuarios_table.sql`
   - `Scripts/03_seed_usuarios.sql`
   - `Scripts/04_consultas_exploracion.sql`

4. **Compilar y Ejecutar:**
   ```bash
   cd SakilaRentalApp
   dotnet restore
   dotnet build
   dotnet run
   ```
   Abra su navegador en la dirección indicada en la consola (por ejemplo: `http://localhost:5217`).

---

## 📁 Estructura del Código

```text
Proyecto_P1_Robles_Hector/
│
├── SakilaRentalApp.slnx                    # Archivo de solución Visual Studio
├── README.md                               # Documentación general del proyecto
│
├── Scripts/                                # Scripts SQL para SQL Server
│   ├── 01_alter_sakila_film_softdelete.sql # Reto 2: Eliminación lógica
│   ├── 02_create_usuarios_table.sql        # Creación de tabla usuarios
│   ├── 03_seed_usuarios.sql                # Población de usuarios con PBKDF2
│   └── 04_consultas_exploracion.sql        # 3 consultas exploratorias solicitadas
│
└── SakilaRentalApp/                        # Proyecto ASP.NET Core MVC
    ├── Controllers/                        # Controladores MVC
    │   ├── AccountController.cs            # Login, Logout, Registro, Acceso Denegado
    │   ├── AdminPeliculasController.cs     # CRUD admin, gestión de copias, soft delete
    │   ├── AlquileresController.cs         # Alquilar, Mis Alquileres, Devolución
    │   ├── HomeController.cs               # Dashboard de métricas y bienvenida
    │   ├── PeliculasController.cs          # Catálogo público, búsqueda, detalles
    │   └── ReportesController.cs           # Reporte de películas más alquiladas
    │
    ├── Data/                               # Capa de Acceso a Datos (ADO.NET)
    │   ├── DatabaseHelper.cs               # Factoría y apertura segura de conexiones
    │   ├── DbInitializer.cs                # Inicializador de BD y sembrado automático
    │   ├── FilmRepository.cs               # Consultas parametrizadas de películas
    │   ├── InventoryRepository.cs          # Gestión física de inventario
    │   ├── RentalRepository.cs             # Transacciones de alquiler y devolución
    │   ├── ReporteRepository.cs            # Consultas de agregación para reportes
    │   └── UsuarioRepository.cs            # Validación de contraseñas y usuarios
    │
    ├── Extensions/                         # Métodos de extensión
    │   └── AppSessionExtensions.cs         # Manejo tipado de Session
    │
    ├── Filters/                            # Filtros de Autorización
    │   └── AuthorizeRoleAttribute.cs       # Restricción de URLs y verificación de rol
    │
    ├── Models/                             # Entidades y ViewModels
    │   ├── Film.cs
    │   ├── InventoryItem.cs
    │   ├── Rental.cs
    │   ├── Usuario.cs
    │   └── ViewModels/
    │       ├── FilmCatalogViewModel.cs
    │       ├── FilmDetailViewModel.cs
    │       ├── FilmFormViewModel.cs
    │       ├── LoginViewModel.cs
    │       ├── PeliculasMasAlquiladasViewModel.cs
    │       └── RegisterViewModel.cs
    │
    ├── Utils/                              # Criptografía y Seguridad
    │   └── PasswordHelper.cs               # Algoritmo PBKDF2 con Salt (Reto 1)
    │
    └── Views/                              # Vistas Razor y Layout Bootstrap
        ├── Account/                        # Login, Register, AccessDenied
        ├── AdminPeliculas/                 # Index CRUD, Crear, Editar, Inventario
        ├── Alquileres/                     # MisAlquileres, HistorialAdmin
        ├── Home/                           # Index Dashboard
        ├── Peliculas/                      # Index Catálogo, Detalle
        ├── Reportes/                       # PeliculasMasAlquiladas
        └── Shared/                         # _Layout, _Navbar, _Alerts
```
