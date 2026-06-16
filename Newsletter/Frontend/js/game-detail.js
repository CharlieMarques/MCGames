document.addEventListener('DOMContentLoaded', () => {
    const urlParams = new URLSearchParams(window.location.search);
    const gameId = urlParams.get('gameId');

    if (!gameId) {
        window.location.href = '../index.html';
        return;
    }

    cargarDetallesDelJuego(gameId);
    configurarSistemaEstrellas();
    configurarFormularioResena(gameId);
    verificarEstadoLogin();
    cargarResenasDelJuego(gameId);
    const btnAddLibrary = document.getElementById('btn-add-library');
    if (btnAddLibrary) {
        btnAddLibrary.addEventListener('click', () => agregarABiblioteca(gameId));
    }

    verificarSiEstaEnBiblioteca(gameId);
});

async function cargarDetallesDelJuego(gameId) {
    try {
        // En tu backend, este endpoint de /games?id=... ya te devuelve el objeto del juego con los datos de Steam y Epic
        const response = await fetch(`${API_BASE_URL}/Game/GET/games?id=${gameId}&page=1&pageSize=10`);

        if (!response.ok) {
            throw new Error("No se pudo cargar la información del juego.");
        }

        const data = await response.json();
        const listaJuegos = Array.isArray(data) ? data : (data.items || []);

        if (!listaJuegos || listaJuegos.length === 0) {
            throw new Error("El juego no existe en la base de datos.");
        }

        const juego = listaJuegos[0];
        const imageUrl = juego.gameCoverUrl || 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?auto=format&fit=crop&w=800&q=80';

        // 1. Fondos y Títulos
        const heroBg = document.getElementById('hero-background');
        if (heroBg) {
            heroBg.style.backgroundImage = `url('${imageUrl}')`;
            heroBg.style.backgroundSize = 'cover';
            heroBg.style.backgroundPosition = 'center top';
        }

        const gamePoster = document.getElementById('game-poster');
        if (gamePoster) {
            gamePoster.src = imageUrl;
        }

        document.getElementById('game-title').textContent = juego.name;
        
        if (juego.releaseDate) {
            const fecha = new Date(juego.releaseDate).toLocaleDateString('es-ES', { year: 'numeric', month: 'long', day: 'numeric' });
            document.getElementById('game-release').innerHTML = `<i class="bi bi-calendar3"></i> Lanzamiento: ${fecha}`;
        } else {
            document.getElementById('game-release').textContent = "Lanzamiento: Desconocido";
        }

        // 💡 2. NUEVO: Lógica de Precios y Botones de Tiendas
        const pricesContainer = document.getElementById('game-prices-container');
        if (pricesContainer) {
            pricesContainer.innerHTML = ''; // Limpiamos el estado inicial

            // --- TARJETA DE STEAM ---
            if (juego.steamAppId) {
                let steamPriceHTML = '';
                if (juego.price === 0) {
                    steamPriceHTML = '<h5 class="mb-0 text-success fw-bold">Gratis</h5>';
                } else if (juego.onOffer && juego.discountPercentage > 0) {
                    steamPriceHTML = `
                        <div class="d-flex align-items-center gap-2">
                            <span class="badge bg-success fs-6">-${juego.discountPercentage}%</span>
                            <div class="d-flex flex-column text-start">
                                <span class="text-secondary text-decoration-line-through" style="font-size: 0.8rem;">USD ${juego.price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                                <span class="text-white fw-bold">USD ${juego.finalPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                            </div>
                        </div>
                    `;
                } else {
                    steamPriceHTML = `<h5 class="mb-0 text-white fw-bold">USD ${juego.finalPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</h5>`;
                }

                pricesContainer.innerHTML += `
                    <div class="p-3 rounded border border-secondary bg-black bg-opacity-50 d-flex flex-column justify-content-between shadow-sm" style="min-width: 210px; backdrop-filter: blur(5px);">
                        <div class="mb-3 d-flex align-items-center gap-2">
                            <i class="bi bi-steam fs-5 text-white"></i>
                            <span class="text-secondary fw-bold small text-uppercase tracking-wide">Steam</span>
                        </div>
                        <div class="mb-3">
                            ${steamPriceHTML}
                        </div>
                        <a href="https://store.steampowered.com/app/${juego.steamAppId}" target="_blank" class="btn btn-sm btn-outline-light mt-auto fw-bold">
                            Ver en la tienda <i class="bi bi-box-arrow-up-right ms-1"></i>
                        </a>
                    </div>
                `;
            }

            // --- TARJETA DE EPIC GAMES ---
            if (juego.epicStoreId) {
                let epicPriceHTML = '';
                if (juego.epicPrice === 0) {
                    epicPriceHTML = '<h5 class="mb-0 text-success fw-bold">Gratis</h5>';
                } else if (juego.epicOnOffer && juego.epicDiscountPercentage > 0) {
                    epicPriceHTML = `
                        <div class="d-flex align-items-center gap-2">
                            <span class="badge bg-success fs-6">-${juego.epicDiscountPercentage}%</span>
                            <div class="d-flex flex-column text-start">
                                <span class="text-secondary text-decoration-line-through" style="font-size: 0.8rem;">USD ${juego.epicPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                                <span class="text-info fw-bold">USD ${juego.epicFinalPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
                            </div>
                        </div>
                    `;
                } else {
                    epicPriceHTML = `<h5 class="mb-0 text-info fw-bold">USD ${juego.epicFinalPrice.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</h5>`;
                }

                pricesContainer.innerHTML += `
                    <div class="p-3 rounded border border-secondary bg-black bg-opacity-50 d-flex flex-column justify-content-between shadow-sm" style="min-width: 210px; backdrop-filter: blur(5px);">
                        <div class="mb-3 d-flex align-items-center gap-2">
                            <i class="bi bi-controller fs-5 text-info"></i>
                            <span class="text-secondary fw-bold small text-uppercase tracking-wide">Epic Games</span>
                        </div>
                        <div class="mb-3">
                            ${epicPriceHTML}
                        </div>
                        <a href="https://store.epicgames.com/p/${juego.epicStoreId}" target="_blank" class="btn btn-sm btn-outline-info mt-auto fw-bold">
                            Ver en la tienda <i class="bi bi-box-arrow-up-right ms-1"></i>
                        </a>
                    </div>
                `;
            }
        }

        // 3. Descripción, Géneros y Categorías
        document.getElementById('game-description').textContent = juego.shortDescription || 'Sin descripción disponible para este título.';

        const genresContainer = document.getElementById('game-genres');
        if (genresContainer) {
            genresContainer.innerHTML = '';
            if (juego.genres && juego.genres.length > 0) {
                juego.genres.forEach(g => {
                    genresContainer.innerHTML += `<span class="badge bg-dark border border-secondary text-light px-2 py-1">${g.description}</span>`;
                });
            } else {
                genresContainer.innerHTML = '<span class="text-muted small fst-italic">Sin géneros</span>';
            }
        }

        const categoryContainer = document.getElementById('game-category');
        if (categoryContainer) {
            categoryContainer.innerHTML = '';
            if (juego.categories && juego.categories.length > 0) {
                juego.categories.forEach(cat => {
                    categoryContainer.innerHTML += `<span class="badge bg-accent px-3 py-1 text-uppercase font-monospace shadow-sm me-2 mb-2" style="letter-spacing: 0.5px;">${cat.description}</span>`;
                });
            } else {
                categoryContainer.innerHTML = '<span class="text-muted small fst-italic">Sin categorías asignadas</span>';
            }
        }

    } catch (error) {
        console.error("Error cargando el juego:", error);
        document.getElementById('game-title').textContent = "Error de conexión";
        document.getElementById('game-description').textContent = "No pudimos conectar con la base de datos para traer este juego.";
    }
}


function configurarSistemaEstrellas() {
    const stars = document.querySelectorAll('.star-btn');
    const ratingInput = document.getElementById('rating-value');

    stars.forEach(star => {
        star.addEventListener('mouseover', function () {
            const value = this.getAttribute('data-value');
            iluminarEstrellas(stars, value);
        });
        star.addEventListener('mouseout', function () {
            const currentValue = ratingInput.value;
            iluminarEstrellas(stars, currentValue);
        });


        star.addEventListener('click', function () {
            const value = this.getAttribute('data-value');
            ratingInput.value = value;
            iluminarEstrellas(stars, value);

            this.style.transform = "scale(1.2)";
            setTimeout(() => this.style.transform = "scale(1)", 200);
        });
    });
}

function iluminarEstrellas(stars, count) {
    stars.forEach(star => {
        const starValue = star.getAttribute('data-value');
        if (starValue <= count) {
            star.classList.remove('bi-star', 'text-secondary');
            star.classList.add('bi-star-fill', 'text-warning');
        } else {
            star.classList.remove('bi-star-fill', 'text-warning');
            star.classList.add('bi-star', 'text-secondary');
        }
    });
}


function verificarEstadoLogin() {
    const token = localStorage.getItem('jwtToken');
    if (!token) {

        document.getElementById('review-form').classList.add('d-none');
        document.getElementById('review-auth-message').classList.remove('d-none');
    }
}

function configurarFormularioResena(gameId) {
    const form = document.getElementById('review-form');
    
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        
        const token = localStorage.getItem('jwtToken');
        const rating = document.getElementById('rating-value').value;
        const text = document.getElementById('review-text').value;

        if (rating == 0) {
            alert("Por favor, selecciona una calificación en estrellas.");
            return;
        }

        const reviewDto = {
            gameId: gameId,
            rating: parseInt(rating),
            comment: text
        };

        const btnSubmit = form.querySelector('button[type="submit"]');
        const textoOriginal = btnSubmit.innerHTML;
        btnSubmit.disabled = true;
        btnSubmit.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Publicando...';

        try {
            const response = await fetch(`${API_BASE_URL}/Reviews/Create/review`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}` 
                },
                body: JSON.stringify(reviewDto)
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || "Error al publicar la reseña.");
            }

            form.reset();
            document.getElementById('rating-value').value = 0;
            
         
            const stars = document.querySelectorAll('.star-btn');
            stars.forEach(s => {
                s.classList.remove('bi-star-fill', 'text-warning');
                s.classList.add('bi-star', 'text-secondary');
            });


            cargarResenasDelJuego(gameId);

        } catch (error) {
            console.error("Error enviando reseña:", error);
            alert(error.message);
        } finally {
            btnSubmit.disabled = false;
            btnSubmit.innerHTML = textoOriginal;
        }
    });
}

async function cargarResenasDelJuego(gameId) {
    const lista = document.getElementById('reviews-list');
    lista.innerHTML = '<div class="text-center text-secondary"><span class="spinner-border spinner-border-sm"></span> Cargando reseñas...</div>';

    try {
        const response = await fetch(`${API_BASE_URL}/Reviews/GET/reviews?gameId=${gameId}`);
        
        if (!response.ok) {
            throw new Error("No se pudieron cargar las reseñas.");
        }

        const resenas = await response.json();
        lista.innerHTML = ''; 

        if (!resenas || resenas.length === 0) {
            lista.innerHTML = '<p class="text-secondary fst-italic">Aún no hay reseñas. ¡Sé el primero en opinar!</p>';
            return;
        }


        resenas.forEach(resena => {
            const nombreUsuario = resena.userName || "Jugador";
            const puntaje = resena.rating || 0;
            const comentario = resena.comment || "";

            const resenaHTML = `
                <div class="card border-secondary bg-dark text-light mb-2">
                    <div class="card-body py-2 px-3">
                        <div class="d-flex justify-content-between align-items-center mb-1">
                            <span class="fw-bold text-accent"><i class="bi bi-person-circle me-1"></i> ${nombreUsuario}</span>
                            <small>${dibujarEstrellas(puntaje)}</small>
                        </div>
                        <p class="mb-0 text-secondary" style="font-size: 0.95rem;">${comentario}</p>
                    </div>
                </div>
            `;
            lista.innerHTML += resenaHTML;
        });

    } catch (error) {
        console.error("Error:", error);
        lista.innerHTML = '<p class="text-danger">Hubo un problema al cargar los comentarios.</p>';
    }
}


function dibujarEstrellas(cantidad) {
    let html = '';
    for(let i = 1; i <= 5; i++) {
        html += i <= cantidad 
            ? '<i class="bi bi-star-fill text-warning"></i>' 
            : '<i class="bi bi-star text-secondary"></i>';
    }
    return html;
}
async function agregarABiblioteca(gameId) {
    const token = localStorage.getItem('jwtToken');
    const btn = document.getElementById('btn-add-library');


    if (!token) {
        alert("¡Alto ahí! Debes iniciar sesión para agregar juegos a tu biblioteca.");
        window.location.href = 'login.html';
        return;
    }

    const contenidoOriginal = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Agregando...';

    try {

        const response = await fetch(`${API_BASE_URL}/Library/AddGame/Library`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}` 
            },

            body: JSON.stringify({ gameId: gameId }) 
        });

        if (!response.ok) {

            const errorText = await response.text();
            throw new Error(errorText || "No se pudo agregar el juego.");
        }


        btn.innerHTML = '<i class="bi bi-check-lg"></i> En tu biblioteca';
        btn.classList.remove('btn-accent');
        btn.classList.add('btn-success');
        
        btn.disabled = true; 

    } catch (error) {
        console.error("Error al agregar a biblioteca:", error);
        alert(error.message);
        btn.disabled = false;
        btn.innerHTML = contenidoOriginal;
    }
}
async function verificarSiEstaEnBiblioteca(gameId) {
    const token = localStorage.getItem('jwtToken');
    const btn = document.getElementById('btn-add-library');

    if (!token || !btn) return;

    try {
        const response = await fetch(`${API_BASE_URL}/Library/GET/Library/Games`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

      if (response.ok) {
            const miBiblioteca = await response.json();
            
            console.log("Juegos en mi biblioteca:", miBiblioteca);
            console.log("Buscando el ID exacto:", gameId);
            
            const idBuscado = gameId.toString().toLowerCase();

            const yaLoTengo = miBiblioteca.some(juego => {
                const idDelJuego = (juego.id || juego.gameId || '').toString().toLowerCase();
                return idDelJuego === idBuscado;
            });

            if (yaLoTengo) {

                btn.innerHTML = '<i class="bi bi-check-lg"></i> En tu biblioteca';
                btn.classList.remove('btn-accent');
                btn.classList.add('btn-success');
                btn.disabled = true; 
            }
        }
    } catch (error) {
        console.error("Error al verificar el estado en la biblioteca:", error);
    }
}