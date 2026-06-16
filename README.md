# 🎮 MCGames - Plataforma de Videojuegos Unificada

MCGames es una plataforma web integral diseñada para unificar el catálogo de videojuegos de múltiples tiendas digitales (Steam y Epic Games).
Permite a los usuarios explorar lanzamientos, descubrir ofertas y comparar precios en tiempo real entre diferentes plataformas desde una única interfaz.

https://www.mcgames.com.ar/
<img width="1499" height="947" alt="imagenes" src="https://github.com/user-attachments/assets/26295762-cc60-4d26-a72f-88ec5e67ec01" />
## 🛠️ Tecnologías Utilizadas
**Backend:**
* C# / ASP.NET Core (RESTful API)
* Entity Framework Core ( Code-First)
* SQL Server (Base de datos relacional)
* JWT (JSON Web Tokens para seguridad y sesiones)
* 
**Frontend:**
* HTML5, CSS3, JavaScript
* Bootstrap 5.3

## 🚀 Instalación y Configuración Local

Sigue estos pasos para levantar el entorno de desarrollo en tu máquina local.

### Prerrequisitos
* [.NET SDK 8.0](https://dotnet.microsoft.com/download)
* SQL Server Management Studio (SSMS) o similar.
* Node.js / Live Server (Opcional, para levantar el frontend rápidamente).

### Pasos

 1-  Clonar el repositorio**
     bash
      git clone  https://github.com/CharlieMarques/MCGames.git
   
  2- Configurar la Base de Datos
       Abre el archivo appsettings.json en el proyecto backend.
       Modifica la cadena de conexión (DefaultConnection) para que apunte a tu instancia local de SQL Server.

  3- Abre la Consola del Administrador de Paquetes (Package Manager Console) en Visual Studio y ejecuta:
      PowerShell
        dotnet ef migrations add NombreDeTuMigracion
        dotnet ef database update

 4-  Ejecutar la API
    Inicia el proyecto backend desde Visual Studio o mediante la CLI:
       bash
          dotnet run

 5-  Ejecutar el Frontend
    Abre la carpeta del frontend y sirve el archivo index.html (puedes usar la extensión Live Server de VS Code).
    Verifica que API_BASE_URL en tu js/config/api.js apunte al puerto correcto de tu localhost (ej: https://localhost:7001).

## 🏗️ Arquitectura del Backend (API REST)

El backend está desarrollado bajo los lineamientos de una arquitectura limpia en capas (N-Tier Architecture), asegurando que el código sea modular, fácil de mantener y escalable.

### Patrones y Prácticas Implementadas:

* **Enfoque Code-First (Entity Framework Core):** Partiendo de un diseño relacional previamente planificado y bien estructurado, se optó por utilizar el enfoque *Code-First*. Esto permitió modelar la base de datos de manera precisa directamente desde las entidades en C# y mantener un control estricto de la evolución del esquema mediante el historial de migraciones.
* **Separación de Responsabilidades (SoC) y Patrón Repositorio:**
  * **Controllers:** Manejan exclusivamente las peticiones HTTP, rutas y validación básica.
  * **Services:** Contienen la lógica de negocio, cálculos y orquestación (como el motor automático de importación de Epic Games).
  * **Repositories:** Actúan como una capa de abstracción entre los servicios y la base de datos (`Data`), centralizando las consultas mediante el *Repository Pattern* para un código más limpio y testeable.
* **Patrón DTO (Data Transfer Objects):** Las entidades reales de la base de datos nunca se exponen al cliente. Se utilizan clases intermedias (`DTOs`) para enviar al frontend únicamente los datos necesarios, mejorando la seguridad y optimizando el consumo de red.
* **Consultas Diferidas y Paginación Dinámica:** Las búsquedas complejas se construyen dinámicamente utilizando `IQueryable`. El motor de SQL Server ejecuta el filtrado y la paginación a nivel de base de datos, garantizando tiempos de respuesta ultrarrápidos para catálogos masivos.
* **Seguridad Stateless (JWT):** La autenticación se maneja mediante JSON Web Tokens, permitiendo una comunicación segura entre la API y el cliente sin necesidad de mantener sesiones activas en el servidor.

### Estructura de Carpetas Principal
\`\`\`text
📁 Directorio del Proyecto/
 ┣ 📂 Controllers/    # Endpoints de la API REST
 ┣ 📂 Data/           # Contexto de la base de datos (DbContext)
 ┣ 📂 DTOs/           # Objetos de transferencia para comunicación segura
 ┣ 📂 Frontend/       # Interfaz de usuario (HTML, CSS, JS, Bootstrap)
 ┣ 📂 Migrations/     # Historial de versiones de la estructura de la base de datos
 ┣ 📂 Models/         # Entidades de dominio que representan las tablas de SQL
 ┣ 📂 Repositories/   # Abstracción de acceso a datos (Patrón Repositorio)
 ┗ 📂 Services/       # Lógica de negocio y tareas automáticas en segundo plano
\`\`\`

Actualmente este proyecto esta en MVP 
🔮 Futuras Mejoras (Roadmap)

    Implementación de filtros avanzados (por género, precio máximo, etc.).

    Paginación dinámica optimizada en el lado del cliente.

    Integración de una tercera plataforma (GOG o Xbox Store).

