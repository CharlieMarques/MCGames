const API_BASE_URL = 'https://localhost:44347';

function getAuthHeaders(){
    const token = localStorage.getItem('jwtToken');
    return {
        'Content-Type': 'application/json',
        'Authorization' : token ? `Bearer ${token}` : ''
    };
}