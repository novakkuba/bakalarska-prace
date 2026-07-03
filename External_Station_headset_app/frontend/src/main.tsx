import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'

import { OpenAPI } from './client';     
import { BACKEND_URL } from './config'; 

OpenAPI.BASE = BACKEND_URL;

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
