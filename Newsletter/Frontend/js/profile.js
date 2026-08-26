document.addEventListener('DOMContentLoaded', () => {
    renderizarNavbar();
    verificarEstadoAuth();

    const token = localStorage.getItem('jwtToken');
    if (!token) {
        window.location.href = 'login.html';
        return;
    }

    cargarDatosIniciales(token);
    configurarEventosPerfil();
});

function cargarDatosIniciales(token) {
    try {
        const payloadBase64 = token.split('.')[1];
        const tokenData = JSON.parse(atob(payloadBase64));

        const username = tokenData.sub || tokenData.unique_name || tokenData.name || "Jugador";
        const email = tokenData.email || tokenData['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] || "correo@oculto.com";
        const role = tokenData['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || "Usuario";

        document.getElementById('display-username').textContent = username;

        let rolVisual = Array.isArray(role) ? (role.includes('Admin') ? 'Administrador' : 'Moderador') : role;
        document.getElementById('display-role').textContent = rolVisual === 'User' ? 'Jugador Verificado' : rolVisual;

        // llenar inputs
        document.getElementById('input-username').value = username;
        document.getElementById('input-email').value = email;

    } catch (e) {
        console.error("Error leyendo token:", e);
    }
}
function configurarEventosPerfil() {
    const toggleButtons = document.querySelectorAll('.toggle-visibility');
    toggleButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            const targetId = this.getAttribute('data-target');
            const input = document.getElementById(targetId);
            const icon = this.querySelector('i');

            if (input.type === 'password') {
                input.type = 'text';
                icon.classList.remove('bi-eye-fill');
                icon.classList.add('bi-eye-slash-fill');
            } else {
                input.type = 'password';
                icon.classList.remove('bi-eye-slash-fill');
                icon.classList.add('bi-eye-fill');
            }
        });
    });

    const btnTogglePass = document.getElementById('btn-toggle-password');
    const passSection = document.getElementById('password-section');

    btnTogglePass.addEventListener('click', () => {
        if (passSection.classList.contains('d-none')) {
            passSection.classList.remove('d-none');
            btnTogglePass.classList.add('btn-secondary', 'text-white');
            btnTogglePass.classList.remove('btn-outline-secondary');
            btnTogglePass.innerHTML = '<i class="bi bi-x-circle me-2"></i> Cancelar Cambio de Contraseña';
        } else {
            passSection.classList.add('d-none');
            btnTogglePass.classList.remove('btn-secondary', 'text-white');
            btnTogglePass.classList.add('btn-outline-secondary');
            btnTogglePass.innerHTML = '<i class="bi bi-key-fill me-2"></i> Cambiar Contraseña';

            document.getElementById('input-current-password').value = '';
            document.getElementById('input-new-password').value = '';
            document.getElementById('input-confirm-password').value = '';
        }
    });

    const formPerfil = document.getElementById('form-perfil');
    formPerfil.addEventListener('submit', async (e) => {
        e.preventDefault();
        if (passSection.classList.contains('d-none')) {
            alert("No has realizado ningún cambio para guardar.");
            return;
        }

        const currentPassword = document.getElementById('input-current-password').value;
        const newPassword = document.getElementById('input-new-password').value;
        const confirmPassword = document.getElementById('input-confirm-password').value;

        if (!currentPassword) {
            alert("Debes ingresar tu contraseña actual.");
            return;
        }

        if (newPassword.length < 6) {
            alert("La nueva contraseña debe tener al menos 6 caracteres.");
            return;
        }

        if (newPassword !== confirmPassword) {
            alert("Las contraseñas nuevas no coinciden.");
            return;
        }

        const btnSave = document.getElementById('btn-save-profile');
        const originalText = btnSave.innerHTML;

        btnSave.disabled = true;
        btnSave.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Guardando...';

        try {
            const token = localStorage.getItem('token');
            const response = await fetch(`${API_BASE_URL}/User/Change-Password`, {
                method: 'PATCH',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`
                },
                body: JSON.stringify({
                    CurrentPassword: currentPassword,
                    NewPassword: newPassword,
                    ConfirmPassword: confirmPassword
                })
            });

            const data = await response.json();

            if (!response.ok) {
                alert(data.message || "Error al intentar cambiar la contraseña.");
            } else {
                alert("¡Contraseña actualizada con éxito!");

                btnTogglePass.click();
            }

        } catch (error) {
            console.error("Error en la conexión:", error);
            alert("Hubo un problema de comunicación con el servidor.");
        } finally {
            btnSave.disabled = false;
            btnSave.innerHTML = originalText;
        }
    });
}