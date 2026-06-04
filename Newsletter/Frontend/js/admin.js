
let modalJuegoBootstrap;
let adminCurrentPage = 1;
const adminPageSize = 10; 
let adminSearchTerm = '';

document.addEventListener('DOMContentLoaded', () => {
    renderizarNavbar();
    verificarEstadoAuth();

    // 1. SEGURIDAD: Verificar que sea Admin
    const token = localStorage.getItem('jwtToken');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }

    try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
        if (role !== 'Admin' && (!Array.isArray(role) || !role.includes('Admin'))) {
            alert("Acceso denegado. Área exclusiva para administradores.");
            window.location.href = 'index.html';
            return;
        }
    } catch (e) {
        window.location.href = 'index.html';
        return;
    }

    modalJuegoBootstrap = new bootstrap.Modal(document.getElementById('modalJuego'));
    modalGeneroBootstrap = new bootstrap.Modal(document.getElementById('modalGenero'));
    modalPlataformaBootstrap = new bootstrap.Modal(document.getElementById('modalPlataforma'));

    cargarJuegosAdmin();
    cargarGenerosAdmin();
    cargarPlataformasAdmin();

  
    document.getElementById('admin-search-game').addEventListener('keypress', (e) => {
        if (e.key === 'Enter') buscarJuegoAdmin();
    });
});

function buscarJuegoAdmin() {
    adminSearchTerm = document.getElementById('admin-search-game').value.trim();
    cargarJuegosAdmin(1); // Siempre que buscamos algo nuevo, volvemos a la página 1
}

async function cargarJuegosAdmin(page = 1) {
    adminCurrentPage = page;
    const tbody = document.getElementById('admin-games-table');
    tbody.innerHTML = '<tr><td colspan="5" class="text-center py-4"><span class="spinner-border text-accent"></span></td></tr>';

    try {
        let url = `${API_BASE_URL}/Game/GET/games?page=${adminCurrentPage}&pageSize=${adminPageSize}`;
        if (adminSearchTerm) {
            url += `&name=${encodeURIComponent(adminSearchTerm)}`;
        }

        const response = await fetch(url);
        if (!response.ok) throw new Error("Error cargando juegos");

        const data = await response.json();
        const juegos = Array.isArray(data) ? data : (data.items || []);
        const totalPages = data.totalPages || 1;

        tbody.innerHTML = '';

        if (juegos.length === 0) {
            tbody.innerHTML = '<tr><td colspan="5" class="text-center py-4 text-secondary">No se encontraron juegos.</td></tr>';
            document.getElementById('admin-games-pagination').innerHTML = '';
            return;
        }

        juegos.forEach(juego => {
            const img = juego.gameCoverUrl || 'https://via.placeholder.com/50x70?text=No+Img';
            const estadoBadge = juego.state
                ? '<span class="badge bg-success">Activo</span>'
                : '<span class="badge bg-danger">Inactivo</span>';

            tbody.innerHTML += `
                <tr>
                    <td><img src="${img}" alt="${juego.name}" style="width: 50px; height: 70px; object-fit: cover; border-radius: 4px;"></td>
                    <td class="fw-bold text-white">${juego.name}</td>
                    <td>$${juego.price.toFixed(2)}</td>
                    <td>${estadoBadge}</td>
                    <td class="text-end">
                        <button class="btn btn-sm btn-outline-info me-1" onclick="abrirModalEditar('${juego.id}')" title="Editar">
                            <i class="bi bi-pencil-square"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="eliminarJuego('${juego.id}')" title="Eliminar">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                </tr>
            `;
        });

        dibujarPaginacionAdmin(totalPages);

    } catch (error) {
        console.error(error);
        tbody.innerHTML = '<tr><td colspan="5" class="text-center py-4 text-danger">Error conectando con la base de datos.</td></tr>';
    }
}
function dibujarPaginacionAdmin(totalPages) {
    const controls = document.getElementById('admin-games-pagination');
    if (!controls) return;
    controls.innerHTML = '';

    if (totalPages <= 1) return;

    // 1. Botón Anterior
    const prevDisabled = adminCurrentPage === 1 ? 'disabled' : '';
    controls.innerHTML += `
        <li class="page-item ${prevDisabled}">
            <button class="page-link bg-dark border-secondary text-light" onclick="cargarJuegosAdmin(${adminCurrentPage - 1})">&laquo;</button>
        </li>
    `;

    let delta = 2; 
    let range = [];
    let rangeWithDots = [];
    let l;

    for (let i = 1; i <= totalPages; i++) {
        if (i === 1 || i === totalPages || (i >= adminCurrentPage - delta && i <= adminCurrentPage + delta)) {
            range.push(i);
        }
    }


    for (let i of range) {
        if (l) {
            if (i - l === 2) {
                rangeWithDots.push(l + 1);
            } else if (i - l !== 1) {
                // Si el salto es mayor, ponemos los puntos
                rangeWithDots.push('...');
            }
        }
        rangeWithDots.push(i);
        l = i;
    }

    for (let i of rangeWithDots) {
        if (i === '...') {
            controls.innerHTML += `<li class="page-item disabled"><span class="page-link bg-dark border-secondary text-secondary">...</span></li>`;
        } else {
            const activeClass = i === adminCurrentPage ? 'active bg-accent border-accent text-white' : 'bg-dark border-secondary text-light';
            controls.innerHTML += `
                <li class="page-item">
                    <button class="page-link ${activeClass}" onclick="cargarJuegosAdmin(${i})">${i}</button>
                </li>
            `;
        }
    }


    const nextDisabled = adminCurrentPage === totalPages ? 'disabled' : '';
    controls.innerHTML += `
        <li class="page-item ${nextDisabled}">
            <button class="page-link bg-dark border-secondary text-light" onclick="cargarJuegosAdmin(${adminCurrentPage + 1})">&raquo;</button>
        </li>
    `;
}
function abrirModalJuego() {
    document.getElementById('form-juego').reset();
    document.getElementById('juego-id').value = '';
    document.getElementById('modalJuegoTitle').textContent = 'Nuevo Juego';
    modalJuegoBootstrap.show();
}

async function abrirModalEditar(id) {
    try {
        const response = await fetch(`${API_BASE_URL}/Game/GET/games?id=${id}&page=1&pageSize=1`);
        const data = await response.json();
        const lista = Array.isArray(data) ? data : (data.items || []);

        if (lista.length > 0) {
            const juego = lista[0];

            document.getElementById('juego-id').value = juego.id;
            document.getElementById('juego-nombre').value = juego.name;
            document.getElementById('juego-precio').value = juego.price;
            document.getElementById('juego-desc').value = juego.shortDescription || '';
            document.getElementById('juego-imagen').value = juego.gameCoverUrl || '';
            document.getElementById('juego-estado').checked = juego.state;

            document.getElementById('modalJuegoTitle').textContent = 'Editar Juego';
            modalJuegoBootstrap.show();
        }
    } catch (error) {
        alert("Error al cargar los datos del juego.");
    }
}

async function guardarJuego() {
    const id = document.getElementById('juego-id').value;
    const nombre = document.getElementById('juego-nombre').value;
    const precio = parseFloat(document.getElementById('juego-precio').value);
    const desc = document.getElementById('juego-desc').value;
    const imagen = document.getElementById('juego-imagen').value;
    const estado = document.getElementById('juego-estado').checked;

    if (!nombre || isNaN(precio)) {
        alert("El nombre y el precio son obligatorios.");
        return;
    }

    const juegoData = {
        Name: nombre,
        Price: precio,
        ShortDescription: desc,
        GameCoverUrl: imagen,
        State: estado
    };

    const token = localStorage.getItem('jwtToken');
    const method = id ? 'PUT' : 'POST';
    const url = id ? `${API_BASE_URL}/Game/Edit/game/${id}` : `${API_BASE_URL}/Game/Create/game`;

    try {
        const response = await fetch(url, {
            method: method,
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify(juegoData)
        });

        if (!response.ok) throw new Error("Error al guardar");

        modalJuegoBootstrap.hide();
        cargarJuegosAdmin();

    } catch (error) {
        alert("Ocurrió un error al intentar guardar el juego.");
        console.error(error);
    }
}



async function eliminarJuego(id) {
    if (confirm("¿Estás seguro de que deseas eliminar este juego? Esta acción no se puede deshacer.")) {
        const token = localStorage.getItem('jwtToken');

        try {
            const response = await fetch(`${API_BASE_URL}/Game/DELETE/game/${id}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });

            if (!response.ok) throw new Error("Error al eliminar");

            cargarJuegosAdmin(); 

        } catch (error) {
            alert("No se pudo eliminar el juego. Puede que tenga ventas asociadas.");
            console.error(error);
        }
    }
}
async function cargarGenerosAdmin() {
    const tbody = document.getElementById('admin-genres-table');
    tbody.innerHTML = '<tr><td colspan="3" class="text-center py-4"><span class="spinner-border text-accent"></span></td></tr>';

    try {
        const response = await fetch(`${API_BASE_URL}/Genre/GET/genres`);
        if (!response.ok) throw new Error("Error de conexión");
        
        const generos = await response.json();
        tbody.innerHTML = '';

        if (generos.length === 0) {
            tbody.innerHTML = '<tr><td colspan="3" class="text-center py-4 text-secondary">No hay géneros.</td></tr>';
            return;
        }

        generos.forEach(g => {
            const descripcion = g.description || g.name || 'Desconocido';
            tbody.innerHTML += `
                <tr>
                    <td class="text-secondary">#${g.id}</td>
                    <td class="fw-bold text-white">${descripcion}</td>
                    <td class="text-end">
                        <button class="btn btn-sm btn-outline-info me-1" onclick="abrirModalGenero('${g.id}', '${descripcion}')" title="Editar">
                            <i class="bi bi-pencil-square"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="eliminarGenero('${g.id}')" title="Eliminar">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                </tr>
            `;
        });
    } catch (error) {
        tbody.innerHTML = '<tr><td colspan="3" class="text-center py-4 text-danger">Error conectando con la BD.</td></tr>';
    }
}

function abrirModalGenero(id = '', descripcion = '') {
    document.getElementById('form-genero').reset();
    document.getElementById('genero-id').value = id;
    document.getElementById('genero-desc').value = descripcion;
    document.getElementById('modalGeneroTitle').textContent = id ? 'Editar Género' : 'Nuevo Género';
    modalGeneroBootstrap.show();
}

async function guardarGenero() {
    const id = document.getElementById('genero-id').value;
    const desc = document.getElementById('genero-desc').value.trim();

    if (!desc) { alert("La descripción es obligatoria."); return; }

    const token = localStorage.getItem('jwtToken');
    const method = id ? 'PUT' : 'POST';
    const url = id ? `${API_BASE_URL}/Genre/PUT/genres/${id}` : `${API_BASE_URL}/Genre/POST/genres`;

    try {
        const response = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
            body: JSON.stringify({ description: desc })
        });

        if (!response.ok) throw new Error("Error al guardar");

        modalGeneroBootstrap.hide();
        cargarGenerosAdmin(); 
    } catch (error) {
        alert("Error guardando el género.");
    }
}

async function eliminarGenero(id) {
    if (confirm("¿Seguro que deseas eliminar este género?")) {
        const token = localStorage.getItem('jwtToken');
        try {
            const response = await fetch(`${API_BASE_URL}/Genre/DELETE/genres/${id}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!response.ok) throw new Error("Error al eliminar");
            cargarGenerosAdmin();
        } catch (error) {
            alert("No se pudo eliminar. Quizás hay juegos usándolo.");
        }
    }
}
async function cargarPlataformasAdmin() {
    const tbody = document.getElementById('admin-platforms-table');
    if (!tbody) return;
    
    tbody.innerHTML = '<tr><td colspan="3" class="text-center py-4"><span class="spinner-border text-accent"></span> Cargando plataformas...</td></tr>';

    try {
        const response = await fetch(`${API_BASE_URL}/Platform/GET/platforms`); 
        
        if (!response.ok) {
            throw new Error(`Error de servidor: ${response.status}`);
        }
        
        const plataformas = await response.json();
        tbody.innerHTML = '';

        if (!plataformas || plataformas.length === 0) {
            tbody.innerHTML = '<tr><td colspan="3" class="text-center py-4 text-secondary">No hay plataformas cargadas en la base de datos.</td></tr>';
            return;
        }

        plataformas.forEach(p => {
            const id = p.id !== undefined ? p.id : p.Id;
            const descripcion = p.description || p.Description || p.name || p.Name || 'Sin nombre';

            tbody.innerHTML += `
                <tr>
                    <td class="text-secondary">#${id}</td>
                    <td class="fw-bold text-white">${descripcion}</td>
                    <td class="text-end">
                        <button class="btn btn-sm btn-outline-info me-1" onclick="abrirModalPlataforma('${id}', '${descripcion}')" title="Editar">
                            <i class="bi bi-pencil-square"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="eliminarPlataforma('${id}')" title="Eliminar">
                            <i class="bi bi-trash"></i>
                        </button>
                    </td>
                </tr>
            `;
        });
    } catch (error) {
        console.error("Error en plataformas:", error);
        tbody.innerHTML = `<tr><td colspan="3" class="text-center py-4 text-danger"><i class="bi bi-exclamation-triangle me-2"></i> No se pudieron cargar las plataformas. Revisa la consola (F12).</td></tr>`;
    }
}

function abrirModalPlataforma(id = '', descripcion = '') {
    document.getElementById('form-plataforma').reset();
    document.getElementById('plataforma-id').value = id;
    document.getElementById('plataforma-desc').value = descripcion;
    document.getElementById('modalPlataformaTitle').textContent = id ? 'Editar Plataforma' : 'Nueva Plataforma';
    modalPlataformaBootstrap.show();
}

async function guardarPlataforma() {
    const id = document.getElementById('plataforma-id').value;
    const desc = document.getElementById('plataforma-desc').value.trim();

    if (!desc) { alert("La descripción es obligatoria."); return; }

    const token = localStorage.getItem('jwtToken');
    const method = id ? 'PUT' : 'POST';
    const url = id ? `${API_BASE_URL}/Platform/PUT/platforms/${id}` : `${API_BASE_URL}/Platform/POST/platforms`; // 🚨 Verifica tus endpoints

    try {
        const response = await fetch(url, {
            method: method,
            headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
            body: JSON.stringify({ description: desc })
        });

        if (!response.ok) throw new Error("Error al guardar");

        modalPlataformaBootstrap.hide();
        cargarPlataformasAdmin(); 
    } catch (error) {
        alert("Error guardando la plataforma.");
    }
}

async function eliminarPlataforma(id) {
    if (confirm("¿Seguro que deseas eliminar esta plataforma?")) {
        const token = localStorage.getItem('jwtToken');
        try {
            const response = await fetch(`${API_BASE_URL}/Platform/DELETE/platforms/${id}`, {
                method: 'DELETE',
                headers: { 'Authorization': `Bearer ${token}` }
            });
            if (!response.ok) throw new Error("Error al eliminar");
            cargarPlataformasAdmin();
        } catch (error) {
            alert("No se pudo eliminar. Quizás hay juegos usándola.");
        }
    }
}