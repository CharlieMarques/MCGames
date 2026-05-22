document.addEventListener('DOMContentLoaded', () => {
    verificarSesion();
    cargarMiBiblioteca();
    configurarBotonSalir();
});

// ==========================================
// 1. SEGURIDAD FRONTEND
// ==========================================
function verificarSesion() {
    const token = localStorage.getItem('jwtToken');
    
    // Si no hay token, lo pateamos al login inmediatamente
    if (!token) {
        window.location.href = 'login.html';
        return;
    }
}

function configurarBotonSalir() {
    const btnLogout = document.getElementById('btnLogout');
    if (btnLogout) {
        btnLogout.addEventListener('click', () => {
            // Destruimos el token de la bóveda
            localStorage.removeItem('jwtToken');
            localStorage.removeItem('username');
            // Redirigimos al inicio
            window.location.href = '../index.html';
        });
    }
}

// ==========================================
// 2. CONEXIÓN CON LA API DE C#
// ==========================================
async function cargarMiBiblioteca() {
    const token = localStorage.getItem('jwtToken');
    const grid = document.getElementById('my-games-grid');
    const spinner = document.getElementById('loading-spinner');
    const emptyState = document.getElementById('empty-state');
    const countText = document.getElementById('games-count');

    try {
        // Hacemos el llamado a tu endpoint seguro
        const response = await fetch(`${API_BASE_URL}/Library/GET/Library/Games`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                // 💡 AQUÍ ESTÁ LA MAGIA: Enviamos el pase VIP
                'Authorization': `Bearer ${token}` 
            }
        });

        // Si el token expiró o es inválido, el backend devuelve 401 Unauthorized
        if (response.status === 401) {
            alert("Tu sesión ha expirado. Por favor, inicia sesión de nuevo.");
            localStorage.removeItem('jwtToken');
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            throw new Error("Error al cargar la biblioteca");
        }

        // Parseamos la lista de ReadGamesLibraryDto que nos manda C#
        const juegos = await response.json();
        spinner.classList.add('d-none'); // Ocultar animación de carga

        // Si la lista viene vacía
        if (juegos.length === 0) {
            emptyState.classList.remove('d-none');
            countText.textContent = '0 juegos';
            return;
        }

        // Si hay juegos, los pintamos
        grid.classList.remove('d-none');
        countText.textContent = `${juegos.length} juegos`;
        grid.innerHTML = ''; // Limpiar grilla por si acaso

        juegos.forEach(juego => {
            // Formatear la fecha (asumiendo que C# envía formato ISO "2024-05-12T00:00:00")
            const fecha = new Date(juego.releaseDate).toLocaleDateString('es-AR', { year: 'numeric', month: 'short',day: 'numeric' });

            const cardHTML = `
                <div class="col-12 col-sm-6 col-md-4 col-lg-3">
                    <div class="card game-card h-100 text-light border-secondary">
                        <img src="${juego.gameCoverUrl}" class="card-img-top" alt="${juego.name}" style="height: 250px;">
                        
                        <div class="card-body d-flex flex-column" style="background-color: var(--bg-secondary);">
                            <h5 class="card-title text-truncate fw-bold mb-1" title="${juego.name}">${juego.name}</h5>
                            <small class="text-secondary mb-3"><i class="bi bi-calendar3"></i> Agregado el : ${fecha}</small>
                            
                            <button class="btn btn-sm btn-outline-light w-100 mt-auto">
                                <i class="bi bi-play-fill"></i> Jugar
                            </button>
                        </div>
                    </div>
                </div>
            `;
            grid.innerHTML += cardHTML;
        });

    } catch (error) {
        console.error("Error:", error);
        spinner.innerHTML = `<div class="alert alert-danger">Error de conexión con el servidor.</div>`;
    }
}