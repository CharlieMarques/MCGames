document.addEventListener('DOMContentLoaded', () => {
    const registerForm = document.getElementById('registerForm');
    
    // Verificamos si estamos en la página de registro (para que no falle en el login)
    if (registerForm) {
        registerForm.addEventListener('submit', handleRegister);
    }
    if (loginForm) {
        loginForm.addEventListener('submit', handleLogin);
    }
});

async function handleRegister(event) {
    // 1. Evitamos que el formulario recargue la página web
    event.preventDefault();

    // 2. Capturamos los elementos del DOM
    const username = document.getElementById('username').value;
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    const confirmPassword = document.getElementById('confirmPassword').value;
    const errorDiv = document.getElementById('error-message');
    const btnSubmit = document.getElementById('btnSubmit');

    // Ocultar mensaje de error anterior
    errorDiv.classList.add('d-none');

    // 3. Validación básica Frontend
    if (password !== confirmPassword) {
        mostrarError("Las contraseñas no coinciden. Inténtalo de nuevo.");
        return;
    }

    if (password.length < 6) {
        mostrarError("La contraseña debe tener al menos 6 caracteres.");
        return;
    }

    // 4. Preparamos el objeto a enviar (Asegúrate de que coincida con tu DTO de C#)
    const userDto = {
        userName: username,
        email: email,
        password: password,
        confirmPassword: confirmPassword
    };

    // Deshabilitar el botón mientras carga para evitar doble clic
    btnSubmit.disabled = true;
    btnSubmit.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Registrando...';

    try {
        // 5. Llamada a tu API (Ajusta 'Auth/Register' según tu controlador en C#)
        const response = await fetch(`${API_BASE_URL}/Auth/Register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(userDto)
        });

        if (!response.ok) {
            // Si el backend devuelve un BadRequest (ej. correo duplicado)
            const errorText = await response.text();
            throw new Error(errorText || "Ocurrió un error en el servidor");
        }

        // 6. ¡Éxito! 
        // Si tu backend hace auto-login al registrar, aquí guardarías el token.
        // Si no, simplemente redirigimos al login.
        alert("¡Cuenta creada con éxito! Ahora puedes iniciar sesión.");
        window.location.href = 'login.html';

    } catch (error) {
        mostrarError(error.message);
    } finally {
        // Restaurar el botón a su estado normal
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
        // Asegúrate de que la URL coincida con el endpoint de tu Controlador de C#
        const response = await fetch(`${API_BASE_URL}/Auth/Login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(loginDto)
        });

        if (!response.ok) {
            // Manejamos el error (ej. credenciales incorrectas 401)
            const errorText = await response.text();
            throw new Error(errorText || "Usuario o contraseña incorrectos.");
        }

        // Si el login es exitoso, tu API de C# debe devolver el Token (y quizás otros datos)
        // Por ejemplo: { token: "eyJh...", username: "GamerX", role: "User" }
        const data = await response.json();

        // 💡 EL PASO MÁS IMPORTANTE: Guardamos el token en la bóveda del navegador
        // Ajusta "data.token" al nombre exacto de la propiedad que devuelva tu C#
        localStorage.setItem('jwtToken', data.token); 
        
        // (Opcional) Guardar el nombre de usuario para mostrarlo en el Navbar
        if (data.username) {
            localStorage.setItem('username', data.username);
        }

        // Redirigir a la biblioteca o al inicio
        window.location.href = 'biblioteca.html';

    } catch (error) {
        mostrarError(error.message);
    } finally {
        btnSubmit.disabled = false;
        btnSubmit.innerHTML = 'Ingresar';
    }
}

// Función auxiliar para mostrar errores visualmente
function mostrarError(mensaje) {
    const errorDiv = document.getElementById('error-message');
    errorDiv.textContent = mensaje;
    errorDiv.classList.remove('d-none');
}