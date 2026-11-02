document.addEventListener('DOMContentLoaded', () => {
    const registerForm = document.getElementById('registerForm');
    const loginForm = document.getElementById('loginForm');
    
    if (registerForm) {
        registerForm.addEventListener('submit', handleRegister);
    }
    if (loginForm) {
        loginForm.addEventListener('submit', handleLogin);
    }
});

async function handleRegister(event) {
    event.preventDefault();

    const username = document.getElementById('username').value;
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    const confirmPassword = document.getElementById('confirmPassword').value;
    const errorDiv = document.getElementById('error-message');
    const btnSubmit = document.getElementById('btnSubmit');

    errorDiv.classList.add('d-none');

    if (password !== confirmPassword) {
        mostrarError("Las contraseñas no coinciden. Inténtalo de nuevo.");
        return;
    }

    if (password.length < 6) {
        mostrarError("La contraseña debe tener al menos 6 caracteres.");
        return;
    }

    const userDto = {
        userName: username,
        email: email,
        password: password,
        confirmPassword: confirmPassword
    };

    btnSubmit.disabled = true;
    btnSubmit.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Registrando...';

    try {
        const response = await fetch(`${API_BASE_URL}/Auth/Register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(userDto)
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || "Ocurrió un error en el servidor");
        }

        alert("¡Cuenta creada! Te enviamos un email para confirmarla. Revisá tu bandeja de entrada (y la carpeta de spam).");
        window.location.href = 'login.html';

    } catch (error) {
        mostrarError(error.message);
    } finally {
        btnSubmit.disabled = false;
        btnSubmit.innerHTML = 'Registrarse';
    }
}



async function handleLogin(event) {
    event.preventDefault();

    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    const errorDiv = document.getElementById('error-message');
    const btnSubmit = document.getElementById('btnSubmit');

    errorDiv.classList.add('d-none');

    const loginDto = {
        username: username,
        password: password
    };

    btnSubmit.disabled = true;
    btnSubmit.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Ingresando...';

    try {
        const response = await fetch(`${API_BASE_URL}/Auth/Login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(loginDto)
        });

        if (!response.ok) {
            const errorText = await response.text();
            if (response.status === 403) {
                // Email sin confirmar: mostramos el mensaje con el link para reenviarlo.
                mostrarErrorConReenvio(errorText);
                return;
            }
            throw new Error(errorText || "Usuario o contraseña incorrectos.");
        }

        const data = await response.json();
        localStorage.setItem('jwtToken', data.token); 
        if (data.username) {
            localStorage.setItem('username', data.username);
        }
        window.location.href = 'biblioteca.html';

    } catch (error) {
        mostrarError(error.message);
    } finally {
        btnSubmit.disabled = false;
        btnSubmit.innerHTML = 'Ingresar';
    }
}

function mostrarError(mensaje) {
    const errorDiv = document.getElementById('error-message');
    errorDiv.textContent = mensaje;
    errorDiv.classList.remove('d-none');
}

function mostrarErrorConReenvio(mensaje) {
    const errorDiv = document.getElementById('error-message');
    errorDiv.textContent = (mensaje || 'Tenés que confirmar tu email antes de iniciar sesión.') + ' ';
    const link = document.createElement('a');
    link.href = 'ConfirmarEmail.html?reenviar=1';
    link.className = 'alert-link';
    link.textContent = 'Reenviar email de confirmación';
    errorDiv.appendChild(link);
    errorDiv.classList.remove('d-none');
}
