import { useEffect, useState } from 'react'
import { HubConnectionBuilder } from '@microsoft/signalr'
import { CircleMarker, MapContainer, Popup, TileLayer, useMap } from 'react-leaflet'
import { adoptionApi, API_URL, appointmentsApi, authApi, chatApi, clearStoredToken, getStoredToken, getTokenRole, marketplaceApi, socialApi, storeToken } from './api'
import './App.css'
import 'leaflet/dist/leaflet.css'

const navigationItems = [
  { id: 'feed', label: 'Feed', eyebrow: 'PETPALS / SOCIAL', title: 'Un buen día para compartirlo.', subtitle: 'Momentos reales de personas que viven la misma manada.', icon: 'feed' },
  { id: 'marketplace', label: 'Tienda', eyebrow: 'PETPALS / MARKETPLACE', title: 'Cuida su mundo.', subtitle: 'Productos seleccionados para acompañar cada etapa.', icon: 'store' },
  { id: 'appointments', label: 'Citas', eyebrow: 'PETPALS / CITAS', title: 'Salud sin complicaciones.', subtitle: 'Organiza sus próximas visitas en un solo lugar.', icon: 'calendar' },
  { id: 'adoptions', label: 'Adopciones', eyebrow: 'PETPALS / ADOPCIONES', title: 'Una nueva historia empieza aquí.', subtitle: 'Conecta con animales que buscan un hogar.', icon: 'heart' },
  { id: 'maps', label: 'Mapa', eyebrow: 'PETPALS / MAPA', title: 'Todo cerca de tu manada.', subtitle: 'Encuentra veterinarias y servicios próximos a ti.', icon: 'map' },
  { id: 'chat', label: 'Mensajes', eyebrow: 'PETPALS / CHAT', title: 'Habla con tu comunidad.', subtitle: 'Conversaciones privadas, sencillas y seguras.', icon: 'chat' },
]

const profileNavigationItem = { id: 'profile', label: 'Perfil', eyebrow: 'PETPALS / PERFIL', title: 'Tu espacio, a tu manera.', subtitle: 'Una vista rápida de tu vida en PetPals.', icon: 'user' }

function SidebarGlyph({ name }) {
  const icons = {
    feed: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" width="20" height="20"><path d="M4 6h16M4 12h16M4 18h16" /></svg>,
    store: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" width="20" height="20"><path d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" /></svg>,
    calendar: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" width="20" height="20"><rect x="3" y="4" width="18" height="18" rx="2" ry="2" /><line x1="16" y1="2" x2="16" y2="6" /><line x1="8" y1="2" x2="8" y2="6" /><line x1="3" y1="10" x2="21" y2="10" /></svg>,
    heart: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" width="20" height="20"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z" /></svg>,
    map: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" width="20" height="20"><polygon points="3 6 9 3 15 6 21 3 21 18 15 21 9 18 3 21" /><line x1="9" y1="3" x2="9" y2="18" /><line x1="15" y1="6" x2="15" y2="21" /></svg>,
    chat: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" width="20" height="20"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" /></svg>,
    user: <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" width="20" height="20"><path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" /><circle cx="12" cy="7" r="4" /></svg>
  }
  return icons[name] || icons.feed
}

function PetPicker({ pets, value, onChange }) {
  return (
    <div className="chip-picker" role="radiogroup" aria-label="Mascota de la publicación">
      <label className="chip-option">
        <input type="radio" name="post-pet" value="" checked={value === ''} onChange={(event) => onChange(event.target.value)} />
        <span>Sin mascota</span>
      </label>
      {pets.map((pet) => (
        <label className="chip-option" key={pet.id}>
          <input type="radio" name="post-pet" value={pet.id} checked={value === pet.id} onChange={(event) => onChange(event.target.value)} />
          <span>{pet.name}</span>
        </label>
      ))}
    </div>
  )
}

const apiOrigin = API_URL.replace(/\/api$/, '')
export function profileImg(url) {
  if (!url) return null
  return url.startsWith('/') ? `${apiOrigin}${url}` : url
}

function PetGallery({ pet, token, onError, onPrimary }) {
  const [photos, setPhotos] = useState([])
  const [open, setOpen] = useState(false)

  useEffect(() => {
    if (!open) return
    socialApi.petPhotos(token, pet.id).then(setPhotos).catch(onError)
  }, [open, pet.id]) // ponytail: lazy, solo carga al abrir

  async function upload(file) {
    if (!file) return
    try {
      const photo = await socialApi.uploadPetPhoto(token, pet.id, file)
      setPhotos((current) => [photo, ...current])
      if (!pet.primaryImageUrl) onPrimary?.(pet.id, photo.imageUrl)
    } catch (requestError) { onError(requestError) }
  }

  async function remove(photoId) {
    try {
      await socialApi.deletePetPhoto(token, pet.id, photoId)
      setPhotos((current) => current.filter((p) => p.id !== photoId))
    } catch (requestError) { onError(requestError) }
  }

  async function makePrimary(photo) {
    try {
      const updated = await socialApi.setPetPrimary(token, pet.id, photo.id)
      onPrimary?.(pet.id, updated.primaryImageUrl)
    } catch (requestError) { onError(requestError) }
  }

  return (
    <div className="pet-card-row" style={{ flexDirection: 'column', alignItems: 'stretch' }}>
      <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
        {pet.primaryImageUrl
          ? <img src={profileImg(pet.primaryImageUrl)} alt={pet.name} style={{ width: 32, height: 32, borderRadius: '50%', objectFit: 'cover' }} />
          : <span className="pet-dot">{pet.name[0]}</span>}
        <div className="pet-info"><strong>{pet.name}</strong><small>{pet.species}</small></div>
        <button className="button button-ghost" type="button" onClick={() => setOpen(!open)}>{open ? 'Cerrar' : 'Fotos'}</button>
      </div>
      {open && (
        <div style={{ marginTop: 8 }}>
          <label><small className="muted">Añadir foto </small><input type="file" accept="image/*" onChange={(event) => upload(event.target.files[0])} /></label>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(90px,1fr))', gap: 8, marginTop: 8 }}>
            {photos.map((photo) => (
              <div key={photo.id} style={{ position: 'relative' }}>
                <img src={profileImg(photo.imageUrl)} alt="" style={{ width: '100%', height: 90, objectFit: 'cover', borderRadius: 8 }} />
                <div style={{ display: 'flex', gap: 4, marginTop: 4 }}>
                  <button className="button button-ghost" type="button" title="Usar como perfil" onClick={() => makePrimary(photo)}>★</button>
                  <button className="button button-ghost" type="button" title="Eliminar" onClick={() => remove(photo.id)}>✕</button>
                </div>
              </div>
            ))}
          </div>
          {!photos.length && <p className="muted">Sin fotos todavía.</p>}
        </div>
      )}
    </div>
  )
}

function ProfileView({ profile, setProfile, token, onError, pets, setPets }) {
  const [form, setForm] = useState({ displayName: '', bio: '', isPublic: true })
  const [myPosts, setMyPosts] = useState([])
  const [email, setEmail] = useState('')
  const [passwords, setPasswords] = useState({ currentPassword: '', newPassword: '' })
  const [notice, setNotice] = useState('')

  useEffect(() => {
    if (!profile) return
    setForm({ displayName: profile.displayName || '', bio: profile.bio || '', isPublic: profile.isPublic !== false })
    socialApi.myPosts(token).then(setMyPosts).catch(onError)
  }, [profile?.userId]) // ponytail: load once per account, not per keystroke

  if (!profile) return null
  const role = profile?.role || 'User'
  const avatar = profileImg(profile.avatarUrl)
  const banner = profileImg(profile.bannerUrl)

  async function saveProfile(event) {
    event.preventDefault()
    try {
      const updated = await socialApi.updateProfile(token, { ...form, avatarUrl: profile.avatarUrl, bannerUrl: profile.bannerUrl })
      setProfile({ ...profile, ...updated })
      setNotice('Perfil actualizado.')
    } catch (requestError) { onError(requestError) }
  }

  async function upload(kind, file) {
    if (!file) return
    try {
      const updated = kind === 'avatar'
        ? await socialApi.uploadAvatar(token, file)
        : await socialApi.uploadBanner(token, file)
      setProfile({ ...profile, ...updated })
      setNotice(kind === 'avatar' ? 'Foto de perfil actualizada.' : 'Banner actualizado.')
    } catch (requestError) { onError(requestError) }
  }

  async function saveEmail(event) {
    event.preventDefault()
    try {
      await authApi.changeEmail(token, email)
      setEmail('')
      setNotice('Correo actualizado. Usa el nuevo en tu próximo login.')
    } catch (requestError) { onError(requestError) }
  }

  async function savePassword(event) {
    event.preventDefault()
    try {
      await authApi.changePassword(token, passwords)
      setPasswords({ currentPassword: '', newPassword: '' })
      setNotice('Contraseña actualizada.')
    } catch (requestError) { onError(requestError) }
  }

  return (
    <div className="profile-view fade-in-up">
      {banner && <img className="profile-banner" src={banner} alt="Portada del perfil" style={{ width: '100%', height: 180, objectFit: 'cover', borderRadius: 16 }} />}
      <div className="profile-header scale-in">
        {avatar
          ? <img className="avatar profile-avatar" src={avatar} alt="Foto de perfil" style={{ width: 72, height: 72, borderRadius: '50%', objectFit: 'cover' }} />
          : <div className="avatar profile-avatar">{profile?.displayName?.[0] || 'P'}</div>}
        <div className="profile-meta">
          <h1>{profile?.displayName || 'Tu perfil'}</h1>
          <span className="role-badge">{role === 'Clinic' ? 'Veterinaria' : role === 'Shelter' ? 'Refugio' : 'Dueño de mascota'}</span>
          {profile?.bio && <p className="bio">{profile.bio}</p>}
          <small className="muted">{profile.isPublic === false ? 'Perfil privado' : 'Perfil público'}</small>
        </div>
      </div>
      {notice && <div className="alert" style={{ marginTop: 12 }}>{notice}</div>}
      <div className="profile-grid">
        <div className="profile-main">
          <section className="section-card stagger-2">
            <div className="section-card-head"><h2>Mis publicaciones</h2><span className="count-badge">{myPosts.length}</span></div>
            <div className="section-card-body">
              {!myPosts.length && <p className="muted" style={{ textAlign: 'center', padding: 20 }}>Aún no publicas nada.</p>}
              <div className="post-list" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(180px,1fr))', gap: 12 }}>
                {myPosts.map((post) => (
                  <article className="post-card" key={post.id} style={{ padding: 12 }}>
                    {post.mediaUrl && <img src={profileImg(post.mediaUrl)} alt="" style={{ width: '100%', borderRadius: 8 }} />}
                    <p className="post-content" style={{ fontSize: 13 }}>{post.content}</p>
                    <small className="muted">♡ {post.likesCount} · ◌ {post.commentsCount}</small>
                  </article>
                ))}
              </div>
            </div>
          </section>
          <section className="section-card stagger-3">
            <div className="section-card-head"><h2>Mis mascotas</h2><span className="count-badge">{pets.length}</span></div>
            <div className="section-card-body">
              {pets.map((pet) => (
                <PetGallery key={pet.id} pet={pet} token={token} onError={onError}
                  onPrimary={(petId, url) => {
                    setPets((current) => current.map((p) => p.id === petId ? { ...p, primaryImageUrl: url } : p))
                  }} />
              ))}
              {!pets.length && <p className="muted" style={{ textAlign: 'center', padding: 20 }}>Aún no tienes mascotas.</p>}
            </div>
          </section>
        </div>
        <aside className="profile-sidebar">
          <div className="profile-sidebar-card stagger-4">
            <h3>Editar perfil</h3>
            <form className="compact-form" onSubmit={saveProfile} style={{ marginTop: 0 }}>
              <input aria-label="Nombre" placeholder="Nombre" value={form.displayName} onChange={(event) => setForm({ ...form, displayName: event.target.value })} required maxLength={100} />
              <textarea aria-label="Bio" placeholder="Bio" value={form.bio} onChange={(event) => setForm({ ...form, bio: event.target.value })} maxLength={500} />
              <label className="pet-check"><input type="checkbox" checked={form.isPublic} onChange={(event) => setForm({ ...form, isPublic: event.target.checked })} /> Perfil público</label>
              <button className="button button-secondary button-wide" type="submit">Guardar</button>
            </form>
          </div>
          <div className="profile-sidebar-card stagger-4">
            <h3>Fotos</h3>
            <label>Foto de perfil<input type="file" accept="image/*" onChange={(event) => upload('avatar', event.target.files[0])} /></label>
            <label>Banner<input type="file" accept="image/*" onChange={(event) => upload('banner', event.target.files[0])} /></label>
          </div>
          <div className="profile-sidebar-card stagger-5">
            <h3>Cuenta</h3>
            <form className="compact-form" onSubmit={saveEmail} style={{ marginTop: 0 }}>
              <input aria-label="Nuevo correo" type="email" placeholder="Nuevo correo" value={email} onChange={(event) => setEmail(event.target.value)} required />
              <button className="button button-secondary button-wide" type="submit">Cambiar correo</button>
            </form>
            <form className="compact-form" onSubmit={savePassword} style={{ marginTop: 12 }}>
              <input aria-label="Contraseña actual" type="password" placeholder="Contraseña actual" value={passwords.currentPassword} onChange={(event) => setPasswords({ ...passwords, currentPassword: event.target.value })} required />
              <input aria-label="Nueva contraseña" type="password" placeholder="Nueva contraseña (8+)" value={passwords.newPassword} onChange={(event) => setPasswords({ ...passwords, newPassword: event.target.value })} required minLength={8} />
              <button className="button button-secondary button-wide" type="submit">Cambiar contraseña</button>
            </form>
          </div>
        </aside>
      </div>
    </div>
  )
}

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
  const [sidebarOpen, setSidebarOpen] = useState(false)

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
      if (authMode === 'register') {
        setAuthMode('login')
        setAuthForm({ email: '', password: '', displayName: '', role: 'User' })
      }
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

  const role = getTokenRole(token)
  const activeNav = activeView === 'profile'
    ? profileNavigationItem
    : navigationItems.find((item) => item.id === activeView) || navigationItems[0]
  const displayName = profile?.displayName || 'Mi perfil'
  const displayInitial = displayName[0] || 'P'

  if (!token) {
    return <AuthScreen mode={authMode} setMode={setAuthMode} form={authForm} setForm={setAuthForm} onSubmit={handleAuth} loading={loading} error={error} />
  }

  return (
    <main className="app-shell">
      <aside className={sidebarOpen ? 'app-sidebar open' : 'app-sidebar'}>
        <div className="sidebar-glow" aria-hidden="true" />
        <div className="sidebar-brand">
          <div className="brand-mark">P</div>
          <div><strong>PetPals</strong><span>Tu manada digital</span></div>
        </div>
        <div className="sidebar-welcome"><span>ESPACIO PERSONAL</span><strong>Hola, {displayName.split(' ')[0]}</strong></div>
        <nav className="sidebar-nav" aria-label="Navegación principal">
          <p className="sidebar-label">Explora PetPals</p>
          {navigationItems.map((item) => <button className={activeView === item.id ? 'sidebar-nav-button active' : 'sidebar-nav-button'} aria-current={activeView === item.id ? 'page' : undefined} type="button" key={item.id} onClick={() => { setActiveView(item.id); setSidebarOpen(false) }}><SidebarGlyph name={item.icon} /><span>{item.label}</span>{activeView === item.id && <i aria-hidden="true" />}</button>)}
        </nav>
        <div className="sidebar-quote"><span>“</span><p>Las mejores historias tienen patas.</p></div>
        <div className="sidebar-bottom">
          <button className={activeView === 'profile' ? 'sidebar-user-button active' : 'sidebar-user-button'} type="button" onClick={() => { setActiveView('profile'); setSidebarOpen(false) }}>
            <span className="avatar sidebar-avatar">{displayInitial}</span>
            <span className="sidebar-user-copy"><strong>{displayName}</strong><small>{role === 'Clinic' ? 'Veterinaria' : role === 'Shelter' ? 'Refugio' : 'Dueño de mascota'}</small></span>
            <span className="sidebar-user-arrow" aria-hidden="true">↗</span>
          </button>
          <button className="sidebar-logout" type="button" onClick={logout}><span aria-hidden="true">↪</span>Salir</button>
        </div>
      </aside>

      <div className="app-main">
        <header className="topbar">
          <div className="topbar-copy">
            <p className="eyebrow">{activeNav.eyebrow}</p>
            <div className="topbar-title-row"><h1 className="gradient-title">{activeNav.title}</h1><span className="live-badge"><i /> EN VIVO</span></div>
            <p className="topbar-subtitle">{activeNav.subtitle}</p>
          </div>
          <div className="topbar-actions"><span className="community-status"><i /> Comunidad conectada</span><button className="menu-button" type="button" aria-label="Abrir menú" aria-expanded={sidebarOpen} onClick={() => setSidebarOpen(!sidebarOpen)}><span aria-hidden="true">☰</span></button><button className="mobile-profile-button" type="button" aria-label="Abrir mi perfil" onClick={() => setActiveView('profile')}><span className="avatar">{displayInitial}</span></button></div>
        </header>

        {error && <div className="alert">{error}</div>}

        <div className="page-content view-enter" key={activeView}>
      {activeView === 'profile'
        ? <ProfileView profile={profile} setProfile={setProfile} token={token} onError={handleError} pets={pets} setPets={setPets} />
        : activeView === 'marketplace'
          ? <MarketplaceView token={token} role={role} onError={handleError} />
        : activeView === 'appointments'
          ? role === 'Clinic' ? <ClinicAppointmentsView token={token} onError={handleError} /> : <AppointmentsView token={token} pets={pets} onError={handleError} />
        : activeView === 'adoptions'
          ? <AdoptionsView token={token} role={role} onError={handleError} />
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
              <PetPicker pets={pets} value={selectedPet} onChange={setSelectedPet} />
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
        </div>
      </div>
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

function ClinicPublicProfile({ token, clinicId, onClose, onError }) {
  const [profile, setProfile] = useState(null)
  const [loading, setLoading] = useState(true)
  const [rating, setRating] = useState(5)
  const [comment, setComment] = useState('')

  useEffect(() => {
    marketplaceApi.clinicProfile(token, clinicId).then(setProfile).catch(onError).finally(() => setLoading(false))
  }, [clinicId, onError, token])

  async function saveReview(event) {
    event.preventDefault()
    try {
      await marketplaceApi.saveClinicReview(token, clinicId, { rating: Number(rating), comment })
      setComment('')
      setProfile(await marketplaceApi.clinicProfile(token, clinicId))
    } catch (requestError) { onError(requestError) }
  }

  if (loading) return <div className="card loading-card">Cargando perfil...</div>
  if (!profile) return null
  const { clinic, services, schedules, products, photos, clinicPhotos, adoptablePets, avgRating, reviewCount, reviews } = profile
  return (
    <div className="card" style={{ marginTop: 16 }}>
      {clinic.bannerUrl && <img src={profileImg(clinic.bannerUrl)} alt="Portada" style={{ width: '100%', height: 140, objectFit: 'cover', borderRadius: 8 }} />}
      <div style={{ display: 'flex', gap: 12, alignItems: 'center', marginTop: 8 }}>
        {clinic.logoUrl
          ? <img src={profileImg(clinic.logoUrl)} alt="Logo" style={{ width: 64, height: 64, borderRadius: '50%', objectFit: 'cover' }} />
          : <span className="pet-dot">{clinic.name?.[0]}</span>}
        <div><h3 style={{ margin: 0 }}>{clinic.name} {clinic.isVerified && <small className="muted">✓</small>}</h3>
          <small className="muted">★ {Number(avgRating || 0).toFixed(1)} ({reviewCount}) · {services.length} servicios · {photos.length} fotos · {adoptablePets.length} animales</small></div>
        <span style={{ flex: 1 }} />
        <button className="button button-ghost" type="button" onClick={onClose}>✕</button>
      </div>
      {clinic.description && <p>{clinic.description}</p>}
      <p className="muted">{clinic.address || 'Dirección no disponible'}{clinic.phone ? ` · ${clinic.phone}` : ''} · <a href={`https://www.openstreetmap.org/?mlat=${clinic.latitude}&mlon=${clinic.longitude}#map=17/${clinic.latitude}/${clinic.longitude}`} target="_blank" rel="noreferrer">Ver ubicación</a></p>
      <p className="eyebrow">FOTOS</p>
      {(clinicPhotos?.length > 0 || photos.length > 0) ? (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(100px,1fr))', gap: 8, margin: '8px 0' }}>
          {(clinicPhotos?.length ? clinicPhotos.map((p) => p.imageUrl) : photos).map((url) => <img key={url} src={profileImg(url)} alt="" style={{ width: '100%', height: 100, objectFit: 'cover', borderRadius: 8 }} />)}
        </div>
      ) : <p className="muted">Sin fotos todavía.</p>}
      <p className="eyebrow">SERVICIOS</p>
      {services.length ? <ul>{services.map((s) => <li key={s.id}><small><strong>{s.name}</strong>{s.price ? ` · $${s.price}` : ''}{s.durationMinutes ? ` · ${s.durationMinutes} min` : ''}</small></li>)}</ul> : <p className="muted">Sin servicios publicados.</p>}
      <p className="eyebrow">HORARIOS</p>
      {schedules.length ? <ul>{schedules.map((s) => <li key={s.id}><small>{s.dayOfWeek} · {s.opensAt}–{s.closesAt}</small></li>)}</ul> : <p className="muted">Sin horarios publicados.</p>}
      <p className="eyebrow">ANIMALES RELACIONADOS</p>
      {adoptablePets.length ? (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(120px,1fr))', gap: 8 }}>
          {adoptablePets.map((pet) => (
            <div key={pet.id}>
              {(pet.primaryImageUrl || pet.photos?.[0]?.imageUrl) && <img src={profileImg(pet.primaryImageUrl || pet.photos[0].imageUrl)} alt={pet.name} style={{ width: '100%', height: 90, objectFit: 'cover', borderRadius: 8 }} />}
              <small><strong>{pet.name}</strong> · {pet.species}</small>
            </div>
          ))}
        </div>
      ) : <p className="muted">Sin animales en adopción vinculados.</p>}
      <p className="eyebrow">RESEÑAS</p>
      {(reviews?.length > 0)
        ? <div>{reviews.map((r) => <div className="request-row" key={r.id}><strong>{r.authorName}</strong><span>{'★'.repeat(r.rating)}{'☆'.repeat(5 - r.rating)}</span>{r.comment && <small>{r.comment}</small>}</div>)}</div>
        : <p className="muted">Sin reseñas todavía. Sé la primera persona en opinar.</p>}
      <form className="publish-form" onSubmit={saveReview}>
        <select aria-label="Calificación" value={rating} onChange={(event) => setRating(event.target.value)}>
          <option value="5">★★★★★ (5)</option>
          <option value="4">★★★★ (4)</option>
          <option value="3">★★★ (3)</option>
          <option value="2">★★ (2)</option>
          <option value="1">★ (1)</option>
        </select>
        <textarea placeholder="Cuéntanos tu experiencia..." value={comment} onChange={(event) => setComment(event.target.value)} maxLength={1000} />
        <button className="button button-secondary" type="submit">Publicar reseña</button>
      </form>
    </div>
  )
}

function MapsView({ token, onError }) {
  const [clinics, setClinics] = useState([])
  const [location, setLocation] = useState(null)
  const [loading, setLoading] = useState(true)
  const [selectedClinicId, setSelectedClinicId] = useState(null)
  const defaultCenter = [40.4168, -3.7038] // Madrid as sensible default

  useEffect(() => {
    marketplaceApi.clinics(token)
      .then(setClinics)
      .catch(onError)
      .finally(() => setLoading(false))
    navigator.geolocation?.getCurrentPosition(
      (position) => setLocation({ latitude: position.coords.latitude, longitude: position.coords.longitude }),
      () => {},
      { timeout: 8000, maximumAge: 300000 }
    )
  }, [onError, token])

  const sortedClinics = [...clinics].sort((left, right) => distanceFrom(location, left) - distanceFrom(location, right))
  const mapClinic = sortedClinics[0]
  const rawCenter = mapClinic ? [mapClinic.latitude, mapClinic.longitude] : location ? [location.latitude, location.longitude] : defaultCenter
  const mapCenter = (Array.isArray(rawCenter) && rawCenter.every(c => Number.isFinite(c))) ? rawCenter : defaultCenter
  const zoomLevel = (mapClinic || location) ? 13 : 6

  return (
    <section className="maps-layout">
      <div className="maps-main">
        <div className="marketplace-heading">
          <div>
            <p className="eyebrow">PETPALS / MAPA</p>
            <h2>Veterinarias cerca de ti.</h2>
            <p className="marketplace-intro">Activa tu ubicación para ordenar los resultados por cercanía.</p>
          </div>
          <button className="button button-secondary" type="button" onClick={() => navigator.geolocation?.getCurrentPosition((position) => setLocation({ latitude: position.coords.latitude, longitude: position.coords.longitude }))}>Usar mi ubicación</button>
        </div>
        <div className="map-card">
          {loading && <div className="map-loading"><i /><span>Cargando mapa...</span></div>}
          <MapContainer center={mapCenter} zoom={zoomLevel} scrollWheelZoom>
            <TileLayer attribution='&copy; OpenStreetMap contributors' url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
            <MapCenter center={mapCenter} />
            {location && <CircleMarker center={[location.latitude, location.longitude]} pathOptions={{ color: '#c56b45', weight: 3 }}><Popup>Tu ubicación aproximada</Popup></CircleMarker>}
            {clinics.map((clinic) => {
              const lat = Number(clinic.latitude)
              const lng = Number(clinic.longitude)
              if (!Number.isFinite(lat) || !Number.isFinite(lng)) return null
              return <CircleMarker key={clinic.id} center={[lat, lng]} pathOptions={{ color: '#315c4b', weight: 3 }}><Popup><strong>{clinic.name}</strong><br />{clinic.address || 'Dirección no disponible'}</Popup></CircleMarker>
            })}
          </MapContainer>
        </div>
      </div>
      <aside className="clinic-list">
        <div className="section-heading"><div><p className="eyebrow">UBICACIONES</p><h2>Veterinarias</h2></div><span className="count-badge">{clinics.length}</span></div>
        {loading ? <div className="map-loading" style={{position: 'static', padding: '40px'}}><i /><span>Cargando ubicaciones...</span></div>
        : !clinics.length ? <div className="map-empty"><strong>Sin veterinarias aún</strong><span>Cuando una clínica se registre aparecerá aquí.</span></div>
        : sortedClinics.map((clinic) => <div className="clinic-row" key={clinic.id}><div><strong>{clinic.name}</strong><small>{clinic.address || 'Dirección no disponible'}</small></div><span className="distance">{formatDistance(distanceFrom(location, clinic))}</span><button className="button button-secondary" type="button" onClick={() => setSelectedClinicId(clinic.id)}>Ver perfil</button></div>)}
        {selectedClinicId && <ClinicPublicProfile token={token} clinicId={selectedClinicId} onClose={() => setSelectedClinicId(null)} onError={onError} />}
      </aside>
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
  const [shelter, setShelter] = useState(null)
  const [shelterForm, setShelterForm] = useState({ name: '', description: '', phone: '', address: '', latitude: 0, longitude: 0 })
  const [selected, setSelected] = useState(null)

  useEffect(() => {
    const loads = [adoptionApi.pets(token), role === 'Shelter' ? adoptionApi.shelterRequests(token) : adoptionApi.requests(token)]
    if (role === 'Shelter') loads.push(adoptionApi.myShelter(token).catch(() => null))
    Promise.all(loads)
      .then(([nextPets, nextRequests, nextShelter]) => {
        setPets(nextPets)
        setRequests(nextRequests)
        if (nextShelter) {
          setShelter(nextShelter)
          setShelterForm(nextShelter)
        }
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

  async function saveShelter(event) {
    event.preventDefault()
    try {
      const saved = await adoptionApi.saveShelter(token, {
        ...shelterForm, latitude: Number(shelterForm.latitude), longitude: Number(shelterForm.longitude),
      })
      setShelter(saved)
      setShelterForm(saved)
    } catch (requestError) {
      onError(requestError)
    }
  }

  async function uploadShelterPhoto(kind, file) {
    if (!file) return
    try {
      const updated = kind === 'logo'
        ? await adoptionApi.uploadShelterLogo(token, file)
        : await adoptionApi.uploadShelterBanner(token, file)
      setShelter(updated)
      setShelterForm(updated)
    } catch (requestError) {
      onError(requestError)
    }
  }

  async function openDetail(pet) {
    try {
      const full = await adoptionApi.pet(token, pet.id)
      setSelected(full)
    } catch (requestError) { onError(requestError) }
  }

  async function uploadAdoptablePhoto(file) {
    if (!file || !selected) return
    try {
      const photo = await adoptionApi.uploadPetPhoto(token, selected.id, file)
      const next = { ...selected, photos: [photo, ...(selected.photos || [])], primaryImageUrl: selected.primaryImageUrl || photo.imageUrl }
      setSelected(next)
      setPets((current) => current.map((p) => p.id === next.id ? { ...p, primaryImageUrl: next.primaryImageUrl, photos: next.photos } : p))
    } catch (requestError) { onError(requestError) }
  }

  async function removeAdoptablePhoto(photoId) {
    try {
      await adoptionApi.deletePetPhoto(token, selected.id, photoId)
      const next = { ...selected, photos: (selected.photos || []).filter((p) => p.id !== photoId) }
      setSelected(next)
      setPets((current) => current.map((p) => p.id === next.id ? { ...p, photos: next.photos } : p))
    } catch (requestError) { onError(requestError) }
  }

  async function makeAdoptablePrimary(photo) {
    try {
      const updated = await adoptionApi.setPetPrimary(token, selected.id, photo.id)
      setSelected(updated)
      setPets((current) => current.map((p) => p.id === updated.id ? updated : p))
    } catch (requestError) { onError(requestError) }
  }

  return (
    <section className="adoptions-layout">
      <div className="adoptions-main">
        <div className="marketplace-heading"><div><p className="eyebrow">PETPALS / ADOPCIONES</p><h2>Encuentra un nuevo compañero.</h2><p className="marketplace-intro">Conoce animales publicados por refugios de la comunidad.</p></div></div>
        {role === 'Shelter' && <form className="clinic-profile-form card" onSubmit={saveShelter}>
          <strong>{shelter ? 'Perfil del refugio' : 'Configura tu refugio'}</strong>
          {shelter?.bannerUrl && <img src={profileImg(shelter.bannerUrl)} alt="Portada" style={{ width: '100%', height: 120, objectFit: 'cover', borderRadius: 8 }} />}
          <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
            {shelter?.logoUrl
              ? <img src={profileImg(shelter.logoUrl)} alt="Logo" style={{ width: 56, height: 56, borderRadius: '50%', objectFit: 'cover' }} />
              : <span className="pet-dot">{shelterForm.name?.[0] || 'R'}</span>}
            {shelter?.isVerified && <small className="muted">✓ Verificado</small>}
          </div>
          <input required placeholder="Nombre" value={shelterForm.name} onChange={(event) => setShelterForm({ ...shelterForm, name: event.target.value })} />
          <textarea placeholder="Descripción" value={shelterForm.description || ''} onChange={(event) => setShelterForm({ ...shelterForm, description: event.target.value })} maxLength={1000} />
          <input placeholder="Dirección" value={shelterForm.address} onChange={(event) => setShelterForm({ ...shelterForm, address: event.target.value })} />
          <input placeholder="Teléfono" value={shelterForm.phone} onChange={(event) => setShelterForm({ ...shelterForm, phone: event.target.value })} />
          <div className="form-row"><input type="number" step="any" placeholder="Latitud" value={shelterForm.latitude} onChange={(event) => setShelterForm({ ...shelterForm, latitude: event.target.value })} /><input type="number" step="any" placeholder="Longitud" value={shelterForm.longitude} onChange={(event) => setShelterForm({ ...shelterForm, longitude: event.target.value })} /></div>
          <label>Logo<input type="file" accept="image/*" onChange={(event) => uploadShelterPhoto('logo', event.target.files[0])} /></label>
          <label>Portada<input type="file" accept="image/*" onChange={(event) => uploadShelterPhoto('banner', event.target.files[0])} /></label>
          <button className="button button-secondary" type="submit">Guardar perfil</button>
        </form>}
        {loading && <div className="card loading-card">Cargando animales...</div>}
        {!loading && !pets.length && <div className="card empty-card"><h2>No hay animales disponibles</h2><p>Vuelve más tarde para conocer nuevas historias.</p></div>}
        <div className="adoption-grid">{pets.map((pet) => <article className="adoption-card card" key={pet.id}>{(pet.primaryImageUrl || pet.photos?.[0]?.imageUrl) && <img src={profileImg(pet.primaryImageUrl || pet.photos[0].imageUrl)} alt={pet.name} style={{ width: '100%', height: 160, objectFit: 'cover', borderRadius: 8 }} />}<div className="adoption-info"><p className="eyebrow">{pet.shelterName}</p><h3>{pet.name}</h3><p>{pet.species}{pet.breed ? ` · ${pet.breed}` : ''}{pet.approximateAge ? ` · ${pet.approximateAge}` : ''}</p><p>{pet.description || 'Este animal busca un hogar responsable.'}</p>{pet.vaccinations?.length > 0 && <small>{pet.vaccinations.length} vacuna(s) registradas</small>}<div style={{ display: 'flex', gap: 8 }}><button className="button button-secondary" type="button" onClick={() => openDetail(pet)}>Ver detalle</button>{role !== 'Shelter' && <button className="button button-primary" type="button" onClick={() => requestAdoption(pet.id)}>Solicitar</button>}</div></div></article>)}</div>
        {selected && (
          <div className="card" style={{ marginTop: 16 }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <h3>{selected.name} · {selected.species}</h3>
              <button className="button button-ghost" type="button" onClick={() => setSelected(null)}>✕</button>
            </div>
            <p className="muted">{selected.shelterName}{selected.breed ? ` · ${selected.breed}` : ''}{selected.sex ? ` · ${selected.sex}` : ''}{selected.approximateAge ? ` · ${selected.approximateAge}` : ''}</p>
            {(selected.photos?.length > 0 || selected.primaryImageUrl) && (
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(110px,1fr))', gap: 8, margin: '8px 0' }}>
                {selected.primaryImageUrl && <img src={profileImg(selected.primaryImageUrl)} alt="" style={{ width: '100%', height: 110, objectFit: 'cover', borderRadius: 8 }} />}
                {(selected.photos || []).map((photo) => (
                  <div key={photo.id}>
                    <img src={profileImg(photo.imageUrl)} alt="" style={{ width: '100%', height: 110, objectFit: 'cover', borderRadius: 8 }} />
                    {role === 'Shelter' && <div style={{ display: 'flex', gap: 4, marginTop: 4 }}>
                      <button className="button button-ghost" type="button" title="Perfil" onClick={() => makeAdoptablePrimary(photo)}>★</button>
                      <button className="button button-ghost" type="button" title="Eliminar" onClick={() => removeAdoptablePhoto(photo.id)}>✕</button>
                    </div>}
                  </div>
                ))}
              </div>
            )}
            {role === 'Shelter' && <label><small className="muted">Añadir foto </small><input type="file" accept="image/*" onChange={(event) => uploadAdoptablePhoto(event.target.files[0])} /></label>}
            <p>{selected.description || 'Este animal busca un hogar responsable.'}</p>
            <p className="eyebrow">VACUNAS / HISTORIAL</p>
            {(selected.vaccinations?.length > 0)
              ? <ul>{selected.vaccinations.map((v) => <li key={v.id}><small>{v.vaccineName} · {v.appliedOn}{v.nextDueOn ? ` → ${v.nextDueOn}` : ''}{v.notes ? ` — ${v.notes}` : ''}</small></li>)}</ul>
              : <p className="muted">Sin vacunas registradas todavía.</p>}
            {role !== 'Shelter' && <button className="button button-primary" type="button" onClick={() => requestAdoption(selected.id)}>Solicitar adopción</button>}
          </div>
        )}
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

function ClinicGallery({ token, clinic, onLogo, onError }) {
  const [photos, setPhotos] = useState([])
  const [open, setOpen] = useState(false)

  useEffect(() => {
    if (!open || !clinic?.id) return
    marketplaceApi.clinicPhotos(token, clinic.id).then(setPhotos).catch(onError)
  }, [open, clinic?.id]) // ponytail: lazy, solo carga al abrir

  async function upload(file) {
    if (!file) return
    try {
      const photo = await marketplaceApi.uploadClinicPhoto(token, file)
      setPhotos((current) => [photo, ...current])
      if (!clinic.logoUrl) onLogo?.(photo.imageUrl)
    } catch (requestError) { onError(requestError) }
  }

  async function remove(photoId) {
    try {
      await marketplaceApi.deleteClinicPhoto(token, photoId)
      setPhotos((current) => current.filter((p) => p.id !== photoId))
    } catch (requestError) { onError(requestError) }
  }

  async function makePrimary(photo) {
    try {
      const updated = await marketplaceApi.setClinicPrimary(token, photo.id)
      onLogo?.(updated.logoUrl)
    } catch (requestError) { onError(requestError) }
  }

  if (!clinic?.id) return null
  return (
    <div style={{ marginTop: 8 }}>
      <button className="button button-ghost" type="button" onClick={() => setOpen(!open)}>{open ? 'Cerrar galería' : 'Galería de fotos'}</button>
      {open && (
        <div style={{ marginTop: 8 }}>
          <label><small className="muted">Añadir foto </small><input type="file" accept="image/*" onChange={(event) => upload(event.target.files[0])} /></label>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill,minmax(90px,1fr))', gap: 8, marginTop: 8 }}>
            {photos.map((photo) => (
              <div key={photo.id}>
                <img src={profileImg(photo.imageUrl)} alt="" style={{ width: '100%', height: 90, objectFit: 'cover', borderRadius: 8 }} />
                <div style={{ display: 'flex', gap: 4, marginTop: 4 }}>
                  <button className="button button-ghost" type="button" title="Usar como logo" onClick={() => makePrimary(photo)}>★</button>
                  <button className="button button-ghost" type="button" title="Eliminar" onClick={() => remove(photo.id)}>✕</button>
                </div>
              </div>
            ))}
          </div>
          {!photos.length && <p className="muted">Sin fotos todavía.</p>}
        </div>
      )}
    </div>
  )
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

  async function uploadClinicPhoto(kind, file) {
    if (!file) return
    try {
      const updated = kind === 'logo'
        ? await marketplaceApi.uploadClinicLogo(token, file)
        : await marketplaceApi.uploadClinicBanner(token, file)
      setClinic(updated)
      setClinicForm(updated)
    } catch (requestError) {
      onError(requestError)
    }
  }

  return (
    <section className="marketplace-layout">
      <div className="marketplace-main">
        <div className="marketplace-heading"><div><p className="eyebrow">PETPALS / MARKETPLACE</p><h2>Cuida su mundo.</h2><p className="marketplace-intro">Productos seleccionados por veterinarias de la comunidad.</p></div><select aria-label="Filtrar por categoría" value={category} onChange={(event) => setCategory(event.target.value)}><option value="">Todas las categorías</option><option value="0">Alimento</option><option value="1">Vacunas</option><option value="2">Medicinas</option><option value="3">Accesorios</option><option value="4">Higiene</option><option value="5">Otros</option></select></div>
        {role === 'Clinic' && <form className="clinic-profile-form card" onSubmit={saveClinic}>
          <strong>{clinic ? 'Perfil de veterinaria' : 'Configura tu veterinaria'}</strong>
          {clinic?.bannerUrl && <img src={profileImg(clinic.bannerUrl)} alt="Portada" style={{ width: '100%', height: 120, objectFit: 'cover', borderRadius: 8 }} />}
          <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
            {clinic?.logoUrl
              ? <img src={profileImg(clinic.logoUrl)} alt="Logo" style={{ width: 56, height: 56, borderRadius: '50%', objectFit: 'cover' }} />
              : <span className="pet-dot">{clinicForm.name?.[0] || 'V'}</span>}
            {clinic?.isVerified && <small className="muted">✓ Verificada</small>}
          </div>
          <input required placeholder="Nombre" value={clinicForm.name} onChange={(event) => setClinicForm({ ...clinicForm, name: event.target.value })} />
          <textarea placeholder="Descripción del negocio" value={clinicForm.description || ''} onChange={(event) => setClinicForm({ ...clinicForm, description: event.target.value })} maxLength={1000} />
          <input placeholder="Dirección" value={clinicForm.address} onChange={(event) => setClinicForm({ ...clinicForm, address: event.target.value })} />
          <input placeholder="Teléfono" value={clinicForm.phone} onChange={(event) => setClinicForm({ ...clinicForm, phone: event.target.value })} />
          <div className="form-row"><input type="number" step="any" placeholder="Latitud" value={clinicForm.latitude} onChange={(event) => setClinicForm({ ...clinicForm, latitude: event.target.value })} /><input type="number" step="any" placeholder="Longitud" value={clinicForm.longitude} onChange={(event) => setClinicForm({ ...clinicForm, longitude: event.target.value })} /></div>
          <label>Logo<input type="file" accept="image/*" onChange={(event) => uploadClinicPhoto('logo', event.target.files[0])} /></label>
          <label>Portada<input type="file" accept="image/*" onChange={(event) => uploadClinicPhoto('banner', event.target.files[0])} /></label>
          <ClinicGallery token={token} clinic={clinic} onLogo={(url) => { setClinic((c) => c && { ...c, logoUrl: url }); setClinicForm((f) => ({ ...f, logoUrl: url })) }} onError={onError} />
          <button className="button button-secondary" type="submit">Guardar perfil</button>
        </form>}
        {loading && <div className="card loading-card">Cargando productos...</div>}
        {!loading && !products.length && <div className="card empty-card"><span className="empty-icon">+</span><h2>Aún no hay productos</h2><p>Prueba otra categoría o vuelve más tarde.</p></div>}
        <div className="product-grid">{products.map((product) => <article className="product-card card" key={product.id}><div className="product-art"><span>{product.category === 0 ? 'FOOD' : 'CARE'}</span></div><div className="product-info"><p className="eyebrow">{product.clinicName}</p><h3>{product.name}</h3><p>{product.description || 'Producto para el cuidado de tu mascota.'}</p><div className="product-bottom"><strong>${product.price.toFixed(2)}</strong><button className="button button-primary" disabled={!product.stock} type="button" onClick={() => addProduct(product.id)}>{product.stock ? 'Añadir' : 'Agotado'}</button></div><small>{product.stock} disponibles</small></div></article>)}</div>
      </div>
      {role !== 'Clinic' ? <aside className="cart-panel card"><div className="section-heading"><div><p className="eyebrow">TU CARRITO</p><h2>Resumen</h2></div><span className="count-badge">{cart?.items?.length || 0}</span></div>{!cart?.items?.length ? <p className="muted">Tu carrito está vacío.</p> : <><div className="cart-items">{cart.items.map((item) => <div className="cart-item" key={item.id}><div><strong>{item.productName}</strong><small>{item.clinicName} · {item.quantity} x ${item.unitPrice.toFixed(2)}</small></div><b>${item.subtotal.toFixed(2)}</b></div>)}</div><div className="cart-total"><span>Total</span><strong>${cart.total.toFixed(2)}</strong></div><button className="button button-primary button-wide" type="button" onClick={checkout}>Confirmar pedido</button><small className="cart-note">Sin pago real en esta versión.</small></>}</aside> : <aside className="cart-panel card"><p className="eyebrow">GESTIÓN DE VETERINARIA</p><form className="publish-form" onSubmit={createProduct}><strong>Nuevo producto</strong><input required placeholder="Nombre" value={productForm.name} onChange={(event) => setProductForm({ ...productForm, name: event.target.value })} /><input placeholder="Descripción" value={productForm.description} onChange={(event) => setProductForm({ ...productForm, description: event.target.value })} /><input type="number" min="0" step="0.01" placeholder="Precio" value={productForm.price} onChange={(event) => setProductForm({ ...productForm, price: event.target.value })} /><input type="number" min="0" placeholder="Stock" value={productForm.stock} onChange={(event) => setProductForm({ ...productForm, stock: event.target.value })} /><button className="button button-secondary" type="submit">Publicar producto</button></form><div className="clinic-orders"><strong>Pedidos</strong>{orders.map((order) => <div className="request-row" key={order.id}><span>{order.id.slice(0, 8)} · ${order.total.toFixed(2)}</span><select value={order.status} onChange={(event) => updateOrder(order.id, event.target.value)}><option value="0">Pendiente</option><option value="1">Confirmado</option><option value="2">Cancelado</option><option value="3">Completado</option></select></div>)}</div></aside>}
    </section>
  )
}

function AuthScreen({ mode, setMode, form, setForm, onSubmit, loading, error }) {
  const [showPassword, setShowPassword] = useState(false)

  return (
    <main className={mode === 'register' ? 'auth-shell register-mode' : 'auth-shell'}>
      <section className="auth-visual">
        <p className="eyebrow">PETPALS / SOCIAL FEED</p>
        <div className="auth-animal-wall" aria-hidden="true">
          <figure className="animal-tile animal-tile-one"><img src="https://images.unsplash.com/photo-1552053831-71594a27632d?auto=format&fit=crop&w=500&q=82" alt="" /></figure>
          <figure className="animal-tile animal-tile-two"><img src="https://images.unsplash.com/photo-1518791841217-8f162f1e1131?auto=format&fit=crop&w=500&q=82" alt="" /></figure>
          <figure className="animal-tile animal-tile-three"><img src="https://images.unsplash.com/photo-1548199973-03cce0bbc87b?auto=format&fit=crop&w=500&q=82" alt="" /></figure>
          <figure className="animal-tile animal-tile-four"><img src="https://images.unsplash.com/photo-1573865526739-10659fec78a5?auto=format&fit=crop&w=500&q=82" alt="" /></figure>
          <figure className="animal-tile animal-tile-five"><img src="https://images.unsplash.com/photo-1585110396000-c9ffd4e4b308?auto=format&fit=crop&w=500&q=82" alt="" /></figure>
        </div>
        <div className="auth-slogan"><span>Pequeños momentos.</span><strong>Grandes historias.</strong></div>
        <p>Un lugar para compartir la vida que construyes con tus mascotas.</p>
        <div className="orbit orbit-one" /><div className="orbit orbit-two" />
        <div className="paw-note">La comunidad empieza con una historia.</div>
      </section>
      <section className="auth-panel">
        <div className="auth-form-wrap" key={mode}>
          <div className="brand-lockup"><div className="brand-mark">P</div><span>PetPals</span></div>
          <p className="eyebrow">{mode === 'login' ? 'BIENVENIDO DE VUELTA' : 'ÚNETE A LA MANADA'}</p>
          <h1>{mode === 'login' ? 'Vuelve a tu comunidad.' : 'Crea tu espacio.'}</h1>
          <p className="auth-intro">{mode === 'login' ? 'Continúa compartiendo esos momentos que importan.' : 'Comparte la vida de tus mascotas con personas que la entienden.'}</p>
          {error && <div className="alert">{error}</div>}
          <form className="auth-form" onSubmit={onSubmit}>
            {mode === 'register' && (
              <>
                <label>Nombre visible<input required value={form.displayName} onChange={(event) => setForm({ ...form, displayName: event.target.value })} /></label>
                <fieldset className="role-picker"><legend>Tipo de cuenta</legend><div className="role-options">{[{ value: 'User', title: 'Dueño', sub: 'Comparte tu manada' }, { value: 'Clinic', title: 'Veterinaria', sub: 'Ofrece servicios' }, { value: 'Shelter', title: 'Refugio', sub: 'Publica adopciones' }].map((option) => <label className="role-option" key={option.value}><input type="radio" name="role" value={option.value} checked={form.role === option.value} onChange={(event) => setForm({ ...form, role: event.target.value })} /><strong>{option.title}</strong><small>{option.sub}</small></label>)}</div></fieldset>
              </>
            )}
            <label>Email<input required type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></label>
            <label>Contraseña
              <div className="password-field">
                <input required type={showPassword ? 'text' : 'password'} minLength="8" value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} />
                <button className="password-toggle" type="button" aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'} onClick={() => setShowPassword(!showPassword)}>
                  {showPassword ? (
                    <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <path d="M3 3l18 18" /><path d="M10.58 10.58a2 2 0 0 0 2.83 2.83" />
                      <path d="M9.88 5.09A10.94 10.94 0 0 1 12 4.5c5 0 8.5 5.5 8.5 5.5a15.7 15.7 0 0 1-3.07 3.73M6.61 6.61C4.18 8.15 2.5 10.5 2.5 10.5S6 16 12 16c1.15 0 2.22-.2 3.2-.52" />
                    </svg>
                  ) : (
                    <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <path d="M2.5 12s3.5-5.5 9.5-5.5S21.5 12 21.5 12 18 17.5 12 17.5 2.5 12 2.5 12Z" /><circle cx="12" cy="12" r="2.5" />
                    </svg>
                  )}
                </button>
              </div>
            </label>
            <button className="button button-primary button-wide" disabled={loading} type="submit">{loading ? 'Procesando...' : mode === 'login' ? 'Entrar a PetPals' : 'Crear cuenta'}</button>
          </form>
          <button className="switch-auth" type="button" onClick={() => setMode(mode === 'login' ? 'register' : 'login')}>
            {mode === 'login' ? '¿Aún no tienes cuenta? Regístrate' : 'Ya tengo una cuenta'}
          </button>
        </div>
      </section>
    </main>
  )
}

export default App
