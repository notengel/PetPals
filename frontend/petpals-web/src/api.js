const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5296/api'

export function getStoredToken() {
  return localStorage.getItem('petpals_token')
}

export function storeToken(token) {
  localStorage.setItem('petpals_token', token)
}

export function clearStoredToken() {
  localStorage.removeItem('petpals_token')
}

export function getTokenRole(token) {
  if (!token) return null

  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    return payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload.role
  } catch {
    return null
  }
}

export async function apiRequest(path, options = {}) {
  const { token, ...fetchOptions } = options
  const headers = new Headers(fetchOptions.headers)
  headers.set('Content-Type', 'application/json')

  if (token) {
    headers.set('Authorization', `Bearer ${token}`)
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...fetchOptions,
    headers,
  })

  if (response.status === 204) {
    return null
  }

  const data = await response.json().catch(() => ({}))
  if (!response.ok) {
    const message = data.errors?.join?.(', ') || data.title || 'Request failed'
    throw new Error(message)
  }

  return data
}

export const authApi = {
  login: (body) => apiRequest('/auth/login', {
    method: 'POST',
    body: JSON.stringify(body),
  }),
  register: (body) => apiRequest('/auth/register', {
    method: 'POST',
    body: JSON.stringify(body),
  }),
}

export const socialApi = {
  profile: (token) => apiRequest('/social/profile/me', { token }),
  pets: (token) => apiRequest('/social/pets', { token }),
  createPet: (token, body) => apiRequest('/social/pets', {
    method: 'POST',
    body: JSON.stringify(body),
    token,
  }),
  feed: (token) => apiRequest('/social/feed?page=1&pageSize=20', { token }),
  createPost: (token, body) => apiRequest('/social/posts', {
    method: 'POST',
    body: JSON.stringify(body),
    token,
  }),
  like: (token, postId, liked) => apiRequest(`/social/posts/${postId}/likes`, {
    method: liked ? 'DELETE' : 'POST',
    token,
  }),
  comments: (token, postId) => apiRequest(`/social/posts/${postId}/comments`, { token }),
  addComment: (token, postId, body) => apiRequest(`/social/posts/${postId}/comments`, {
    method: 'POST',
    body: JSON.stringify(body),
    token,
  }),
}

export const marketplaceApi = {
  clinics: (token) => apiRequest('/marketplace/clinics', { token }),
  products: (token, clinicId, category) => {
    const params = new URLSearchParams()
    if (clinicId) params.set('clinicId', clinicId)
    if (category !== '') params.set('category', category)
    const query = params.toString()
    return apiRequest(`/marketplace/products${query ? `?${query}` : ''}`, { token })
  },
  cart: (token) => apiRequest('/marketplace/cart', { token }),
  addToCart: (token, productId, quantity = 1) => apiRequest('/marketplace/cart/items', {
    method: 'POST',
    body: JSON.stringify({ productId, quantity }),
    token,
  }),
  checkout: (token) => apiRequest('/marketplace/cart/checkout', {
    method: 'POST',
    token,
  }),
}

export const appointmentsApi = {
  services: (token, clinicId) => apiRequest(`/appointments/clinics/${clinicId}/services`, { token }),
  schedules: (token, clinicId) => apiRequest(`/appointments/clinics/${clinicId}/schedules`, { token }),
  mine: (token) => apiRequest('/appointments/mine', { token }),
  create: (token, body) => apiRequest('/appointments', {
    method: 'POST',
    body: JSON.stringify(body),
    token,
  }),
  cancel: (token, appointmentId) => apiRequest(`/appointments/${appointmentId}/cancel`, {
    method: 'POST',
    token,
  }),
}

export const adoptionApi = {
  pets: (token) => apiRequest('/adoptions/pets', { token }),
  requests: (token) => apiRequest('/adoptions/requests/mine', { token }),
  request: (token, petId, applicantMessage) => apiRequest(`/adoptions/pets/${petId}/requests`, {
    method: 'POST',
    body: JSON.stringify({ applicantMessage }),
    token,
  }),
  createPet: (token, body) => apiRequest('/adoptions/pets', {
    method: 'POST',
    body: JSON.stringify(body),
    token,
  }),
}
