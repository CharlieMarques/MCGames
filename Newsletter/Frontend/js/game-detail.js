document.addEventListener('DOMContentLoaded', () => {
    // 1. Extraer el ID del juego de la URL (Ejemplo: detalle-juego.html?gameId=123)
    const urlParams = new URLSearchParams(window.location.search);
    const gameId = urlParams.get('gameId');

    if (!gameId) {
        // Si no hay ID en la URL, lo devolvemos al inicio
        window.location.href = '../index.html';
        return;
    }

    cargarDetallesDelJuego(gameId);
   // verificarEstadoReview();
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

// ==========================================
// CARGA DE DATOS DEL JUEGO
// ==========================================
async function cargarDetallesDelJuego(gameId) {
    try {
        // 1. Petición real al backend
        const response = await fetch(`${API_BASE_URL}/Game/GET/games?id=${gameId}&page=1&pageSize=10`);

        if (!response.ok) {
            throw new Error("No se pudo cargar la información del juego.");
        }

  // 2. Extraemos el objeto completo (que ahora incluye la paginación)
        const data = await response.json();

        // 💡 MAGIA: Sacamos la lista real de juegos de la propiedad "items"
        const listaJuegos = Array.isArray(data) ? data : (data.items || []);

        // 3. Verificamos que la lista no venga vacía
        if (!listaJuegos || listaJuegos.length === 0) {
            throw new Error("El juego no existe en la base de datos.");
        }

        // 4. Tomamos el primer (y único) juego de esa lista
        const juego = listaJuegos[0];

        // 2. Inyectar en el DOM mapeando las propiedades de tu GameReadDto

        // Imagen segura por si viene nula
        const imageUrl = juego.gameCoverUrl || 'https://images.unsplash.com/photo-1550745165-9bc0b252726f?auto=format&fit=crop&w=800&q=80';

        // 1. Asignamos la imagen al fondo gigante (el HTML ya se encarga de ponerle el blur y oscurecerlo)
        const heroBg = document.getElementById('hero-background');
        if (heroBg) {
            heroBg.style.backgroundImage = `url('${imageUrl}')`;
            heroBg.style.backgroundSize = 'cover';
            heroBg.style.backgroundPosition = 'center top';
        }

        // 2. Asignamos la misma imagen al póster pequeño para que se vea perfectamente nítida
        const gamePoster = document.getElementById('game-poster');
        if (gamePoster) {
            gamePoster.src = imageUrl;
        }

        document.getElementById('game-title').textContent = juego.name;

        // Formateo de fecha validando nulos
        if (juego.releaseDate) {
            const fecha = new Date(juego.releaseDate).toLocaleDateString('es-ES', { year: 'numeric', month: 'long', day: 'numeric' });
            document.getElementById('game-release').innerHTML = `<i class="bi bi-calendar3"></i> Lanzamiento: ${fecha}`;
        } else {
            document.getElementById('game-release').textContent = "Lanzamiento: Desconocido";
        }

        // Fíjate que usamos shortDescription, igual que en tu C#
        document.getElementById('game-description').textContent = juego.shortDescription || 'Sin descripción disponible para este título.';

        // Inyectar géneros leyendo la propiedad 'description' del GenreDto
        const containerGeneros = document.getElementById('game-genres');
        containerGeneros.innerHTML = ''; // Limpiamos si había algo antes

        if (juego.genres && juego.genres.length > 0) {
            juego.genres.forEach(g => {
                containerGeneros.innerHTML += `<span class="badge bg-primary border border-secondary">${g.description}</span>`;
            });
        } else {
            containerGeneros.innerHTML = `<span class="badge bg-dark border border-secondary text-secondary">Sin clasificar</span>`;
        }
     

    } catch (error) {
        console.error("Error cargando el juego:", error);
        document.getElementById('game-title').textContent = "Error de conexión";
        document.getElementById('game-description').textContent = "No pudimos conectar con la base de datos para traer este juego.";
    }
}

// ==========================================
// SISTEMA INTERACTIVO DE ESTRELLAS
// ==========================================
function configurarSistemaEstrellas() {
    const stars = document.querySelectorAll('.star-btn');
    const ratingInput = document.getElementById('rating-value');

    stars.forEach(star => {
        // Efecto Hover (Al pasar el mouse)
        star.addEventListener('mouseover', function () {
            const value = this.getAttribute('data-value');
            iluminarEstrellas(stars, value);
        });

        // Quitar hover (Al sacar el mouse)
        star.addEventListener('mouseout', function () {
            const currentValue = ratingInput.value;
            iluminarEstrellas(stars, currentValue); // Vuelve al valor seleccionado
        });

        // Efecto Click (Guardar el valor)
        star.addEventListener('click', function () {
            const value = this.getAttribute('data-value');
            ratingInput.value = value;
            iluminarEstrellas(stars, value);

            // Animación de rebote (Opcional, se ve muy profesional)
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
            star.classList.add('bi-star-fill', 'text-warning'); // Estrella llena y amarilla
        } else {
            star.classList.remove('bi-star-fill', 'text-warning');
            star.classList.add('bi-star', 'text-secondary'); // Estrella vacía y gris
        }
    });
}

// ==========================================
// LÓGICA DEL FORMULARIO Y SEGURIDAD
// ==========================================
function verificarEstadoLogin() {
    const token = localStorage.getItem('jwtToken');
    if (!token) {
        // Si no está logueado, ocultamos el formulario y mostramos la advertencia
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

        // 🚨 Revisa tu Swagger: Asegúrate de que tu DTO en C# pida exactamente estas propiedades
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
            // 🚨 Revisa tu Swagger: Asegúrate de que la ruta POST sea correcta (Ej: /Review o /Review/Add)
            const response = await fetch(`${API_BASE_URL}/Reviews/Create/review`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}` // Necesitamos saber quién comenta
                },
                body: JSON.stringify(reviewDto)
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || "Error al publicar la reseña.");
            }

            // Limpiar formulario al tener éxito
            form.reset();
            document.getElementById('rating-value').value = 0;
            
            // Apagar las estrellas
            const stars = document.querySelectorAll('.star-btn');
            stars.forEach(s => {
                s.classList.remove('bi-star-fill', 'text-warning');
                s.classList.add('bi-star', 'text-secondary');
            });

            // Recargar la lista de reseñas para que aparezca la nuestra inmediatamente
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
        // 🚨 Revisa tu Swagger: Ruta GET para traer reseñas de un juego específico
        // Puede ser algo como /Review/Game?gameId=... o /Review/GetByGame/ID
        const response = await fetch(`${API_BASE_URL}/Reviews/GET/reviews?gameId=${gameId}`);
        
        if (!response.ok) {
            throw new Error("No se pudieron cargar las reseñas.");
        }

        const resenas = await response.json();
        lista.innerHTML = ''; // Limpiamos el mensaje de carga

        if (!resenas || resenas.length === 0) {
            lista.innerHTML = '<p class="text-secondary fst-italic">Aún no hay reseñas. ¡Sé el primero en opinar!</p>';
            return;
        }

        // Iterar sobre las reseñas reales
        resenas.forEach(resena => {
            // Asegúrate de que los nombres de propiedades (userName, rating, comment) coincidan con tu DTO de C#
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

// Generador visual de estrellas en HTML
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

    // 1. Validar si el usuario está logueado
    if (!token) {
        alert("¡Alto ahí! Debes iniciar sesión para agregar juegos a tu biblioteca.");
        window.location.href = 'login.html';
        return;
    }

    // 2. Efecto visual de carga en el botón
    const contenidoOriginal = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Agregando...';

    try {
        // 3. Petición POST al Backend enviando el Token de seguridad
        // 🚨 ATENCIÓN: Revisa en tu Swagger la ruta exacta. Usualmente es algo como /Library, /UserGame o /Library/Add
        const response = await fetch(`${API_BASE_URL}/Library/AddGame/Library`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}` // ¡Aquí va la llave mágica!
            },
            // Enviamos el ID del juego. Asegúrate de que coincida con el DTO que espera tu C#
            body: JSON.stringify({ gameId: gameId }) 
        });

        if (!response.ok) {
            // Si da error (por ejemplo, si el juego ya estaba en la biblioteca)
            const errorText = await response.text();
            throw new Error(errorText || "No se pudo agregar el juego.");
        }

        // 4. ¡Éxito! Cambiamos el botón a verde
        btn.innerHTML = '<i class="bi bi-check-lg"></i> En tu biblioteca';
        btn.classList.remove('btn-accent');
        btn.classList.add('btn-success');
        
        // Lo dejamos deshabilitado para que no haga clic 2 veces
        btn.disabled = true; 

    } catch (error) {
        console.error("Error al agregar a biblioteca:", error);
        alert(error.message);
        
        // Restaurar el botón si falló
        btn.disabled = false;
        btn.innerHTML = contenidoOriginal;
    }
}
async function verificarSiEstaEnBiblioteca(gameId) {
    const token = localStorage.getItem('jwtToken');
    const btn = document.getElementById('btn-add-library');

    // Si no está logueado o no existe el botón, no tiene sentido verificar
    if (!token || !btn) return;

    try {
        // Hacemos una petición GET a tu endpoint de Biblioteca
        // 🚨 Revisa en Swagger: Este debe ser el endpoint que te devuelve todos los juegos del usuario
        const response = await fetch(`${API_BASE_URL}/Library/GET/Library/Games`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

      if (response.ok) {
            const miBiblioteca = await response.json();
            
            // 💡 ESPÍA: Mira la consola (F12) para ver qué está devolviendo C#
            console.log("Juegos en mi biblioteca:", miBiblioteca);
            console.log("Buscando el ID exacto:", gameId);
            
            // Búsqueda a prueba de balas (convierte todo a texto minúscula para comparar)
            const idBuscado = gameId.toString().toLowerCase();

            const yaLoTengo = miBiblioteca.some(juego => {
                // Verificamos cuál propiedad existe en tu DTO (id o gameId) y la comparamos
                const idDelJuego = (juego.id || juego.gameId || '').toString().toLowerCase();
                return idDelJuego === idBuscado;
            });

            if (yaLoTengo) {
                // Transformamos el botón a estado "Comprado/Agregado"
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