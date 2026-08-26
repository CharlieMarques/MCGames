document.addEventListener('DOMContentLoaded', () => {
    renderizarNavbar();
    verificarEstadoAuth();
    const urlParams = new URLSearchParams(window.location.search);
    const busquedaDesdeNavbar = urlParams.get('search');

    if (busquedaDesdeNavbar) {
        currentSearchTerm = busquedaDesdeNavbar;
        const searchInput = document.getElementById('search-input');
        if (searchInput) {
            searchInput.value = busquedaDesdeNavbar;
        }
    }

    cargarCatalogo(1);

    const searchInput = document.getElementById('search-input');
    if (searchInput) {
        searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                buscarJuegos();
            }
        });
    }
});
document.addEventListener('submit', (e) => {
    if (e.target && e.target.id === 'global-search-form') {
        e.preventDefault();
        
        const termino = document.getElementById('global-search-input').value.trim();
        
        if (termino) {
            const enRaiz = window.location.pathname.endsWith('/') || window.location.pathname.endsWith('index.html');
            const rutaCatalogo = enRaiz ? './pages/catalogo.html' : './catalogo.html';
            window.location.href = `${rutaCatalogo}?search=${encodeURIComponent(termino)}`;
        }
    }
});

function renderizarNavbar() {
    const enRaiz = window.location.pathname.endsWith('/') || window.location.pathname.endsWith('index.html');
    const prefijo = enRaiz ? './pages/' : './';
    const linkInicio = enRaiz ? '#' : '../index.html';
    const rutaLogo = enRaiz ? './assets/icons/Logo.svg' : '../assets/icons/Logo.svg';
    const navbarContainer = document.getElementById('navbar-container');   
    if (!navbarContainer) return; 

    navbarContainer.innerHTML = `
        <nav class="navbar navbar-expand-lg navbar-dark bg-dark border-bottom border-secondary sticky-top">
            <div class="container">
                <a class="navbar-brand d-flex align-items-center gap-2" href="${linkInicio}">               
                    <img src="${rutaLogo}" alt="Logo MCGames" width="32" height="32" class="me-2">
                    <span class="fw-bold">MCGames</span>
                </a>

                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarNav">
                    <span class="navbar-toggler-icon"></span>
                </button>

                <div class="collapse navbar-collapse" id="navbarNav">
                    <ul class="navbar-nav me-auto">
                        <li class="nav-item">
                            <a class="nav-link active" href="${prefijo}catalogo.html">Tienda</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" href="${prefijo}biblioteca.html">Mi Biblioteca</a>
                        </li>
                    </ul>
                    <form class="d-flex mx-auto my-2 my-lg-0" id="global-search-form" style="max-width: 400px; width: 100%;">
                <div class="input-group">
                    <input class="form-control bg-dark border-secondary text-light" type="search" id="global-search-input" placeholder="Buscar juegos..." required>
                    <button class="btn btn-outline-accent" type="submit">
                        <i class="bi bi-search"></i>
                    </button>
                </div>
            </form>

                    <div class="d-flex gap-3 align-items-center">
                        <div id="nav-guest" class="d-flex gap-2">
                            <a href="${prefijo}login.html" class="btn btn-outline-accent">Iniciar Sesión</a>
                            <a href="${prefijo}registro.html" class="btn btn-accent">Registrarse</a>
                        </div>

                        <div id="nav-user" class="d-flex gap-3 align-items-center d-none">
                                        <a href="${prefijo}perfil.html" class="text-light text-decoration-none fw-bold text-truncate"
                            style="max-width: 150px;">
                            <i class="bi bi-person-circle text-accent me-1"></i>
                            <span id="nav-username"></span>
                        </a>
                            <button id="btnLogout" class="btn btn-sm btn-outline-danger" title="Cerrar Sesión">
                                <i class="bi bi-box-arrow-right"></i> Salir
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        </nav>
    `;
}
function verificarEstadoAuth() {
 const token = localStorage.getItem('jwtToken');
    const navGuest = document.getElementById('nav-guest');
    const navUser = document.getElementById('nav-user');
    const navUsername = document.getElementById('nav-username');
    const btnLogout = document.getElementById('btnLogout');
    if (!navGuest || !navUser || !navUsername) return;

    if (token) {
        let nombreReal = localStorage.getItem('username') || 'Gamer'; 
        let esAdmin = false;

        try {
            const payloadBase64 = token.split('.')[1];
            const payloadJson = atob(payloadBase64); 
            const tokenData = JSON.parse(payloadJson);
            nombreReal = tokenData.sub || nombreReal; 

            const roles = tokenData['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
            if (Array.isArray(roles)) {
                esAdmin = roles.includes('Admin') || roles.includes('Moderador');
            } else {
                esAdmin = (roles === 'Admin' || roles === 'Moderador');
            }
        } catch (error) {
            console.error("No se pudo decodificar el token", error);
        }

        navGuest.classList.add('d-none');
        navUser.classList.remove('d-none');
        navUsername.textContent = nombreReal; 
        if (esAdmin && !document.getElementById('nav-admin-link')) {
            const enRaiz = window.location.pathname.endsWith('/') || window.location.pathname.endsWith('index.html');
            const prefijo = enRaiz ? './pages/' : './';  
            const ulNavbar = document.querySelector('.navbar-nav');
            if (ulNavbar) {
                ulNavbar.innerHTML += `
                    <li class="nav-item ms-lg-3" id="nav-admin-link">
                        <a class="nav-link text-warning fw-bold" href="${prefijo}admin-dashboard.html">
                            <i class="bi bi-shield-lock-fill"></i> Panel Admin
                        </a>
                    </li>
                `;
            }
        }
        if (btnLogout) {
            btnLogout.addEventListener('click', () => {
                localStorage.removeItem('jwtToken');
                localStorage.removeItem('username');
                const enRaiz = window.location.pathname.endsWith('/') || window.location.pathname.endsWith('index.html');
                window.location.href = enRaiz ? window.location.href : '../index.html'; 
            });
        }
    } else {
        navGuest.classList.remove('d-none');
        navUser.classList.add('d-none');
    }
}