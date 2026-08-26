
let currentPage = 1;
const pageSize = 24;
let currentSearchTerm = '';
let selectedGenres = [];
let currentSort = 'releasedate_desc';
let soloOfertas = false;
let soloAmbasPlataformas = false;
let soloEpic = false;
let currentDiscount = 0;

document.addEventListener('DOMContentLoaded', () => {
    if (typeof renderizarNavbar === 'function') renderizarNavbar();
    if (typeof verificarEstadoAuth === 'function') verificarEstadoAuth();

    const urlParams = new URLSearchParams(window.location.search);
    const busquedaDesdeNavbar = urlParams.get('search');

    if (busquedaDesdeNavbar) {
        currentSearchTerm = busquedaDesdeNavbar;
        const searchInput = document.getElementById('search-input');
        if (searchInput) searchInput.value = busquedaDesdeNavbar;
    }

    cargarGenerosFiltro();
    cargarCatalogo(1);

    const searchInput = document.getElementById('search-input');
    if (searchInput) {
        searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') buscarJuegosLocales();
        });
    }
});

function buscarJuegosLocales() {
    const searchInput = document.getElementById('search-input');
    if (searchInput) {
        currentSearchTerm = searchInput.value.trim();
        cargarCatalogo(1);
    }
}

async function cargarGenerosFiltro() {
    const container = document.getElementById('filter-genres');

    try {
        const response = await fetch(`${API_BASE_URL}/Genre/GET/genres`);
        if (!response.ok) throw new Error("Error de conexión");

        const generos = await response.json();
        let htmlContent = '<div id="genres-list">';

        generos.forEach((g, index) => {
            const nombre = g.description || g.name || 'Desconocido';
            const ocultarClase = index >= 5 ? 'd-none extra-genre' : '';

            htmlContent += `
                <div class="form-check mb-2 ${ocultarClase}">
                    <input class="form-check-input genre-checkbox" type="checkbox" value="${g.id}" id="genre-${g.id}" onchange="actualizarFiltrosGenero()">
                    <label class="form-check-label text-light small" style="cursor: pointer;" for="genre-${g.id}">
                        ${nombre}
                    </label>
                </div>
            `;
        });

        htmlContent += '</div>';

        if (generos.length > 5) {
            htmlContent += `
                <button id="btn-toggle-genres" class="btn btn-link btn-sm text-accent text-decoration-none p-0 mt-2" onclick="toggleGeneros()">
                    <i class="bi bi-chevron-down"></i> Ver más
                </button>
            `;
        }

        container.innerHTML = htmlContent;

    } catch (error) {
        console.error("Error géneros:", error);
        container.innerHTML = '<span class="text-danger small">Error cargando filtros.</span>';
    }
}

function toggleGeneros() {
    const extraGenres = document.querySelectorAll('.extra-genre');
    if (extraGenres.length === 0) return;

    const btn = document.getElementById('btn-toggle-genres');
    const estanOcultos = extraGenres[0].classList.contains('d-none');

    extraGenres.forEach(genre => {
        if (estanOcultos) {
            genre.classList.remove('d-none');
        } else {
            genre.classList.add('d-none');
        }
    });

    if (estanOcultos) {
        btn.innerHTML = '<i class="bi bi-chevron-up"></i> Ver menos';
    } else {
        btn.innerHTML = '<i class="bi bi-chevron-down"></i> Ver más';
    }
}

function actualizarFiltrosGenero() {
    const checkboxes = document.querySelectorAll('.genre-checkbox:checked');
    selectedGenres = Array.from(checkboxes).map(cb => cb.value);
    cargarCatalogo(1);
}
function actualizarFiltroOfertas() {
    const checkbox = document.getElementById('filter-offers');
    if (checkbox) {
        soloOfertas = checkbox.checked;
        cargarCatalogo(1);
    }
}
function actualizarFiltroPlataformas() {
    const checkbox = document.getElementById('filter-both-stores');
    if (checkbox) {
        soloAmbasPlataformas = checkbox.checked;
        cargarCatalogo(1);
    }
}
function actualizarFiltroEpic() {
    const checkbox = document.getElementById('filter-epic-stores');
    if (checkbox) {
        soloEpic = checkbox.checked;
        cargarCatalogo(1);
    }
}
function cambiarOrden() {
    const select = document.getElementById('sort-select');
    if (select) {
        currentSort = select.value;
        cargarCatalogo(1);
    }
}

function actualizarFiltroDescuento()
{
    const select = document.getElementById('filter-discount')
    if(select){
        currentDiscount = parseInt(select.value) || 0;
        cargarCatalogo(1)
    }
}
async function cargarCatalogo(page) {
    currentPage = page;
    const grid = document.getElementById('catalog-grid');
    const info = document.getElementById('catalog-info');

    if (!grid) return;
    grid.innerHTML = '<div class="col-12 text-center py-5"><span class="spinner-border text-accent"></span> Buscando juegos...</div>';

    try {
        currentSort = `name_asc`
        let url = `${API_BASE_URL}/Game/GET/games?page=${currentPage}&pageSize=${pageSize}&sortBy=${currentSort}`;
        
        if (soloOfertas) {
            url += `&onOffer=true`;
        }

        if(currentDiscount >0){
            url += `&discount=${currentDiscount}`;
        }
        if (soloAmbasPlataformas) {
            url += `&store=both`;
        }
        else if(soloEpic){
            url += `&store=epic`;
        }

        if (currentSearchTerm && currentSearchTerm.trim() !== '') {
            url += `&name=${encodeURIComponent(currentSearchTerm.trim())}`;
        }

        if (selectedGenres.length > 0) {
            selectedGenres.forEach(genreId => {
                url += `&genreIds=${genreId}`;
            });
        }

        const response = await fetch(url);
        if (!response.ok) throw new Error("Error del servidor");

        const data = await response.json();
        const juegos = Array.isArray(data) ? data : (data.items || []);
        const totalPages = data.totalPages || 1;
        const totalRecords = data.totalRecords || juegos.length;

        grid.innerHTML = '';

        if (juegos.length === 0) {
            grid.innerHTML = `
                <div class="col-12 text-center py-5">
                    <i class="bi bi-controller text-secondary" style="font-size: 3rem;"></i>
                    <h5 class="text-white mt-3">No hay coincidencias</h5>
                </div>`;
            if (info) info.textContent = "Mostrando 0 resultados";
            document.getElementById('catalog-pagination').innerHTML = '';
            return;
        }

        if (info) info.textContent = `Mostrando ${juegos.length} juegos de ${totalRecords} (Página ${currentPage} de ${totalPages})`;

        juegos.forEach(juego => {
            const imageUrl = juego.gameCoverUrl || 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?auto=format&fit=crop&w=400&q=80';

            let precioHTML = '';

            if (soloAmbasPlataformas && juego.epicStoreId) {
                let steamP = juego.price === 0 ? '<span class="text-success">Gratis</span>' : `USD ${juego.finalPrice.toFixed(2)}`;
                if (juego.onOffer && juego.discountPercentage > 0) {
                    steamP = `<span class="text-success">USD ${juego.finalPrice.toFixed(2)}</span> <span class="badge bg-success ms-1" style="font-size:0.6rem">-${juego.discountPercentage}%</span>`;
                }

                let epicP = juego.epicPrice === 0 ? '<span class="text-success">Gratis</span>' : `USD ${juego.epicFinalPrice.toFixed(2)}`;
                if (juego.epicOnOffer && juego.epicDiscountPercentage > 0) {
                    epicP = `<span class="text-success">USD ${juego.epicFinalPrice.toFixed(2)}</span> <span class="badge bg-success ms-1" style="font-size:0.6rem">-${juego.epicDiscountPercentage}%</span>`;
                }

                precioHTML = `
                    <div class="w-100 d-flex flex-column gap-1" style="font-size: 0.75rem;">
                        <div class="d-flex justify-content-between border-bottom border-secondary pb-1">
                            <span class="text-secondary"><i class="bi bi-steam me-1 text-white"></i>Steam</span>
                            <span class="text-light fw-bold">${steamP}</span>
                        </div>
                        <div class="d-flex justify-content-between">
                            <span class="text-secondary"><i class="bi bi-controller me-1 text-info"></i>Epic</span>
                            <span class="text-light fw-bold">${epicP}</span>
                        </div>
                    </div>
                `;
            } else {
                if (juego.onOffer && juego.discountPercentage > 0) {
                    precioHTML = `
                        <div class="d-flex flex-column text-start">
                            <span class="text-secondary text-decoration-line-through x-small" style="font-size: 0.75rem;">
                                USD ${juego.price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                            </span>
                            <div class="d-flex align-items-center gap-1">
                                <span class="badge bg-success font-monospace p-1" style="font-size: 0.65rem;">-${juego.discountPercentage}%</span>
                                <span class="text-white fw-bold small">
                                    USD ${juego.finalPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                                </span>
                            </div>
                        </div>
                    `;
                } else {
                    precioHTML = juego.price > 0
                        ? `<span class="text-light fw-bold mini-card-price">USD ${juego.price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>`
                        : `<span class="text-success fw-bold mini-card-price">Gratis</span>`;
                }
            }

            const cardHTML = `
                <div class="col-12 col-sm-6 col-lg-4 col-xl-3">
                    <div class="card h-100 bg-dark border-secondary game-card shadow-sm">
                        <a href="detalle-juego.html?gameId=${juego.id}" class="text-decoration-none">
                            <img src="${imageUrl}" class="card-img-top mini-card-img" alt="${juego.name}">
                            <div class="card-body p-2 d-flex flex-column">
                                <h6 class="card-title text-white fw-bold text-truncate mini-card-title" title="${juego.name}">${juego.name}</h6>
                                <div class="mt-auto d-flex justify-content-between align-items-center pt-2 border-top border-secondary">
                                    ${precioHTML}
                                </div>
                            </div>
                        </a>
                    </div>
                </div>
            `;
            grid.innerHTML += cardHTML;
        });

        dibujarPaginacionCatalogo(totalPages);

    } catch (error) {
        console.error("Error:", error);
        grid.innerHTML = '<div class="col-12 text-center py-5"><h6 class="text-danger mt-2">Error de conexión</h6></div>';
    }
}

function dibujarPaginacionCatalogo(totalPages) {
    const controls = document.getElementById('catalog-pagination');
    if (!controls) return;
    controls.innerHTML = '';

    if (totalPages <= 1) return;

    const prevDisabled = currentPage === 1 ? 'disabled' : '';
    controls.innerHTML += `
        <li class="page-item ${prevDisabled}">
            <button class="page-link bg-dark border-secondary text-light" onclick="cambiarPaginaCatalogo(${currentPage - 1})">&laquo;</button>
        </li>
    `;
    const primeraActiva = currentPage === 1 ? 'active bg-accent border-accent text-white' : 'bg-dark border-secondary text-light';
    controls.innerHTML += `
        <li class="page-item">
            <button class="page-link ${primeraActiva}" onclick="cambiarPaginaCatalogo(1)">1</button>
        </li>
    `;

    if (currentPage > 3) {
        controls.innerHTML += `
            <li class="page-item disabled">
                <span class="page-link bg-dark border-secondary text-secondary">...</span>
            </li>
        `;
    }


    let startPage = Math.max(2, currentPage - 1);
    let endPage = Math.min(totalPages - 1, currentPage + 1);

    for (let i = startPage; i <= endPage; i++) {
        const activeClass = i === currentPage ? 'active bg-accent border-accent text-white' : 'bg-dark border-secondary text-light';
        controls.innerHTML += `
            <li class="page-item">
                <button class="page-link ${activeClass}" onclick="cambiarPaginaCatalogo(${i})">${i}</button>
            </li>
        `;
    }

    if (currentPage < totalPages - 2) {
        controls.innerHTML += `
            <li class="page-item disabled">
                <span class="page-link bg-dark border-secondary text-secondary">...</span>
            </li>
        `;
    }


    if (totalPages > 1) {
        const ultimaActiva = currentPage === totalPages ? 'active bg-accent border-accent text-white' : 'bg-dark border-secondary text-light';
        controls.innerHTML += `
            <li class="page-item">
                <button class="page-link ${ultimaActiva}" onclick="cambiarPaginaCatalogo(${totalPages})">${totalPages}</button>
            </li>
        `;
    }


    const nextDisabled = currentPage === totalPages ? 'disabled' : '';
    controls.innerHTML += `
        <li class="page-item ${nextDisabled}">
            <button class="page-link bg-dark border-secondary text-light" onclick="cambiarPaginaCatalogo(${currentPage + 1})">&raquo;</button>
        </li>
    `;
}
function cambiarPaginaCatalogo(nuevaPagina) {
    const grid = document.getElementById('catalog-grid');
    if (grid) {
        grid.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
    cargarCatalogo(nuevaPagina);
}