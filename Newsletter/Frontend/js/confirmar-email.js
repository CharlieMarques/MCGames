document.addEventListener('DOMContentLoaded', () => {
    document.getElementById('resendForm').addEventListener('submit', handleResend);

    const params = new URLSearchParams(window.location.search);
    const userId = params.get('userId');
    const token = params.get('token');

    if (userId && token) {
        confirmarEmail(userId, token);
    } else {
        mostrarReenvio('Ingresá tu email y te mandamos un nuevo enlace de confirmación.', false);
    }
});

async function confirmarEmail(userId, token) {
    mostrarEstado('cargando');
    try {
        const url = `${API_BASE_URL}/Auth/ConfirmEmail?userId=${encodeURIComponent(userId)}&token=${encodeURIComponent(token)}`;
        const response = await fetch(url);
        const mensaje = await response.text();

        if (response.ok) {
            mostrarExito(mensaje || 'Email confirmado. Ya podés iniciar sesión.');
        } else {
            mostrarReenvio(mensaje || 'El enlace no es válido o venció. Pedí uno nuevo.', true);
        }
    } catch {
        mostrarReenvio('No pudimos conectar con el servidor. Probá de nuevo en unos minutos.', true);
    }
}

async function handleResend(event) {
    event.preventDefault();

    const email = document.getElementById('resendEmail').value.trim();
    const btnResend = document.getElementById('btnResend');

    btnResend.disabled = true;
    btnResend.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Enviando...';

    try {
        const response = await fetch(`${API_BASE_URL}/Auth/ResendConfirmation`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email: email })
        });

        // Un 400 trae los errores de validación en JSON; mostramos un texto simple.
        const mensaje = response.status === 400
            ? 'Revisá que el email esté bien escrito.'
            : await response.text();

        mostrarResultadoReenvio(mensaje, response.ok);
    } catch {
        mostrarResultadoReenvio('No pudimos conectar con el servidor. Probá de nuevo en unos minutos.', false);
    } finally {
        btnResend.disabled = false;
        btnResend.textContent = 'Reenviar enlace';
    }
}

function mostrarEstado(estado) {
    ['cargando', 'exito', 'reenvio'].forEach(nombre => {
        document.getElementById(`estado-${nombre}`).classList.toggle('d-none', nombre !== estado);
    });
}

function mostrarExito(mensaje) {
    document.getElementById('mensaje-exito').textContent = mensaje;
    mostrarEstado('exito');
}

function mostrarReenvio(mensaje, esError) {
    const div = document.getElementById('mensaje-reenvio');
    div.textContent = mensaje;
    div.className = `alert ${esError ? 'alert-warning' : 'alert-info'}`;
    mostrarEstado('reenvio');
}

function mostrarResultadoReenvio(mensaje, ok) {
    const div = document.getElementById('resend-result');
    div.textContent = mensaje;
    div.className = `alert mt-3 ${ok ? 'alert-success' : 'alert-danger'}`;
}
