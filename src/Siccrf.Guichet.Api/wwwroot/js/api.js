/**
 * Client HTTP de l'API du guichet intelligent.
 * Gère le jeton JWT (localStorage) et les erreurs HTTP.
 */

const API_BASE = '/api';

async function requete(chemin, options = {}) {
  const jeton = localStorage.getItem('jeton');
  const enTetes = { 
    'Content-Type': 'application/json', 
    ...(options.headers || {}) 
  };
  if (jeton) enTetes['Authorization'] = `Bearer ${jeton}`;

  const reponse = await fetch(`${API_BASE}${chemin}`, {
    ...options,
    headers: enTetes
  });

  if (reponse.status === 401) {
    // Jeton absent ou expiré : retour à l'écran de connexion
    localStorage.removeItem('jeton');
    localStorage.removeItem('agent');
    window.dispatchEvent(new CustomEvent('session-expiree'));
    throw new Error('Session expirée. Veuillez vous reconnecter.');
  }

  if (!reponse.ok) {
    let detail = '';
    try {
      const corps = await reponse.json();
      detail = corps.erreur || corps.message || JSON.stringify(corps);
    } catch {
      detail = reponse.statusText;
    }
    throw new Error(detail || `Erreur HTTP ${reponse.status}`);
  }

  if (reponse.status === 204) return null;
  return reponse.json();
}

export const api = {
  // Authentification
  login: (identifiant, motDePasse) =>
    requete('/auth/login', { method: 'POST', body: JSON.stringify({ identifiant, motDePasse }) }),

  // Demandes
  listerDemandes: () => requete('/demandes'),
  creerDemande: (message) => requete('/demandes', { method: 'POST', body: JSON.stringify({ message }) }),
  consulterDemande: (id) => requete(`/demandes/${id}`),
  envoyerMessage: (id, message) => requete(`/demandes/${id}/dialogue`, { method: 'POST', body: JSON.stringify({ message }) }),
  editerFiche: (id, fiche) => requete(`/demandes/${id}/fiche`, { method: 'PUT', body: JSON.stringify(fiche) }),
  accepterProposition: (id, versionId) => requete(`/demandes/${id}/accepter-proposition`, { method: 'POST', body: JSON.stringify({ versionId }) }),

  // Export de la fiche (binaire : Word/PDF)
  exporter: async (id, format) => {
    const jeton = localStorage.getItem('jeton');
    const reponse = await fetch(`${API_BASE}/demandes/${id}/export?format=${format}`, {
      headers: { Authorization: `Bearer ${jeton}` }
    });

    if (reponse.status === 401) {
      localStorage.removeItem('jeton');
      localStorage.removeItem('agent');
      window.dispatchEvent(new CustomEvent('session-expiree'));
      throw new Error('Session expirée. Veuillez vous reconnecter.');
    }

    if (!reponse.ok) {
      let detail = '';
      try {
        const corps = await reponse.json();
        detail = corps.erreur || corps.message || JSON.stringify(corps);
      } catch {
        detail = reponse.statusText;
      }
      throw new Error(detail || `Erreur HTTP ${reponse.status}`);
    }

    return reponse.blob();
  },

  // Validation
  soumettre: (id) => requete(`/demandes/${id}/soumettre`, { method: 'POST', body: JSON.stringify({ confirme: true }) }),
  fileEnAttente: () => requete('/validations/en-attente'),
  decider: (demandeId, decision, commentaire) =>
    requete(`/validations/${demandeId}/decision`, { method: 'POST', body: JSON.stringify({ decision, commentaire }) })
};
