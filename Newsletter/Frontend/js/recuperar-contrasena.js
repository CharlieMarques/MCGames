let resetUserId = null;
let resetToken = null;

document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('forgotForm').addEventListener('submit', handleForgot);
    document.getElementById('resetForm').addEventListener('submit', handleReset);
    document.getElementById('btnPedirOtro').addEventListener('click', () => mostrarEstado('pedir'));

    const params = new URLSearchParams(window.location.search);
    resetUserId = params.get('userId');
    resetToken = params.get('token');

    mostrarEstado(resetUserId && resetToken ? 'nueva' : 'pedir');
});

// ---------- Modo 1: pedir el link ----------

async function handleForgot(event) {
    event.preventDefault();

    const email = document.getElementById('forgotEmail').value.trim();
    const btnForgot = document.getElementById('btnForgot');

    btnForgot.disabled = true;
    btnForgot.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Enviando...';

    try {
        const response = await fetch(`${API_BASE_URL}/Auth/ForgotPassword`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: email })
        });

        const mensaje = response.status === 400
            ? 'Revisá que el email esté bien escrito.'
            : await response.text();

        mostrarAlerta('forgot-result', mensaje, response.ok ? 'alert-success' : 'alert-danger');
    } catch {
        mostrarAlerta('forgot-result', 'No pudimos conectar con el servidor. Probá de nuevo en unos minutos.', 'alert-danger');
    } finally {
        btnForgot.disabled = false;
        btnForgot.textContent = 'Enviar enlace';
    }
}

// ---------- Modo 2: elegir la nueva contraseña ----------

async function handleReset(event) {
    event.preventDefault();

    const newPassword = document.getElementById('newPassword').value;
    const confirmPassword = document.getElementById('confirmPassword').value;
    const btnReset = document.getElementById('btnReset');

    ocultarErroresReset();

    if (newPassword !== confirmPassword) {
        mostrarErroresReset(['Las contraseñas no coinciden.']);
        return;
    }
    if (newPassword.length < 6) {
        mostrarErroresReset(['La contraseña debe tener al menos 6 caracteres.']);
        return;
    }

    btnReset.disabled = true;
    btnReset.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Guardando...';

    try {
        const response = await fetch(`${API_BASE_URL}/Auth/ResetPassword`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                userId: resetUserId,
                token: resetToken,
                newPassword: newPassword,
                confirmPassword: confirmPassword
            })
        });

        if (response.ok) {
            // Sacamos el token de la barra de direcciones: ya se usó y no debería quedar en el historial.
            history.replaceState(null, '', window.location.pathname);
            document.getElementById('mensaje-exito').textContent = await response.text();
            mostrarEstado('exito');
            return;
        }

        const data = await leerJson(response);
        if (data && data.invalidLink) {
            mostrarErroresReset(data.errors || ['El enlace no es válido o venció.'], true);
        } else {
            mostrarErroresReset(extraerErrores(data));
        }
    } catch {
        mostrarErroresReset(['No pudimos conectar con el servidor. Probá de nuevo en unos minutos.']);
    } finally {
        btnReset.disabled = false;
        btnReset.textContent = 'Guardar contraseña';
    }
}

// ---------- Ayudas ----------

async function leerJson(response) {
    try {
        return await response.json();
    } catch {
        return null;
    }
}

// La API devuelve { errors: [...] }; la validación automática de ASP.NET devuelve { errors: { Campo: [...] } }.
function extraerErrores(data) {
    if (!data || !data.errors) {
        return ['No se pudo cambiar la contraseña. Probá de nuevo.'];
    }
    return Array.isArray(data.errors) ? data.errors : Object.values(data.errors).flat();
}

function mostrarErroresReset(errores, linkInvalido = false) {
    const div = document.getElementById('reset-errors');
    div.replaceChildren();
    if (errores.length === 1) {
        div.textContent = errores[0];
    } else {
        const lista = document.createElement('ul');
        lista.className = 'mb-0';
        errores.forEach(error => {
            const item = document.createElement('li');
            item.textContent = error;
            lista.appendChild(item);
        });
        div.appendChild(lista);
    }
    div.classList.remove('d-none');
    document.getElementById('btnPedirOtro').classList.toggle('d-none', !linkInvalido);
}

function ocultarErroresReset() {
    document.getElementById('reset-errors').classList.add('d-none');
    document.getElementById('btnPedirOtro').classList.add('d-none');
}

function mostrarAlerta(id, mensaje, clase) {
    const div = document.getElementById(id);
    div.textContent = mensaje;
    div.className = `alert mt-3 ${clase}`;
}

function mostrarEstado(estado) {
    ['pedir', 'nueva', 'exito'].forEach(nombre => {
        document.getElementById(`estado-${nombre}`).classList.toggle('d-none', nombre !== estado);
    });
}
