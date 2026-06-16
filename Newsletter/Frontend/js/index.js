document.addEventListener('DOMContentLoaded', () => {  
    cargarContenidoInicial();
    cargarOfertasIndex();
});

async function cargarContenidoInicial() {
    const grid = document.getElementById('games-grid') || document.getElementById('my-games-grid'); 
    
    if (!grid) return;

    grid.innerHTML = '<div class="text-center w-100 py-5"><span class="spinner-border text-accent"></span> Cargando catálogo unificado...</div>';

    try {
        const [responseJuegos, responseCarrusel] = await Promise.all([
            // 💡 Solicitamos los juegos que están en ambas plataformas
            fetch(`${API_BASE_URL}/Game/GET/games?store=both&page=1&pageSize=10`), 
            fetch(`${API_BASE_URL}/Game/GET/games?sortBy=releasedate_desc&page=1&pageSize=3`) 
        ]);
        
        if (!responseJuegos.ok || !responseCarrusel.ok) {
            throw new Error("No se pudo cargar el catálogo.");
        }

        const dataJuegos = await responseJuegos.json();
        const dataCarrusel = await responseCarrusel.json();
        
        const juegosGrid = Array.isArray(dataJuegos) ? dataJuegos : (dataJuegos.items || []);
        const juegosCarrusel = Array.isArray(dataCarrusel) ? dataCarrusel : (dataCarrusel.items || []);
        
        configurarCarruselInizial(juegosCarrusel);
        grid.innerHTML = ''; 

        if (juegosGrid.length === 0) {
            grid.innerHTML = '<div class="col-12 text-center text-secondary py-5">No hay juegos multiplataforma disponibles en este momento.</div>';
            return;
        }

        juegosGrid.forEach(juego => {
            const imageUrl = juego.gameCoverUrl || 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?auto=format&fit=crop&w=800&q=80';
            
            // 💰 Lógica Visual de Precios para Steam
            let steamHTML = '<span class="text-muted small">No disp.</span>';
            if (juego.steamAppId) {
                if (juego.price === 0) {
                    steamHTML = '<span class="text-success fw-bold small">Gratis</span>';
                } else if (juego.onOffer && juego.discountPercentage > 0) {
                    steamHTML = `
                        <span class="text-secondary text-decoration-line-through me-1" style="font-size: 0.7rem;">USD ${juego.price.toFixed(2)}</span>
                        <span class="text-success fw-bold small">USD ${juego.finalPrice.toFixed(2)}</span>
                    `;
                } else {
                    steamHTML = `<span class="fw-bold text-white small">USD ${juego.finalPrice.toFixed(2)}</span>`;
                }
            }

   let epicHTML = '<span class="text-muted small">No disp.</span>';
            
            if (juego.epicStoreId) {
                if (juego.epicPrice === 0) {
                    epicHTML = '<span class="text-success fw-bold small">Gratis</span>';
                } else if (juego.epicOnOffer && juego.epicDiscountPercentage > 0) {
                    epicHTML = `
                        <span class="text-secondary text-decoration-line-through me-1" style="font-size: 0.7rem;">USD ${juego.epicPrice.toFixed(2)}</span>
                        <span class="text-success fw-bold small">USD ${juego.epicFinalPrice.toFixed(2)}</span>
                    `;
                } else {
                    epicHTML = `<span class="fw-bold text-info small">USD ${juego.epicFinalPrice.toFixed(2)}</span>`;
                }
            }

            const cardHtml = `
            <div class="col-12 col-sm-6 col-md-4 col-lg-3">
                <div class="card h-100 bg-dark text-white border-secondary card-videojuego shadow-sm">
                    <img src="${imageUrl}" class="card-img-top object-fit-cover" alt="${juego.name}" style="height: 220px; object-fit: cover;">
                    
                    <div class="card-body d-flex flex-column justify-content-between p-3">
                        <h5 class="card-title h6 fw-bold text-truncate mb-3" title="${juego.name}">${juego.name}</h5>
                        
                        <div class="precios-comparados bg-black bg-opacity-50 p-2 rounded border border-secondary border-opacity-25">
                            
                            <div class="d-flex justify-content-between align-items-center mb-2 border-bottom border-secondary border-opacity-25 pb-1">
                                <span class="small text-secondary" style="font-size: 0.8rem;">
                                    <i class="bi bi-steam text-white me-1"></i>Steam
                                </span>
                                <div class="text-end">
                                    ${steamHTML}
                                </div>
                            </div>
                            
                            <div class="d-flex justify-content-between align-items-center pt-1">
                                <span class="small text-secondary" style="font-size: 0.8rem;">
                                    <i class="bi bi-controller text-info me-1"></i>Epic
                                </span>
                                <div class="text-end">
                                    ${epicHTML}
                                </div>
                            </div>

                        </div>
                        
                        <a href="pages/detalle-juego.html?gameId=${juego.id}" class="btn btn-sm btn-outline-accent w-100 mt-3 fw-bold">
                            Ver detalles
                        </a>
                    </div>
                </div>
            </div>
            `;
            grid.innerHTML += cardHtml;
        });

    } catch (error) {
        console.error("Error cargando los juegos:", error);
        grid.innerHTML = `
            <div class="col-12 text-center py-5">
                <i class="bi bi-exclamation-triangle text-danger fs-1"></i>
                <h5 class="text-white mt-3">Error de conexión</h5>
                <p class="text-secondary">No pudimos conectar con la base de datos de juegos unificados.</p>
            </div>
        `;
    }
}

function configurarCarruselInizial(juegosCarrusel) {
    const indicatorsContainer = document.getElementById("carousel-indicators");
    const innerContainer = document.getElementById("carousel-inner");

    if (!indicatorsContainer || !innerContainer || juegosCarrusel.length === 0) return;

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
                <div class="hero-banner" style="background-image: url('${coverUrl}'); height: 40vh; min-height: 300px;">
                    <div class="hero-overlay"></div>
                    <div class="hero-content">
                        <span class="badge mb-2 px-2 py-1" style="background-color: var(--accent-secondary);">ÚLTIMO LANZAMIENTO</span>
                        
                        <h1 class="display-5 fw-bold text-white mb-2 text-truncate">${game.name.toUpperCase()}</h1>
                        
                        <p class="lead text-light mb-3 d-none d-md-block" style="color: var(--text-secondary) !important; font-size: 1.1rem; max-height: 60px; overflow: hidden;">
                            ${game.shortDescription || 'Un título espectacular que se suma a nuestro catálogo. ¡Descúbrelo ya!'}
                        </p>
                        
                        <div class="d-flex gap-3">
                            <a href="pages/detalle-juego.html?gameId=${game.id}" class="btn btn-sm btn-accent px-4 py-2 fw-bold">Ver detalles</a>
                        </div>
                    </div>
                </div>
            </div>
        `;
    });
}

async function cargarOfertasIndex() {
    const offersGrid = document.getElementById('games-offers-grid');
    if (!offersGrid) return;

    offersGrid.innerHTML = '<div class="text-center w-100 py-4"><span class="spinner-border text-success"></span> Buscando ofertas...</div>';

    try {
        // 💡 Corrección: Eliminado el "&sortBy" vacío del final
        const response = await fetch(`${API_BASE_URL}/Game/GET/games?onOffer=true&page=1&pageSize=10`);
        if (!response.ok) throw new Error("No se pudieron cargar las ofertas.");

        const data = await response.json();
        const juegos = Array.isArray(data) ? data : (data.items || []);

        // 💡 Corrección: "juego" cambiado por "j" para que coincida con el parámetro del filter
        const juegosEnOferta = juegos.filter(j => j.onOffer === true || j.discountPercentage > 0);

        offersGrid.innerHTML = '';

        if (juegosEnOferta.length === 0) {
            offersGrid.innerHTML = '<div class="text-center text-secondary py-3 w-100">Próximamente más ofertas disponibles.</div>';
            return;
        }

        juegosEnOferta.forEach(juego => {
            const imageUrl = juego.gameCoverUrl || 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?auto=format&fit=crop&w=400&q=80';
            
            const precioHTML = `
                <div class="d-flex flex-column">
                    <span class="text-secondary text-decoration-line-through x-small" style="font-size: 0.75rem;">
                        USD ${juego.price.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                    </span>
                    <div class="d-flex align-items-center gap-1">
                        <span class="badge bg-success font-monospace p-1" style="font-size: 0.7rem;">-${juego.discountPercentage}%</span>
                        <span class="text-white fw-bold small">
                            USD ${juego.finalPrice.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                        </span>
                    </div>
                </div>
            `;

            // 💡 Corrección: Adaptado al array 'categories' que devuelve el DTO de C#
            const categoriaBadge = (juego.categories && juego.categories.length > 0)
                ? `<span class="badge bg-secondary mb-1 text-uppercase" style="font-size: 0.65rem;">${juego.categories[0].description}</span>` 
                : '';
                
            const cardHTML = `
                <div class="col-10 col-sm-6 col-md-4 col-lg-3 flex-shrink-0">
                    <div class="card h-100 bg-dark border-secondary game-card shadow-sm">
                        <a href="pages/detalle-juego.html?gameId=${juego.id}" class="text-decoration-none">
                            <img src="${imageUrl}" class="card-img-top" alt="${juego.name}" style="height: 140px; object-fit: cover;">
                            <div class="card-body p-2 d-flex flex-column">
                                ${categoriaBadge}
                                <h6 class="card-title text-white fw-bold text-truncate mb-2" style="font-size: 0.9rem;" title="${juego.name}">
                                    ${juego.name}
                                </h6>
                                <div class="mt-auto d-flex justify-content-between align-items-center pt-2 border-top border-secondary">
                                    ${precioHTML}
                                </div>
                            </div>
                        </a>
                    </div>
                </div>
            `;
            offersGrid.innerHTML += cardHTML;
        });

    } catch (error) {
        console.error("Error al cargar las ofertas del index:", error);
        offersGrid.innerHTML = '<div class="text-center text-danger py-3 w-100">Error al conectar con el servidor de ofertas.</div>';
    }
}