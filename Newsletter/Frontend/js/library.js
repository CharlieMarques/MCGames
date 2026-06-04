document.addEventListener('DOMContentLoaded', () => {
    verificarSesion();
    cargarMiBiblioteca();
    configurarBotonSalir();
});

function verificarSesion() {
    const token = localStorage.getItem('jwtToken');
    
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

async function cargarMiBiblioteca() {
    const token = localStorage.getItem('jwtToken');
    const grid = document.getElementById('my-games-grid');
    const spinner = document.getElementById('loading-spinner');
    const emptyState = document.getElementById('empty-state');
    const countText = document.getElementById('games-count');

    try {
        const response = await fetch(`${API_BASE_URL}/Library/GET/Library/Games`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}` 
            }
        });

        if (response.status === 401) {
            alert("Tu sesión ha expirado. Por favor, inicia sesión de nuevo.");
            localStorage.removeItem('jwtToken');
            window.location.href = 'login.html';
            return;
        }

        if (!response.ok) {
            throw new Error("Error al cargar la biblioteca");
        }

        const juegos = await response.json();
        spinner.classList.add('d-none');

        if (juegos.length === 0) {
            emptyState.classList.remove('d-none');
            countText.textContent = '0 juegos';
            return;
        }

        grid.classList.remove('d-none');
        countText.textContent = `${juegos.length} juegos`;
        grid.innerHTML = '';

juegos.forEach(juego => {
    const fecha = new Date(juego.releaseDate).toLocaleDateString('es-AR', { year: 'numeric', month: 'short', day: 'numeric' });
    const enlaceJugar = juego.steamAppId 
        ? `href="steam://run/${juego.steamAppId}"` 
        : `href="#" onclick="alert('ID de Steam no disponible'); return false;"`;
    const enlaceComprar = juego.steamAppId 
        ? `href="https://store.steampowered.com/app/${juego.steamAppId}" target="_blank"` 
        : `href="https://store.steampowered.com" target="_blank"`;

    const cardHTML = `
        <div class="col-12 col-sm-6 col-md-4 col-lg-3">
            <div class="card game-card h-100 text-light border-secondary">
                <img src="${juego.gameCoverUrl}" class="card-img-top" alt="${juego.name}" style="height: 250px; object-fit: cover;">
                
                <div class="card-body d-flex flex-column" style="background-color: var(--bg-secondary);">
                    <h5 class="card-title text-truncate fw-bold mb-1" title="${juego.name}">${juego.name}</h5>
                    <small class="text-secondary mb-3"><i class="bi bi-calendar3"></i> Agregado el: ${fecha}</small>
                    
                    <div class="d-flex gap-2 mt-auto">
                        <a ${enlaceJugar} class="btn btn-sm btn-accent w-50 fw-bold text-white shadow-sm">
                            <i class="bi bi-play-fill"></i> Jugar
                        </a>
                        
                        <a ${enlaceComprar} class="btn btn-sm btn-outline-success w-50 fw-bold shadow-sm">
                            <i class="bi bi-cart-fill"></i> Comprar
                        </a>
                    </div>
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