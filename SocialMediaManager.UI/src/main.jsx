import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import OAuthCallback from './OAuthCallback.jsx'

const currentPath = window.location.pathname;
const isOAuthCallback = currentPath.includes('/oauth/callback/');

createRoot(document.getElementById('root')).render(
  <StrictMode>
    {isOAuthCallback ? <OAuthCallback /> : <App />}
  </StrictMode>,
)
