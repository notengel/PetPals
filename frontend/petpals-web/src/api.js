const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5296/api'
export const API_URL = API_BASE_URL

export function getStoredToken() {
  return localStorage.getItem('petpals_token')
}

export function storeToken(token) {
  localStorage.setItem('petpals_token', token)
}

export function clearStoredToken() {
  localStorage.removeItem('petpals_token')
}

export async function apiUpload(path, token, file) {
  const form = new FormData()
  form.append('file', file)
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'POST',
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    body: form,
  })
  const data = await response.json().catch(() => ({}))
  if (!response.ok) throw new Error(data.errors?.join?.(', ') || data.title || 'Upload failed')
  return data
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

  if (response.status === 401) {
    // Disparar evento para forzar logout global
    window.dispatchEvent(new CustomEvent('petpals:auth-expired'))
    throw new Error('Sesión expirada')
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
  changeEmail: (token, newEmail) => apiRequest('/auth/email', {
    method: 'PUT',
    body: JSON.stringify({ newEmail }),
    token,
  }),
  changePassword: (token, body) => apiRequest('/auth/password', {
    method: 'PUT',
    body: JSON.stringify(body),
    token,
  }),
}

export const socialApi = {
  profile: (token) => apiRequest('/social/profile/me', { token }),
  updateProfile: (token, body) => apiRequest('/social/profile/me', {
    method: 'PUT',
    body: JSON.stringify(body),
    token,
  }),
  myPosts: (token) => apiRequest('/social/profile/me/posts?page=1&pageSize=50', { token }),
  uploadAvatar: (token, file) => apiUpload('/social/profile/me/avatar', token, file),
  uploadBanner: (token, file) => apiUpload('/social/profile/me/banner', token, file),
  petPhotos: (token, petId) => apiRequest(`/social/pets/${petId}/photos`, { token }),
  uploadPetPhoto: (token, petId, file) => apiUpload(`/social/pets/${petId}/photos`, token, file),
  deletePetPhoto: (token, petId, photoId) => apiRequest(`/social/pets/${petId}/photos/${photoId}`, {
    method: 'DELETE',
    token,
  }),
  setPetPrimary: (token, petId, photoId) => apiRequest(`/social/pets/${petId}/primary`, {
    method: 'PUT',
    body: JSON.stringify({ photoId }),
    token,
  }),
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
  myClinic: (token) => apiRequest('/marketplace/clinics/me', { token }),
  saveClinic: (token, body) => apiRequest('/marketplace/clinics/me', {
    method: 'PUT',
    body: JSON.stringify(body),
    token,
  }),
  clinic: (token, clinicId) => apiRequest(`/marketplace/clinics/${clinicId}`, { token }),
  clinicProfile: (token, clinicId) => apiRequest(`/marketplace/clinics/${clinicId}/profile`, { token }),
  clinicPhotos: (token, clinicId) => apiRequest(`/marketplace/clinics/${clinicId}/photos`, { token }),
  uploadClinicPhoto: (token, file) => apiUpload('/marketplace/clinics/me/photos', token, file),
  deleteClinicPhoto: (token, photoId) => apiRequest(`/marketplace/clinics/me/photos/${photoId}`, {
    method: 'DELETE',
    token,
  }),
  setClinicPrimary: (token, photoId) => apiRequest('/marketplace/clinics/me/primary', {
    method: 'PUT',
    body: JSON.stringify({ photoId }),
    token,
  }),
  clinicReviews: (token, clinicId) => apiRequest(`/marketplace/clinics/${clinicId}/reviews`, { token }),
  saveClinicReview: (token, clinicId, body) => apiRequest(`/marketplace/clinics/${clinicId}/reviews`, {
    method: 'POST',
    body: JSON.stringify(body),
    token,
  }),
  deleteClinicReview: (token, reviewId) => apiRequest(`/marketplace/reviews/${reviewId}`, {
    method: 'DELETE',
    token,
  }),
  uploadClinicLogo: (token, file) => apiUpload('/marketplace/clinics/me/logo', token, file),
  uploadClinicBanner: (token, file) => apiUpload('/marketplace/clinics/me/banner', token, file),
  products: (token, filters = {}) => {
    const { clinicId, category, minPrice, maxPrice, verifiedOnly, search, sortBy, inStockOnly } = filters
    const params = new URLSearchParams()
    if (clinicId) params.set('clinicId', clinicId)
    if (category !== '' && category !== null && category !== undefined) params.set('category', category)
    if (minPrice !== null && minPrice !== undefined) params.set('minPrice', minPrice)
    if (maxPrice !== null && maxPrice !== undefined) params.set('maxPrice', maxPrice)
    if (verifiedOnly === true) params.set('verifiedOnly', 'true')
    if (search) params.set('search', search)
    if (sortBy) params.set('sortBy', sortBy)
    if (inStockOnly === true) params.set('inStockOnly', 'true')
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
  clinicOrders: (token) => apiRequest('/marketplace/clinic/orders', { token }),
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
  pet: (token, petId) => apiRequest(`/adoptions/pets/${petId}`, { token }),
  petPhotos: (token, petId) => apiRequest(`/adoptions/pets/${petId}/photos`, { token }),
  uploadPetPhoto: (token, petId, file) => apiUpload(`/adoptions/pets/${petId}/photos`, token, file),
  deletePetPhoto: (token, petId, photoId) => apiRequest(`/adoptions/pets/${petId}/photos/${photoId}`, {
    method: 'DELETE',
    token,
  }),
  setPetPrimary: (token, petId, photoId) => apiRequest(`/adoptions/pets/${petId}/primary`, {
    method: 'PUT',
    body: JSON.stringify({ photoId }),
    token,
  }),
  requests: (token) => apiRequest('/adoptions/requests/mine', { token }),
  shelterRequests: (token) => apiRequest('/adoptions/requests/shelter', { token }),
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
  myShelter: (token) => apiRequest('/adoptions/shelter/me', { token }),
  saveShelter: (token, body) => apiRequest('/adoptions/shelter', {
    method: 'PUT',
    body: JSON.stringify(body),
    token,
  }),
  uploadShelterLogo: (token, file) => apiUpload('/adoptions/shelter/me/logo', token, file),
  uploadShelterBanner: (token, file) => apiUpload('/adoptions/shelter/me/banner', token, file),
}

export const chatApi = {
  conversations: (token) => apiRequest('/chat/conversations', { token }),
  createConversation: (token, participantUserId) => apiRequest('/chat/conversations', {
    method: 'POST',
    body: JSON.stringify({ participantUserId }),
    token,
  }),
  messages: (token, conversationId) => apiRequest(`/chat/conversations/${conversationId}/messages`, { token }),
  sendMessage: (token, conversationId, content) => apiRequest(`/chat/conversations/${conversationId}/messages`, {
    method: 'POST',
    body: JSON.stringify({ content }),
    token,
  }),
  createProduct: (token, body) => apiRequest('/marketplace/products', {
    method: 'POST',
    body: JSON.stringify(body),
    token,
  }),
  updateOrderStatus: (token, orderId, status) => apiRequest(`/marketplace/clinic/orders/${orderId}/status`, {
    method: 'PUT',
    body: JSON.stringify({ status }),
    token,
  }),
  updateRequest: (token, requestId, status) => apiRequest(`/adoptions/requests/${requestId}`, {
    method: 'PUT',
    body: JSON.stringify({ status, shelterComment: null }),
    token,
  }),
  clinic: (token) => apiRequest('/appointments/clinic', { token }),
  updateStatus: (token, appointmentId, status) => apiRequest(`/appointments/${appointmentId}/status`, {
    method: 'PUT',
    body: JSON.stringify({ status, clinicNotes: null }),
    token,
  }),
  createService: (token, body) => apiRequest('/appointments/clinic/services', {
    method: 'POST',
    body: JSON.stringify(body),
    token,
  }),
  createSchedule: (token, body) => apiRequest('/appointments/clinic/schedules', {
    method: 'POST',
    body: JSON.stringify(body),
    token,
  }),
}
