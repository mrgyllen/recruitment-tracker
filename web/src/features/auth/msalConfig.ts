import { type Configuration, PublicClientApplication } from '@azure/msal-browser'

const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID,
    authority: import.meta.env.VITE_ENTRA_AUTHORITY,
    redirectUri: import.meta.env.VITE_ENTRA_REDIRECT_URI,
    postLogoutRedirectUri: import.meta.env.VITE_ENTRA_REDIRECT_URI,
  },
  cache: {
    cacheLocation: 'sessionStorage',
  },
}

export const loginRequest = {
  scopes: [`api://${import.meta.env.VITE_ENTRA_CLIENT_ID}/.default`],
}

export const msalInstance = new PublicClientApplication(msalConfig)
