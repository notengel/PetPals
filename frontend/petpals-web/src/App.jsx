import { useEffect, useState } from 'react'
import { authApi, clearStoredToken, getStoredToken, socialApi, storeToken } from './api'
import './App.css'

function App() {
  const [token, setToken] = useState(getStoredToken)
  const [profile, setProfile] = useState(null)
  const [pets, setPets] = useState([])
  const [posts, setPosts] = useState([])
  const [comments, setComments] = useState({})
  const [authMode, setAuthMode] = useState('login')
  const [authForm, setAuthForm] = useState({ email: '', password: '', displayName: '' })
  const [postText, setPostText] = useState('')
  const [selectedPet, setSelectedPet] = useState('')
  const [petForm, setPetForm] = useState({ name: '', species: '' })
  const [commentForms, setCommentForms] = useState({})
  const [loading, setLoading] = useState(Boolean(token))
  const [error, setError] = useState('')

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
        ? { ...authForm, role: 'User' }
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
          <span className="user-pill">{profile?.displayName || 'Mi perfil'}</span>
          <button className="button button-ghost" type="button" onClick={logout}>Salir</button>
        </div>
      </header>

      {error && <div className="alert">{error}</div>}

      <div className="content-grid">
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
      </div>
    </main>
  )
}

function AuthScreen({ mode, setMode, form, setForm, onSubmit, loading, error }) {
  return (
    <main className="auth-shell">
      <section className="auth-visual"><p className="eyebrow">PETPALS / SOCIAL FEED</p><div className="auth-slogan"><span>Pequeños momentos.</span><strong>Grandes historias.</strong></div><p>Un lugar para compartir la vida que construyes con tus mascotas.</p><div className="orbit orbit-one" /><div className="orbit orbit-two" /><div className="paw-note">La comunidad empieza con una historia.</div></section>
      <section className="auth-panel"><div className="auth-form-wrap"><div className="brand-lockup"><div className="brand-mark">P</div><span>PetPals</span></div><p className="eyebrow">{mode === 'login' ? 'BIENVENIDO DE VUELTA' : 'ÚNETE A LA MANADA'}</p><h1>{mode === 'login' ? 'Vuelve a tu comunidad.' : 'Crea tu espacio.'}</h1><p className="auth-intro">{mode === 'login' ? 'Continúa compartiendo esos momentos que importan.' : 'Comparte la vida de tus mascotas con personas que la entienden.'}</p>{error && <div className="alert">{error}</div>}<form className="auth-form" onSubmit={onSubmit}>{mode === 'register' && <label>Nombre visible<input required value={form.displayName} onChange={(event) => setForm({ ...form, displayName: event.target.value })} /></label>}<label>Email<input required type="email" value={form.email} onChange={(event) => setForm({ ...form, email: event.target.value })} /></label><label>Contraseña<input required type="password" minLength="8" value={form.password} onChange={(event) => setForm({ ...form, password: event.target.value })} /></label><button className="button button-primary button-wide" disabled={loading} type="submit">{loading ? 'Procesando...' : mode === 'login' ? 'Entrar a PetPals' : 'Crear cuenta'}</button></form><button className="switch-auth" type="button" onClick={() => setMode(mode === 'login' ? 'register' : 'login')}>{mode === 'login' ? '¿Aún no tienes cuenta? Regístrate' : 'Ya tengo una cuenta'}</button></div></section>
    </main>
  )
}

export default App
