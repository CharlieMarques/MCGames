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

        // Llenar inputs
        document.getElementById('input-username').value = username;
        document.getElementById('input-email').value = email;

    } catch (e) {
        console.error("Error leyendo token:", e);
    }
}
function configurarEventosPerfil() {
    
    const inputUpload = document.getElementById('profile-upload');
    const imgPreview = document.getElementById('profile-preview');

    inputUpload.addEventListener('change', function(e) {
        const file = e.target.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function(evento) {
                imgPreview.src = evento.target.result;
            };
            reader.readAsDataURL(file);
        }
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
            document.getElementById('input-new-password').value = '';
            document.getElementById('input-confirm-password').value = '';
        }
    });

    const formPerfil = document.getElementById('form-perfil');
    formPerfil.addEventListener('submit', async (e) => {
        e.preventDefault();

        const btnSave = document.getElementById('btn-save-profile');
        const originalText = btnSave.innerHTML;
        btnSave.disabled = true;
        btnSave.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Guardando...';

        const nombre = document.getElementById('input-nombre').value;
        const apellido = document.getElementById('input-apellido').value;
        const newPassword = document.getElementById('input-new-password').value;
        const confirmPassword = document.getElementById('input-confirm-password').value;
        const foto = inputUpload.files[0];

        if (!passSection.classList.contains('d-none')) {
            if (newPassword.length < 6) {
                alert("La nueva contraseña debe tener al menos 6 caracteres.");
                btnSave.disabled = false;
                btnSave.innerHTML = originalText;
                return;
            }
            if (newPassword !== confirmPassword) {
                alert("Las contraseñas no coinciden.");
                btnSave.disabled = false;
                btnSave.innerHTML = originalText;
                return;
            }
        }

        try {
            const formData = new FormData();
            formData.append('Nombre', nombre);
            formData.append('Apellido', apellido);
            
            if (newPassword) formData.append('NewPassword', newPassword);
            if (foto) formData.append('ProfileImage', foto); // Archivo físico

            await new Promise(r => setTimeout(r, 1000));
            alert("¡Perfil actualizado con éxito!");

        } catch (error) {
            console.error(error);
            alert("Hubo un problema guardando los cambios.");
        } finally {
            btnSave.disabled = false;
            btnSave.innerHTML = originalText;
        }
    });
}