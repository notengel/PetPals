import { useEffect, useState } from 'react'
import { HubConnectionBuilder } from '@microsoft/signalr'
import { CircleMarker, MapContainer, Popup, TileLayer, useMap } from 'react-leaflet'
import { adoptionApi, API_URL, appointmentsApi, authApi, chatApi, clearStoredToken, getStoredToken, getTokenRole, marketplaceApi, socialApi, storeToken } from './api'
import './App.css'
import 'leaflet/dist/leaflet.css'

function App() {
  const [token, setToken] = useState(getStoredToken)
  const [profile, setProfile] = useState(null)
  const [pets, setPets] = useState([])
  const [posts, setPosts] = useState([])
  const [comments, setComments] = useState({})
  const [authMode, setAuthMode] = useState('login')
  const [authForm, setAuthForm] = useState({ email: '', password: '', displayName: '', role: 'User' })
  const [postText, setPostText] = useState('')
  const [selectedPet, setSelectedPet] = useState('')
  const [petForm, setPetForm] = useState({ name: '', species: '' })
  const [commentForms, setCommentForms] = useState({})
  const [loading, setLoading] = useState(Boolean(token))
  const [error, setError] = useState('')
  const [activeView, setActiveView] = useState('feed')

  useEffect(() => {
    if (!token) return

    Promise.all([
      socialApi.profile(token),
      socialApi.pets(token),
      socialApi.feed(token),
    ])
      .then(([nextProfile, nextPets, nextPosts]) => {
        setProfile(nextProfile)
        setPets(nextPets)
        setPosts(nextPosts)
        setError('')
      })
      .catch((requestError) => setError(requestError.message || 'Something went wrong'))
      .finally(() => setLoading(false))
  }, [token])

  async function handleAuth(event) {
    event.preventDefault()
    setLoading(true)
    setError('')

    try {
      const body = authMode === 'register'
        ? authForm
        : { email: authForm.email, password: authForm.password }
      const response = await (authMode === 'register' ? authApi.register(body) : authApi.login(body))
      storeToken(response.accessToken)
      setToken(response.accessToken)
    } catch (requestError) {
      handleError(requestError)
    } finally {
      setLoading(false)
    }
  }

  async function createPost(event) {
    event.preventDefault()
    if (!postText.trim()) return

    try {
      const post = await socialApi.createPost(token, {
        content: postText,
        petId: selectedPet || null,
      })
      setPosts((current) => [post, ...current])
      setPostText('')
      setSelectedPet('')
    } catch (requestError) {
      handleError(requestError)
    }
  }

  async function createPet(event) {
    event.preventDefault()
    if (!petForm.name.trim() || !petForm.species.trim()) return

    try {
      const pet = await socialApi.createPet(token, petForm)
      setPets((current) => [...current, pet])
      setPetForm({ name: '', species: '' })
    } catch (requestError) {
      handleError(requestError)
    }
  }

  async function toggleLike(post) {
    try {
      await socialApi.like(token, post.id, post.isLiked)
      setPosts((current) => current.map((item) => item.id === post.id
        ? { ...item, isLiked: !item.isLiked, likesCount: item.likesCount + (item.isLiked ? -1 : 1) }
        : item))
    } catch (requestError) {
      handleError(requestError)
    }
  }

  async function toggleComments(postId) {
    if (comments[postId]) {
      setComments((current) => ({ ...current, [postId]: null }))
      return
    }

    try {
      const nextComments = await socialApi.comments(token, postId)
      setComments((current) => ({ ...current, [postId]: nextComments }))
    } catch (requestError) {
      handleError(requestError)
    }
  }

  async function addComment(event, postId) {
    event.preventDefault()
    const content = commentForms[postId]?.trim()
    if (!content) return

    try {
      const comment = await socialApi.addComment(token, postId, { content })
      setComments((current) => ({ ...current, [postId]: [...(current[postId] || []), comment] }))
      setCommentForms((current) => ({ ...current, [postId]: '' }))
      setPosts((current) => current.map((post) => post.id === postId
        ? { ...post, commentsCount: post.commentsCount + 1 }
        : post))
    } catch (requestError) {
      handleError(requestError)
    }
  }

  function logout() {
    clearStoredToken()
    setToken(null)
    setProfile(null)
    setPets([])
    setPosts([])
  }

  function handleError(requestError) {
    setError(requestError.message || 'Something went wrong')
  }

  if (!token) {
    return <AuthScreen mode={authMode} setMode={setAuthMode} form={authForm} setForm={setAuthForm} onSubmit={handleAuth} loading={loading} error={error} />
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <div className="brand-mark">P</div>
        <div>
          <p className="eyebrow">PETPALS / SOCIAL</p>
          <h1>Un buen día para compartirlo.</h1>
        </div>
        <div className="topbar-actions">
          <nav className="top-nav" aria-label="Main navigation">
            <button className={activeView === 'feed' ? 'nav-button active' : 'nav-button'} type="button" onClick={() => setActiveView('feed')}>Feed</button>
            <button className={activeView === 'marketplace' ? 'nav-button active' : 'nav-button'} type="button" onClick={() => setActiveView('marketplace')}>Tienda</button>
            <button className={activeView === 'appointments' ? 'nav-button active' : 'nav-button'} type="button" onClick={() => setActiveView('appointments')}>Citas</button>
            <button className={activeView === 'adoptions' ? 'nav-button active' : 'nav-button'} type="button" onClick={() => setActiveView('adoptions')}>Adopciones</button>
            <button className={activeView === 'maps' ? 'nav-button active' : 'nav-button'} type="button" onClick={() => setActiveView('maps')}>Mapa</button>
            <button className={activeView === 'chat' ? 'nav-button active' : 'nav-button'} type="button" onClick={() => setActiveView('chat')}>Chat</button>
          </nav>
          <span className="user-pill">{profile?.displayName || 'Mi perfil'}</span>
          <button className="button button-ghost" type="button" onClick={logout}>Salir</button>
        </div>
      </header>

      {error && <div className="alert">{error}</div>}

      {activeView === 'marketplace'
        ? <MarketplaceView token={token} role={getTokenRole(token)} onError={handleError} />
        : activeView === 'appointments'
          ? getTokenRole(token) === 'Clinic' ? <ClinicAppointmentsView token={token} onError={handleError} /> : <AppointmentsView token={token} pets={pets} onError={handleError} />
        : activeView === 'adoptions'
          ? <AdoptionsView token={token} role={getTokenRole(token)} onError={handleError} />
        : activeView === 'maps'
          ? <MapsView token={token} onError={handleError} />
        : activeView === 'chat'
          ? <ChatView token={token} onError={handleError} />
        : <div className="content-grid">
        <aside className="sidebar">
          <section className="side-card profile-card">
            <div className="avatar avatar-large">{profile?.displayName?.[0] || 'P'}</div>
            <p className="eyebrow">TU ESPACIO</p>
            <h2>{profile?.displayName || 'Tu perfil'}</h2>
            <p>{profile?.bio || 'Agrega una bio para contarle a la comunidad quien eres.'}</p>
          </section>

          <section className="side-card">
            <div className="section-heading">
              <div>
                <p className="eyebrow">MIS COMPAÑEROS</p>
                <h2>Mascotas</h2>
              </div>
              <span className="count-badge">{pets.length}</span>
            </div>
            <div className="pet-list">
              {pets.map((pet) => (
                <div className="pet-row" key={pet.id}>
                  <span className="pet-dot">{pet.name[0]}</span>
                  <div><strong>{pet.name}</strong><small>{pet.species}</small></div>
                </div>
              ))}
              {!pets.length && <p className="muted">Aun no tienes mascotas.</p>}
            </div>
            <form className="compact-form" onSubmit={createPet}>
              <input aria-label="Nombre de mascota" placeholder="Nombre" value={petForm.name} onChange={(event) => setPetForm({ ...petForm, name: event.target.value })} />
              <input aria-label="Especie de mascota" placeholder="Especie" value={petForm.species} onChange={(event) => setPetForm({ ...petForm, species: event.target.value })} />
              <button className="button button-secondary" type="submit">+ Añadir mascota</button>
            </form>
          </section>
        </aside>

        <section className="feed-column">
          <form className="composer card" onSubmit={createPost}>
            <div className="composer-head"><div className="avatar">{profile?.displayName?.[0] || 'P'}</div><div><strong>¿Qué está pasando?</strong><span>Comparte un momento de tu manada.</span></div></div>
            <textarea aria-label="Contenido de la publicación" value={postText} onChange={(event) => setPostText(event.target.value)} placeholder="Escribe algo bonito..." maxLength={2000} />
            <div className="composer-footer">
              <select aria-label="Mascota de la publicación" value={selectedPet} onChange={(event) => setSelectedPet(event.target.value)}>
                <option value="">Sin mascota asociada</option>
                {pets.map((pet) => <option value={pet.id} key={pet.id}>{pet.name}</option>)}
              </select>
              <button className="button button-primary" type="submit">Publicar</button>
            </div>
          </form>

          <div className="feed-heading"><div><p className="eyebrow">LA COMUNIDAD</p><h2>Feed reciente</h2></div><span className="live-dot">EN VIVO</span></div>
          {loading && <div className="card loading-card">Cargando tu feed...</div>}
          {!loading && !posts.length && <div className="card empty-card"><span className="empty-icon">+</span><h2>Tu feed comienza aqui</h2><p>Publica el primer momento de tu mascota.</p></div>}
          <div className="post-list">
            {posts.map((post) => (
              <article className="post-card card" key={post.id}>
                <div className="post-head"><div className="avatar">{post.authorDisplayName?.[0] || 'P'}</div><div className="post-meta"><strong>{post.authorDisplayName}</strong><span>{new Date(post.createdAtUtc).toLocaleString()}</span></div><button className="more-button" type="button" aria-label="Más opciones">...</button></div>
                <p className="post-content">{post.content}</p>
                {post.petName && <div className="pet-tag">Con {post.petName}</div>}
                <div className="post-actions"><button className={post.isLiked ? 'action-button liked' : 'action-button'} type="button" onClick={() => toggleLike(post)}>♡ {post.likesCount} Me gusta</button><button className="action-button" type="button" onClick={() => toggleComments(post.id)}>◌ {post.commentsCount} Comentarios</button></div>
                {comments[post.id] && <div className="comments"><div className="comment-list">{comments[post.id].map((comment) => <div className="comment" key={comment.id}><span className="avatar avatar-small">{comment.userDisplayName?.[0] || 'P'}</span><p><strong>{comment.userDisplayName}</strong>{comment.content}</p></div>)}</div><form className="comment-form" onSubmit={(event) => addComment(event, post.id)}><input aria-label="Nuevo comentario" placeholder="Escribe un comentario..." value={commentForms[post.id] || ''} onChange={(event) => setCommentForms({ ...commentForms, [post.id]: event.target.value })} /><button type="submit">Enviar</button></form></div>}
              </article>
            ))}
          </div>
        </section>
        </div>}
    </main>
  )
}

function ChatView({ token, onError }) {
  const [conversations, setConversations] = useState([])
  const [conversationId, setConversationId] = useState('')
  const [messages, setMessages] = useState([])
  const [content, setContent] = useState('')
  const [participantId, setParticipantId] = useState('')

  useEffect(() => {
    chatApi.conversations(token)
      .then((nextConversations) => {
        setConversations(nextConversations)
        setConversationId(nextConversations[0]?.id || '')
      })
      .catch(onError)
  }, [onError, token])

  useEffect(() => {
    if (!conversationId) return undefined
    let connection
    chatApi.messages(token, conversationId).then(setMessages).catch(onError)
    connection = new HubConnectionBuilder()
      .withUrl(`${API_URL.replace(/\/api$/, '')}/hubs/chat`, { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build()
    connection.on('MessageReceived', (message) => setMessages((current) => current.some((item) => item.id === message.id) ? current : [...current, message]))
    connection.start().then(() => connection.invoke('JoinConversation', conversationId)).catch(onError)
    return () => { connection.stop() }
  }, [conversationId, onError, token])

  async function createConversation(event) {
    event.preventDefault()
    if (!participantId.trim()) return
    try {
      const conversation = await chatApi.createConversation(token, participantId.trim())
      setConversations((current) => current.some((item) => item.id === conversation.id) ? current : [conversation, ...current])
      setConversationId(conversation.id)
      setParticipantId('')
    } catch (requestError) {
      onError(requestError)
    }
  }

  async function sendMessage(event) {
    event.preventDefault()
    if (!content.trim() || !conversationId) return
    try {
      await chatApi.sendMessage(token, conversationId, content)
      setContent('')
    } catch (requestError) {
      onError(requestError)
    }
  }

  return (
    <section className="chat-layout">
      <aside className="chat-sidebar card"><div className="section-heading"><div><p className="eyebrow">PETPALS / CHAT</p><h2>Conversaciones</h2></div><span className="count-badge">{conversations.length}</span></div><form className="new-chat-form" onSubmit={createConversation}><input placeholder="ID del usuario" value={participantId} onChange={(event) => setParticipantId(event.target.value)} /><button className="button button-secondary" type="submit">Nueva conversación</button></form>{conversations.map((conversation) => <button className={conversation.id === conversationId ? 'conversation-button active' : 'conversation-button'} type="button" key={conversation.id} onClick={() => setConversationId(conversation.id)}>{conversation.participants.map((participant) => participant.displayName).join(' · ')}</button>)}{!conversations.length && <p className="muted">Crea una conversación con el ID de otro usuario.</p>}</aside>
      <section className="chat-panel card"><div className="chat-messages">{!conversationId && <p className="muted">Selecciona o crea una conversación.</p>}{messages.map((message) => <div className="message-bubble" key={message.id}><strong>{message.senderDisplayName}</strong><p>{message.content}</p><small>{new Date(message.sentAtUtc).toLocaleString()}</small></div>)}</div><form className="chat-composer" onSubmit={sendMessage}><input disabled={!conversationId} placeholder="Escribe un mensaje..." value={content} onChange={(event) => setContent(event.target.value)} /><button className="button button-primary" disabled={!conversationId} type="submit">Enviar</button></form></section>
    </section>
  )
}

function MapsView({ token, onError }) {
  const [clinics, setClinics] = useState([])
  const [location, setLocation] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    marketplaceApi.clinics(token)
      .then(setClinics)
      .catch(onError)
      .finally(() => setLoading(false))
    navigator.geolocation?.getCurrentPosition(
      (position) => setLocation({ latitude: position.coords.latitude, longitude: position.coords.longitude }),
    )
  }, [onError, token])

  const sortedClinics = [...clinics].sort((left, right) => distanceFrom(location, left) - distanceFrom(location, right))
  const mapClinic = sortedClinics[0]
  const mapCenter = mapClinic ? [mapClinic.latitude, mapClinic.longitude] : location ? [location.latitude, location.longitude] : [0, 0]

  return (
    <section className="maps-layout">
      <div className="maps-main">
        <div className="marketplace-heading"><div><p className="eyebrow">PETPALS / MAPA</p><h2>Veterinarias cerca de ti.</h2><p className="marketplace-intro">Activa tu ubicación para ordenar los resultados por cercanía.</p></div><button className="button button-secondary" type="button" onClick={() => navigator.geolocation?.getCurrentPosition((position) => setLocation({ latitude: position.coords.latitude, longitude: position.coords.longitude }))}>Usar mi ubicación</button></div>
        <div className="map-card card"><MapContainer center={mapCenter} zoom={mapClinic || location ? 13 : 2} scrollWheelZoom><TileLayer attribution='&copy; OpenStreetMap contributors' url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" /><MapCenter center={mapCenter} />{location && <CircleMarker center={[location.latitude, location.longitude]} pathOptions={{ color: '#c56b45' }}><Popup>Tu ubicación aproximada</Popup></CircleMarker>}{clinics.map((clinic) => <CircleMarker center={[clinic.latitude, clinic.longitude]} pathOptions={{ color: '#315c4b' }} key={clinic.id}><Popup><strong>{clinic.name}</strong><br />{clinic.address || 'Dirección no disponible'}</Popup></CircleMarker>)}</MapContainer></div>
      </div>
      <aside className="clinic-list card"><div className="section-heading"><div><p className="eyebrow">UBICACIONES</p><h2>Veterinarias</h2></div><span className="count-badge">{clinics.length}</span></div>{loading && <p className="muted">Cargando ubicaciones...</p>}{!loading && !clinics.length && <p className="muted">Aún no hay veterinarias registradas.</p>}{sortedClinics.map((clinic) => <a className="clinic-row" href={`https://www.openstreetmap.org/?mlat=${clinic.latitude}&mlon=${clinic.longitude}#map=17/${clinic.latitude}/${clinic.longitude}`} target="_blank" rel="noreferrer" key={clinic.id}><div><strong>{clinic.name}</strong><small>{clinic.address || 'Dirección no disponible'}</small></div><span>{formatDistance(distanceFrom(location, clinic))}</span></a>)}</aside>
    </section>
  )
}

function MapCenter({ center }) {
  const map = useMap()
  useEffect(() => map.setView(center), [center, map])
  return null
}

function distanceFrom(location, clinic) {
  if (!location) return Number.MAX_SAFE_INTEGER
  const earthRadius = 6371
  const latitudeDelta = (clinic.latitude - location.latitude) * Math.PI / 180
  const longitudeDelta = (clinic.longitude - location.longitude) * Math.PI / 180
  const latitude = location.latitude * Math.PI / 180
  const value = Math.sin(latitudeDelta / 2) ** 2 + Math.cos(latitude) * Math.cos(clinic.latitude * Math.PI / 180) * Math.sin(longitudeDelta / 2) ** 2
  return earthRadius * 2 * Math.atan2(Math.sqrt(value), Math.sqrt(1 - value))
}

function formatDistance(distance) {
  return Number.isFinite(distance) ? `${distance.toFixed(1)} km` : 'Distancia n/d'
}

function AdoptionsView({ token, role, onError }) {
  const [pets, setPets] = useState([])
  const [requests, setRequests] = useState([])
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(true)
  const [petForm, setPetForm] = useState({ name: '', species: '', breed: '', approximateAge: '', description: '' })

  useEffect(() => {
    Promise.all([adoptionApi.pets(token), role === 'Shelter' ? adoptionApi.shelterRequests(token) : adoptionApi.requests(token)])
      .then(([nextPets, nextRequests]) => {
        setPets(nextPets)
        setRequests(nextRequests)
      })
      .catch(onError)
      .finally(() => setLoading(false))
  }, [onError, role, token])

  async function requestAdoption(petId) {
    try {
      const request = await adoptionApi.request(token, petId, message)
      setRequests((current) => [request, ...current])
      setMessage('')
    } catch (requestError) {
      onError(requestError)
    }
  }

  async function publishPet(event) {
    event.preventDefault()
    try {
      const pet = await adoptionApi.createPet(token, petForm)
      setPets((current) => [pet, ...current])
      setPetForm({ name: '', species: '', breed: '', approximateAge: '', description: '' })
    } catch (requestError) {
      onError(requestError)
    }
  }

  async function reviewRequest(requestId, status) {
    try {
      const request = await adoptionApi.updateRequest(token, requestId, status)
      setRequests((current) => current.map((item) => item.id === request.id ? request : item))
    } catch (requestError) {
      onError(requestError)
    }
  }

  return (
    <section className="adoptions-layout">
      <div className="adoptions-main">
        <div className="marketplace-heading"><div><p className="eyebrow">PETPALS / ADOPCIONES</p><h2>Encuentra un nuevo compañero.</h2><p className="marketplace-intro">Conoce animales publicados por refugios de la comunidad.</p></div></div>
        {loading && <div className="card loading-card">Cargando animales...</div>}
        {!loading && !pets.length && <div className="card empty-card"><h2>No hay animales disponibles</h2><p>Vuelve más tarde para conocer nuevas historias.</p></div>}
        <div className="adoption-grid">{pets.map((pet) => <article className="adoption-card card" key={pet.id}><div className="adoption-info"><p className="eyebrow">{pet.shelterName}</p><h3>{pet.name}</h3><p>{pet.species}{pet.breed ? ` · ${pet.breed}` : ''}{pet.approximateAge ? ` · ${pet.approximateAge}` : ''}</p><p>{pet.description || 'Este animal busca un hogar responsable.'}</p>{pet.vaccinations?.length > 0 && <small>{pet.vaccinations.length} vacuna(s) registradas</small>}<button className="button button-primary" type="button" onClick={() => requestAdoption(pet.id)}>Solicitar adopción</button></div></article>)}</div>
      </div>
      <aside className="adoption-side card"><p className="eyebrow">{role === 'Shelter' ? 'SOLICITUDES DEL REFUGIO' : 'MI SOLICITUD'}</p>{role !== 'Shelter' && <textarea placeholder="Mensaje para el refugio..." value={message} onChange={(event) => setMessage(event.target.value)} maxLength={1000} />}{requests.map((request) => <div className="request-row" key={request.id}><strong>{request.petName}</strong><span>{request.status}</span><small>{request.applicantMessage || 'Sin mensaje'}</small>{role === 'Shelter' && request.status < 2 && <div><button className="button button-secondary" type="button" onClick={() => reviewRequest(request.id, 2)}>Aprobar</button> <button className="button button-ghost" type="button" onClick={() => reviewRequest(request.id, 3)}>Rechazar</button></div>}</div>)}{role === 'Shelter' && <form className="publish-form" onSubmit={publishPet}><p className="eyebrow">PUBLICAR ANIMAL</p><input required placeholder="Nombre" value={petForm.name} onChange={(event) => setPetForm({ ...petForm, name: event.target.value })} /><input required placeholder="Especie" value={petForm.species} onChange={(event) => setPetForm({ ...petForm, species: event.target.value })} /><input placeholder="Raza" value={petForm.breed} onChange={(event) => setPetForm({ ...petForm, breed: event.target.value })} /><input placeholder="Edad aproximada" value={petForm.approximateAge} onChange={(event) => setPetForm({ ...petForm, approximateAge: event.target.value })} /><textarea placeholder="Descripción" value={petForm.description} onChange={(event) => setPetForm({ ...petForm, description: event.target.value })} /><button className="button button-secondary" type="submit">Publicar</button></form>}</aside>
    </section>
  )
}

function AppointmentsView({ token, pets, onError }) {
  const [clinics, setClinics] = useState([])
  const [services, setServices] = useState([])
  const [schedules, setSchedules] = useState([])
  const [appointments, setAppointments] = useState([])
  const [clinicId, setClinicId] = useState('')
  const [serviceId, setServiceId] = useState('')
  const [petIds, setPetIds] = useState([])
  const [startsAt, setStartsAt] = useState('')
  const [userNotes, setUserNotes] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    Promise.all([marketplaceApi.clinics(token), appointmentsApi.mine(token)])
      .then(([nextClinics, nextAppointments]) => {
        setClinics(nextClinics)
        setAppointments(nextAppointments)
        if (nextClinics[0]) setClinicId(nextClinics[0].id)
      })
      .catch(onError)
      .finally(() => setLoading(false))
  }, [onError, token])

  useEffect(() => {
    if (!clinicId) return
    Promise.all([appointmentsApi.services(token, clinicId), appointmentsApi.schedules(token, clinicId)])
      .then(([nextServices, nextSchedules]) => {
        setServices(nextServices)
        setSchedules(nextSchedules)
        setServiceId(nextServices[0]?.id || '')
      })
      .catch(onError)
  }, [clinicId, onError, token])

  async function bookAppointment(event) {
    event.preventDefault()
    if (!clinicId || !serviceId || !petIds.length || !startsAt) return

    try {
      const appointment = await appointmentsApi.create(token, {
        clinicId,
        clinicServiceId: serviceId,
        petIds,
        startsAtUtc: new Date(startsAt).toISOString(),
        userNotes,
      })
      setAppointments((current) => [...current, appointment].sort((a, b) => new Date(a.startsAtUtc) - new Date(b.startsAtUtc)))
      setStartsAt('')
      setUserNotes('')
      setPetIds([])
    } catch (requestError) {
      onError(requestError)
    }
  }

  async function cancelAppointment(appointmentId) {
    try {
      const cancelled = await appointmentsApi.cancel(token, appointmentId)
      setAppointments((current) => current.map((item) => item.id === cancelled.id ? cancelled : item))
    } catch (requestError) {
      onError(requestError)
    }
  }

  return (
    <section className="appointments-layout">
      <div className="appointments-main">
        <div className="marketplace-heading"><div><p className="eyebrow">PETPALS / CITAS</p><h2>Cuida su salud.</h2><p className="marketplace-intro">Agenda una visita según el horario de cada veterinaria.</p></div></div>
        <form className="card appointment-form" onSubmit={bookAppointment}>
          <label>Veterinaria<select value={clinicId} onChange={(event) => setClinicId(event.target.value)}><option value="">Selecciona una veterinaria</option>{clinics.map((clinic) => <option key={clinic.id} value={clinic.id}>{clinic.name}</option>)}</select></label>
          <label>Servicio<select value={serviceId} onChange={(event) => setServiceId(event.target.value)}><option value="">Selecciona un servicio</option>{services.map((service) => <option key={service.id} value={service.id}>{service.name} · {service.durationMinutes} min</option>)}</select></label>
          <label>Fecha y hora<input required type="datetime-local" value={startsAt} onChange={(event) => setStartsAt(event.target.value)} /></label>
          <fieldset><legend>Mascotas</legend>{pets.map((pet) => <label className="pet-check" key={pet.id}><input type="checkbox" checked={petIds.includes(pet.id)} onChange={(event) => setPetIds((current) => event.target.checked ? [...current, pet.id] : current.filter((id) => id !== pet.id))} />{pet.name} <small>{pet.species}</small></label>)}{!pets.length && <p className="muted">Agrega una mascota antes de reservar.</p>}</fieldset>
          <label>Notas opcionales<textarea value={userNotes} onChange={(event) => setUserNotes(event.target.value)} maxLength={1000} /></label>
          {!!schedules.length && <small className="muted">La veterinaria atiende en horarios semanales configurados. La hora se valida al reservar.</small>}
          <button className="button button-primary" disabled={loading || !pets.length} type="submit">Solicitar cita</button>
        </form>
      </div>
      <aside className="appointment-list card"><div className="section-heading"><div><p className="eyebrow">MIS CITAS</p><h2>Agenda</h2></div><span className="count-badge">{appointments.length}</span></div>{!appointments.length && <p className="muted">Aún no tienes citas.</p>}{appointments.map((appointment) => <div className="appointment-row" key={appointment.id}><strong>{appointment.serviceName}</strong><span>{new Date(appointment.startsAtUtc).toLocaleString()}</span><small>{appointment.pets.map((pet) => pet.name).join(', ')}</small><em>{appointment.status}</em>{appointment.status !== 2 && <button className="button button-ghost" type="button" onClick={() => cancelAppointment(appointment.id)}>Cancelar</button>}</div>)}</aside>
    </section>
  )
}

function ClinicAppointmentsView({ token, onError }) {
  const [appointments, setAppointments] = useState([])
  const [serviceForm, setServiceForm] = useState({ name: '', description: '', price: 0, durationMinutes: 30 })
  const [scheduleForm, setScheduleForm] = useState({ dayOfWeek: 1, opensAt: '09:00', closesAt: '17:00' })

  useEffect(() => {
    appointmentsApi.clinic(token).then(setAppointments).catch(onError)
  }, [onError, token])

  async function updateStatus(appointmentId, status) {
    try {
      const appointment = await appointmentsApi.updateStatus(token, appointmentId, Number(status))
      setAppointments((current) => current.map((item) => item.id === appointment.id ? appointment : item))
    } catch (requestError) {
      onError(requestError)
    }
  }

  async function createService(event) {
    event.preventDefault()
    try {
      await appointmentsApi.createService(token, { ...serviceForm, price: Number(serviceForm.price), durationMinutes: Number(serviceForm.durationMinutes) })
      setServiceForm({ name: '', description: '', price: 0, durationMinutes: 30 })
    } catch (requestError) {
      onError(requestError)
    }
  }

  async function createSchedule(event) {
    event.preventDefault()
    try {
      await appointmentsApi.createSchedule(token, scheduleForm)
    } catch (requestError) {
      onError(requestError)
    }
  }

  return <section className="clinic-dashboard"><div className="marketplace-heading"><div><p className="eyebrow">PETPALS / AGENDA</p><h2>Gestiona tus citas.</h2><p className="marketplace-intro">Confirma, completa o cancela las reservas de tus clientes.</p></div></div><div className="clinic-setup-grid"><form className="card clinic-profile-form" onSubmit={createService}><strong>Nuevo servicio</strong><input required placeholder="Nombre" value={serviceForm.name} onChange={(event) => setServiceForm({ ...serviceForm, name: event.target.value })} /><input placeholder="Descripción" value={serviceForm.description} onChange={(event) => setServiceForm({ ...serviceForm, description: event.target.value })} /><input type="number" min="0" step="0.01" placeholder="Precio" value={serviceForm.price} onChange={(event) => setServiceForm({ ...serviceForm, price: event.target.value })} /><input type="number" min="1" placeholder="Duración en minutos" value={serviceForm.durationMinutes} onChange={(event) => setServiceForm({ ...serviceForm, durationMinutes: event.target.value })} /><button className="button button-secondary" type="submit">Guardar servicio</button></form><form className="card clinic-profile-form" onSubmit={createSchedule}><strong>Horario semanal</strong><select value={scheduleForm.dayOfWeek} onChange={(event) => setScheduleForm({ ...scheduleForm, dayOfWeek: Number(event.target.value) })}><option value="1">Lunes</option><option value="2">Martes</option><option value="3">Miércoles</option><option value="4">Jueves</option><option value="5">Viernes</option><option value="6">Sábado</option><option value="0">Domingo</option></select><input type="time" value={scheduleForm.opensAt} onChange={(event) => setScheduleForm({ ...scheduleForm, opensAt: event.target.value })} /><input type="time" value={scheduleForm.closesAt} onChange={(event) => setScheduleForm({ ...scheduleForm, closesAt: event.target.value })} /><button className="button button-secondary" type="submit">Guardar horario</button></form></div><div className="card dashboard-list">{!appointments.length && <p className="muted">No tienes citas registradas.</p>}{appointments.map((appointment) => <div className="dashboard-row" key={appointment.id}><div><strong>{appointment.serviceName}</strong><span>{new Date(appointment.startsAtUtc).toLocaleString()}</span><small>{appointment.pets.map((pet) => pet.name).join(', ')}</small></div><select value={appointment.status} onChange={(event) => updateStatus(appointment.id, event.target.value)}><option value="0">Pendiente</option><option value="1">Confirmada</option><option value="2">Cancelada</option><option value="3">Completada</option></select></div>)}</div></section>
}

function MarketplaceView({ token, role, onError }) {
  const [products, setProducts] = useState([])
  const [cart, setCart] = useState(null)
  const [orders, setOrders] = useState([])
  const [clinic, setClinic] = useState(null)
  const [clinicForm, setClinicForm] = useState({ name: '', description: '', phone: '', address: '', latitude: 0, longitude: 0 })
  const [category, setCategory] = useState('')
  const [loading, setLoading] = useState(true)
  const [productForm, setProductForm] = useState({ name: '', description: '', category: 0, price: 0, stock: 0 })

  useEffect(() => {
    const requests = [marketplaceApi.products(token, null, category)]
    if (role !== 'Clinic') requests.push(marketplaceApi.cart(token))
    if (role === 'Clinic') requests.push(marketplaceApi.clinicOrders(token))
    if (role === 'Clinic') requests.push(marketplaceApi.myClinic(token).catch(() => null))

    Promise.all(requests)
      .then(([nextProducts, nextSecondary, nextClinic]) => {
        setProducts(nextProducts)
        if (role === 'Clinic') {
          setOrders(nextSecondary || [])
          setClinic(nextClinic)
          if (nextClinic) setClinicForm(nextClinic)
        }
        else setCart(nextSecondary || null)
      })
      .catch(onError)
      .finally(() => setLoading(false))
  }, [category, onError, role, token])

  async function addProduct(productId) {
    try {
      setCart(await marketplaceApi.addToCart(token, productId))
    } catch (requestError) {
      onError(requestError)
    }
  }

  async function checkout() {
    try {
      await marketplaceApi.checkout(token)
      setCart(await marketplaceApi.cart(token))
      setProducts(await marketplaceApi.products(token, null, category))
    } catch (requestError) {
      onError(requestError)
    }
  }

  async function createProduct(event) {
    event.preventDefault()
    try {
      const product = await marketplaceApi.createProduct(token, { ...productForm, price: Number(productForm.price), stock: Number(productForm.stock) })
      setProducts((current) => [product, ...current])
      setProductForm({ name: '', description: '', category: 0, price: 0, stock: 0 })
    } catch (requestError) {
      onError(requestError)
    }
  }

  async function updateOrder(orderId, status) {
    try {
      const order = await marketplaceApi.updateOrderStatus(token, orderId, Number(status))
      setOrders((current) => current.map((item) => item.id === order.id ? order : item))
    } catch (requestError) {
      onError(requestError)
    }
  }

  async function saveClinic(event) {
    event.preventDefault()
    try {
      let coordinates = { latitude: Number(clinicForm.latitude), longitude: Number(clinicForm.longitude) }
      if (clinicForm.address.trim()) {
        const response = await fetch(`https://nominatim.openstreetmap.org/search?format=jsonv2&limit=1&q=${encodeURIComponent(clinicForm.address)}`, {
          headers: { Accept: 'application/json' },
        })
        const [result] = await response.json()
        if (result) coordinates = { latitude: Number(result.lat), longitude: Number(result.lon) }
      }
      const savedClinic = await marketplaceApi.saveClinic(token, {
        ...clinicForm,
        ...coordinates,
      })
      setClinic(savedClinic)
    } catch (requestError) {
      onError(requestError)
    }
  }

  return (
    <section className="marketplace-layout">
      <div className="marketplace-main">
        <div className="marketplace-heading"><div><p className="eyebrow">PETPALS / MARKETPLACE</p><h2>Cuida su mundo.</h2><p className="marketplace-intro">Productos seleccionados por veterinarias de la comunidad.</p></div><select aria-label="Filtrar por categoría" value={category} onChange={(event) => setCategory(event.target.value)}><option value="">Todas las categorías</option><option value="0">Alimento</option><option value="1">Vacunas</option><option value="2">Medicinas</option><option value="3">Accesorios</option><option value="4">Higiene</option><option value="5">Otros</option></select></div>
        {role === 'Clinic' && <form className="clinic-profile-form card" onSubmit={saveClinic}><strong>{clinic ? 'Perfil de veterinaria' : 'Configura tu veterinaria'}</strong><input required placeholder="Nombre" value={clinicForm.name} onChange={(event) => setClinicForm({ ...clinicForm, name: event.target.value })} /><input placeholder="Dirección" value={clinicForm.address} onChange={(event) => setClinicForm({ ...clinicForm, address: event.target.value })} /><input placeholder="Teléfono" value={clinicForm.phone} onChange={(event) => setClinicForm({ ...clinicForm, phone: event.target.value })} /><div className="form-row"><input type="number" step="any" placeholder="Latitud" value={clinicForm.latitude} onChange={(event) => setClinicForm({ ...clinicForm, latitude: event.target.value })} /><input type="number" step="any" placeholder="Longitud" value={clinicForm.longitude} onChange={(event) => setClinicForm({ ...clinicForm, longitude: event.target.value })} /></div><button className="button button-secondary" type="submit">Guardar perfil</button></form>}
        {loading && <div className="card loading-card">Cargando productos...</div>}
        {!loading && !products.length && <div className="card empty-card"><span className="empty-icon">+</span><h2>Aún no hay productos</h2><p>Prueba otra categoría o vuelve más tarde.</p></div>}
        <div className="product-grid">{products.map((product) => <article className="product-card card" key={product.id}><div className="product-art"><span>{product.category === 0 ? 'FOOD' : 'CARE'}</span></div><div className="product-info"><p className="eyebrow">{product.clinicName}</p><h3>{product.name}</h3><p>{product.description || 'Producto para el cuidado de tu mascota.'}</p><div className="product-bottom"><strong>${product.price.toFixed(2)}</strong><button className="button button-primary" disabled={!product.stock} type="button" onClick={() => addProduct(product.id)}>{product.stock ? 'Añadir' : 'Agotado'}</button></div><small>{product.stock} disponibles</small></div></article>)}</div>
      </div>
      {role !== 'Clinic' ? <aside className="cart-panel card"><div className="section-heading"><div><p className="eyebrow">TU CARRITO</p><h2>Resumen</h2></div><span className="count-badge">{cart?.items?.length || 0}</span></div>{!cart?.items?.length ? <p className="muted">Tu carrito está vacío.</p> : <><div className="cart-items">{cart.items.map((item) => <div className="cart-item" key={item.id}><div><strong>{item.productName}</strong><small>{item.clinicName} · {item.quantity} x ${item.unitPrice.toFixed(2)}</small></div><b>${item.subtotal.toFixed(2)}</b></div>)}</div><div className="cart-total"><span>Total</span><strong>${cart.total.toFixed(2)}</strong></div><button className="button button-primary button-wide" type="button" onClick={checkout}>Confirmar pedido</button><small className="cart-note">Sin pago real en esta versión.</small></>}</aside> : <aside className="cart-panel card"><p className="eyebrow">GESTIÓN DE VETERINARIA</p><form className="publish-form" onSubmit={createProduct}><strong>Nuevo producto</strong><input required placeholder="Nombre" value={productForm.name} onChange={(event) => setProductForm({ ...productForm, name: event.target.value })} /><input placeholder="Descripción" value={productForm.description} onChange={(event) => setProductForm({ ...productForm, description: event.target.value })} /><input type="number" min="0" step="0.01" placeholder="Precio" value={productForm.price} onChange={(event) => setProductForm({ ...productForm, price: event.target.value })} /><input type="number" min="0" placeholder="Stock" value={productForm.stock} onChange={(event) => setProductForm({ ...productForm, stock: event.target.value })} /><button className="button button-secondary" type="submit">Publicar producto</button></form><div className="clinic-orders"><strong>Pedidos</strong>{orders.map((order) => <div className="request-row" key={order.id}><span>{order.id.slice(0, 8)} · ${order.total.toFixed(2)}</span><select value={order.status} onChange={(event) => updateOrder(order.id, event.target.value)}><option value="0">Pendiente</option><option value="1">Confirmado</option><option value="2">Cancelado</option><option value="3">Completado</option></select></div>)}</div></aside>}
    </section>
  )
}

function AuthScreen({ mode, setMode, form, setForm, onSubmit, loading, error }) {
  return (
    <main className="auth-shell">
      <section className="auth-visual"><p className="eyebrow">PETPALS / SOCIAL FEED</p><div className="auth-slogan"><span>Pequeños momentos.</span><strong>Grandes historias.</strong></div><p>Un lugar para compartir la vida que construyes con tus mascotas.</p><div className="orbit orbit-one" /><div className="orbit orbit-two" /><div className="paw-note">La comunidad empieza con una historia.</div></section>
      <section className="auth-panel"><div className="auth-form-wrap"><div className="brand-lockup"><div className="brand-mark">P</div><span>PetPals</span></div><p className="eyebrow">{mode === 'login' ? 'BIENVENIDO DE VUELTA' : 'ÚNETE A LA MANADA'}</p><h1>{mode === 'login' ? 'Vuelve a tu comunidad.' : 'Crea tu espacio.'}</h1><p className="auth-intro">{mode === 'login' ? 'Continúa compartiendo esos momentos que importan.' : 'Comparte la vida de tus mascotas con personas que la entienden.'}</p>{error && <div className="alert">{error}</div>}<form className="auth-form" onSubmit={onSubmit}>{mode === 'register' && <><label>Nombre visible<input required value={form.displayName} onChange={(event) => setForm({ ...form, displayName: event.target.value })} /></label><label>Tipo de cuenta<select value={form.role} onChange={(event) => setForm({ ...form, role: event.target.value })}><option value="User">Dueño de mascota</option><option value="Clinic">Veterinaria</option><option value="Shelter">Refugio</option></select></label></>}<label>Email<input required type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></label><label>Contraseña<input required type="password" minLength="8" value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} /></label><button className="button button-primary button-wide" disabled={loading} type="submit">{loading ? 'Procesando...' : mode === 'login' ? 'Entrar a PetPals' : 'Crear cuenta'}</button></form><button className="switch-auth" type="button" onClick={() => setMode(mode === 'login' ? 'register' : 'login')}>{mode === 'login' ? '¿Aún no tienes cuenta? Regístrate' : 'Ya tengo una cuenta'}</button></div></section>
    </main>
  )
}

export default App
