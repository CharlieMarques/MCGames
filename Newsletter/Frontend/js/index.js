document.addEventListener('DOMContentLoaded', () => {  
    // Lanzamos la carga general
    cargarContenidoInicial();
});

async function cargarContenidoInicial() {
    const grid = document.getElementById('games-grid') || document.getElementById('my-games-grid'); 
    
    if (!grid) return;

    grid.innerHTML = '<div class="text-center w-100 py-5"><span class="spinner-border text-accent"></span> Cargando catálogo...</div>';

    try {
        // 💡 OPTIMIZACIÓN: Lanzamos ambas consultas en paralelo aprovechando los filtros del backend
        const [responseJuegos, responseCarrusel] = await Promise.all([
            fetch(`${API_BASE_URL}/Game/GET/games?page=1&pageSize=10`), // Tu grilla estándar
            fetch(`${API_BASE_URL}/Game/GET/games?sortBy=releasedate_desc&page=1&pageSize=3`) // Tu carrusel optimizado por SQL
        ]);
        
        if (!responseJuegos.ok || !responseCarrusel.ok) {
            throw new Error("No se pudo cargar el catálogo.");
        }

        const dataJuegos = await responseJuegos.json();
        const dataCarrusel = await responseCarrusel.json();
        
        // Extraemos los juegos de forma segura usando tu misma validación lógica
        const juegosGrid = Array.isArray(dataJuegos) ? dataJuegos : (dataJuegos.items || []);
        const juegosCarrusel = Array.isArray(dataCarrusel) ? dataCarrusel : (dataCarrusel.items || []);
        
        // --- 1. CARGAMOS EL CARRUSEL DIRECTO CON LOS DATOS DE LA API ---
        configurarCarruselInizial(juegosCarrusel);

        // --- 2. TU LÓGICA ORIGINAL PARA LA GRILLA ---
        grid.innerHTML = ''; // Limpiamos el mensaje de carga
        if (!juegosGrid || juegosGrid.length === 0) {
            grid.innerHTML = '<div class="col-12 text-center text-secondary py-5">No hay juegos disponibles en este momento.</div>';
            return;
        }

        juegosGrid.forEach(juego => {
            const imageUrl = juego.gameCoverUrl || 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?auto=format&fit=crop&w=800&q=80';
            
            const precioHTML = juego.price > 0 
                ? `<span class="price-tag fs-5 text-light fw-bold">USD ${juego.price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>` 
                : `<span class="price-tag text-success fw-bold fs-5">Gratis</span>`;

            const cardHTML = `
                <div class="col-12 col-md-6 col-lg-4 mb-4">
                    <div class="card h-100 bg-dark border-secondary game-card shadow-lg">
                        <img src="${imageUrl}" class="card-img-top" alt="${juego.name}" style="height: 200px; object-fit: cover;">
                        <div class="card-body d-flex flex-column">
                            <h5 class="card-title text-white fw-bold text-truncate">${juego.name}</h5>
                            <p class="card-text text-secondary small flex-grow-1">${juego.shortDescription || 'Sin descripción disponible.'}</p>
                            
                            <div class="d-flex justify-content-between align-items-center mt-3 pt-3 border-top border-secondary">
                                ${precioHTML}
                                <a href="pages/detalle-juego.html?gameId=${juego.id}" class="btn btn-outline-accent btn-sm fw-bold px-3">
                                    Ver más
                                </a>
                            </div>
                        </div>
                    </div>
                </div>
            `;
            grid.innerHTML += cardHTML;
        });

    } catch (error) {
        console.error("Error cargando los juegos:", error);
        grid.innerHTML = `
            <div class="col-12 text-center py-5">
                <i class="bi bi-exclamation-triangle text-danger fs-1"></i>
                <h5 class="text-white mt-3">Error de conexión</h5>
                <p class="text-secondary">No pudimos conectar con la base de datos de juegos.</p>
            </div>
        `;
    }
}

// NUEVA FUNCIÓN AUXILIAR SIMPLIFICADA (Ya no filtra fechas en JS, el Backend ya lo hace)
function configurarCarruselInizial(juegosCarrusel) {
    const indicatorsContainer = document.getElementById("carousel-indicators");
    const innerContainer = document.getElementById("carousel-inner");

    if (!indicatorsContainer || !innerContainer || juegosCarrusel.length === 0) return;

    // Limpiamos los contenedores antes de renderizar
    indicatorsContainer.innerHTML = "";
    innerContainer.innerHTML = "";

    juegosCarrusel.forEach((game, index) => {
        const isActive = index === 0 ? "active" : "";

        indicatorsContainer.innerHTML += `
            <button type="button" data-bs-target="#hero-carousel" data-bs-slide-to="${index}" 
                class="${isActive}" aria-current="${index === 0 ? 'true' : 'false'}" aria-label="Slide ${index + 1}">
            </button>
        `;

        const coverUrl = game.gameCoverUrl || 'https://images.unsplash.com/photo-1605901309584-818e25960b8f?ixlib=rb-4.0.3&auto=format&fit=crop&w=1920&q=80';

        innerContainer.innerHTML += `
            <div class="carousel-item ${isActive}">
                <div class="hero-banner" style="background-image: url('${coverUrl}');">
                    <div class="hero-overlay"></div>
                    <div class="hero-content">
                        <span class="badge mb-3 px-2 py-1" style="background-color: var(--accent-secondary);">ÚLTIMO LANZAMIENTO</span>
                        <h1 class="display-4 fw-bold text-white mb-3">${game.name.toUpperCase()}</h1>
                        <p class="lead text-light mb-4" style="color: var(--text-secondary) !important;">
                            ${game.shortDescription || 'Un título espectacular que se suma a nuestro catálogo. ¡Descúbrelo ya!'}
                        </p>
                        <div class="d-flex gap-3">
                            <a href="pages/detalle-juego.html?gameId=${game.id}" class="btn btn-accent px-4 py-2">Ver detalles</a>
                            <button class="btn btn-outline-light px-4 py-2"><i class="bi bi-heart"></i> Agregar a deseos</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
    });
}