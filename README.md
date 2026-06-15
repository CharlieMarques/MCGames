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


Actualmente este proyecto esta en MVP 
🔮 Futuras Mejoras (Roadmap)

    Implementación de filtros avanzados (por género, precio máximo, etc.).

    Paginación dinámica optimizada en el lado del cliente.

    Integración de una tercera plataforma (ej. GOG o Xbox Store).

