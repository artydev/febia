/**
 * Logique applicative du guichet intelligent (vanilla JS + store à signaux).
 * Gère : connexion, liste des demandes, détail + fiche éditable, dialogue IA,
 * soumission, file de validation et décisions.
 */

import { signal, effect } from './store.js';
import { api } from './api.js';

// --- État global (signaux) ---
const etat = {
    agent: signal(null),            // { agentId, nomComplet, profil }
    demandes: signal([]),           // liste des demandes (selon profil)
    demandeActive: signal(null),    // demande sélectionnée
    fileValidation: signal([]),     // file du chef de service
    historiqueDialogue: signal([]), // messages du dialogue courant
    chargement: signal(false),
    erreur: signal('')
};

// --- Références DOM ---
const $ = (sel) => document.querySelector(sel);
const vueConnexion = $('#vue-connexion');
const vueApplication = $('#vue-application');
const zoneSession = $('#zone-session');
const listeDemandes = $('#liste-demandes');
const vueVide = $('#vue-vide');
const vueDetail = $('#vue-detail');
const vueValidation = $('#vue-validation');
const formConnexion = $('#form-connexion');
const formFiche = $('#form-fiche');
const formDialogue = $('#form-dialogue');
const historiqueDialogue = $('#historique-dialogue');
const listeValidation = $('#liste-validation');
const zonePoints = $('#zone-points');
const listePoints = $('#liste-points');
const messageFiche = $('#message-fiche');
const erreurConnexion = $('#erreur-connexion');

// --- Utilitaires ---
function afficherErreur(message) {
    etat.erreur.value = message;
    alert(message);
}

function formaterDate(iso) {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('fr-FR');
}

function profilEst(profil) {
    return etat.agent.value?.profil === profil;
}

// Libellés d'affichage (les valeurs techniques de l'API restent inchangées)
const LIBELLES_STATUT = {
    Brouillon: 'Brouillon',
    Soumise: 'Soumise',
    EnInstruction: 'En instruction',
    Validee: 'Validée',
    ACompleter: 'À compléter',
    Refusee: 'Refusée',
    Cloturee: 'Clôturée'
};

const LIBELLES_PROFIL = {
    Agent: 'Agent',
    ChefService: 'Chef de service',
    BureauXA: 'Bureau XA',
    Siccrf: 'SICCRF'
};

function libelleStatut(statut) {
    return LIBELLES_STATUT[statut] ?? statut;
}

function libelleProfil(profil) {
    return LIBELLES_PROFIL[profil] ?? profil;
}

function initiales(nomComplet) {
    return (nomComplet || '?')
        .split(/\s+/)
        .filter(Boolean)
        .slice(0, 2)
        .map((mot) => mot[0].toUpperCase())
        .join('');
}

// --- Connexion ---
formConnexion.addEventListener('submit', async (e) => {
    e.preventDefault();
    erreurConnexion.hidden = true;
    const identifiant = $('#identifiant').value.trim();
    const motDePasse = $('#mot-de-passe').value;

    try {
        const login = await api.login(identifiant, motDePasse);
        localStorage.setItem('jeton', login.jeton);
        localStorage.setItem('agent', JSON.stringify({
            agentId: login.agentId,
            nomComplet: login.nomComplet,
            profil: login.profil
        }));
        etat.agent.value = JSON.parse(localStorage.getItem('agent'));
        basculerVues();
        await chargerDonnees();
    } catch (err) {
        erreurConnexion.textContent = err.message;
        erreurConnexion.hidden = false;
    }
});

function deconnexion() {
    localStorage.removeItem('jeton');
    localStorage.removeItem('agent');
    etat.agent.value = null;
    etat.demandes.value = [];
    etat.demandeActive.value = null;
    etat.fileValidation.value = [];
    basculerVues();
}

window.addEventListener('session-expiree', () => {
    etat.agent.value = null;
    basculerVues();
});

function basculerVues() {
    const connecte = !!etat.agent.value;
    vueConnexion.hidden = connecte;
    vueApplication.hidden = !connecte;

    // Le bouton « File de validation » n'a de sens que pour le chef de service
    boutonValidation.hidden = !(connecte && profilEst('ChefService'));

    if (connecte) {
        zoneSession.innerHTML = `
      <div class="utilisateur">
        <span class="avatar" aria-hidden="true">${initiales(etat.agent.value.nomComplet)}</span>
        <span class="utilisateur-nom">
          <strong>${etat.agent.value.nomComplet}</strong>
          <small>${libelleProfil(etat.agent.value.profil)}</small>
        </span>
      </div>
      <button id="btn-deconnexion">Se déconnecter</button>
    `;
        $('#btn-deconnexion').addEventListener('click', deconnexion);
    } else {
        zoneSession.innerHTML = '';
    }
}

// --- Chargement des données ---
async function chargerDonnees() {
    etat.chargement.value = true;
    try {
        etat.demandes.value = await api.listerDemandes();
        if (profilEst('ChefService')) {
            etat.fileValidation.value = await api.fileEnAttente();
        }
    } catch (err) {
        afficherErreur(err.message);
    } finally {
        etat.chargement.value = false;
    }
}

// --- Liste des demandes (réactive) ---
effect(() => {
    const demandes = etat.demandes.value;
    const activeId = etat.demandeActive.value?.id;

    listeDemandes.innerHTML = '';
    if (!demandes.length) {
        listeDemandes.innerHTML = '<p class="vide">Aucune demande.</p>';
        return;
    }

    for (const d of demandes) {
        const item = document.createElement('div');
        item.className = 'item-demande' + (d.id === activeId ? ' actif' : '');
        item.innerHTML = `
      <div class="numero">${d.numero}</div>
      <div class="intitule">${d.ficheCourante?.intituleCourt || 'Sans intitulé'}</div>
      <div class="meta"><span class="badge ${d.statut}">${libelleStatut(d.statut)}</span><span>${formaterDate(d.dateCreation)}</span></div>
    `;
        item.addEventListener('click', () => selectionnerDemande(d.id));
        listeDemandes.appendChild(item);
    }
});

async function selectionnerDemande(id) {
    try {
        const demande = await api.consulterDemande(id);
        etat.demandeActive.value = demande;
        etat.historiqueDialogue.value = [];
        vueVide.hidden = true;
        vueDetail.hidden = false;
        vueValidation.hidden = true;
        remplirFiche(demande.ficheCourante);
        afficherPoints(demande.pointsAPreciser);
        afficherStatut(demande);
        gererBoutons(demande);
    } catch (err) {
        afficherErreur(err.message);
    }
}

function remplirFiche(fiche) {
    if (!fiche) return;
    const champs = [
        'applicationOuService', 'intituleCourt', 'synthese', 'situationActuelle',
        'difficultesRencontrees', 'resultatRecherche', 'criteresDeReussite',
        'solutionEnvisagee', 'utilisateursConcernes', 'processusMetierConcerne',
        'beneficesAttendus', 'echeanceSouhaitee', 'contraintes', 'piecesComplementaires'
    ];
    for (const champ of champs) {
        const el = formFiche.elements[champ];
        if (el) el.value = fiche[champ] || '';
    }
}

function afficherPoints(points) {
    if (points && points.length) {
        zonePoints.hidden = false;
        listePoints.innerHTML = points.map((p) => `<li>${p}</li>`).join('');
    } else {
        zonePoints.hidden = true;
    }
}

function afficherStatut(demande) {
    $('#detail-numero').textContent = demande.numero;
    const badge = $('#detail-statut');
    badge.textContent = libelleStatut(demande.statut);
    badge.className = 'badge ' + demande.statut;
    $('#detail-meta').textContent =
        `Demandeur : ${demande.demandeur} · Bureau : ${demande.bureau} · Créée le ${formaterDate(demande.dateCreation)}` +
        (demande.valideur ? ` · Valideur : ${demande.valideur}` : '');
}

function gererBoutons(demande) {
    const boutonSoumettre = $('#btn-soumettre');
    const editable = demande.statut === 'Brouillon' || demande.statut === 'ACompleter';
    boutonSoumettre.hidden = !editable;
    formFiche.querySelectorAll('input, textarea').forEach((el) => {
        el.disabled = !editable;
    });
    formDialogue.querySelector('input').disabled = !editable;
    formDialogue.querySelector('button').disabled = !editable;
}

// --- Création d'une nouvelle demande ---
$('#btn-nouvelle').addEventListener('click', async () => {
    const message = prompt('Décrivez votre besoin en quelques mots :');
    if (!message || !message.trim()) return;

    try {
        const creation = await api.creerDemande(message.trim());
        etat.demandes.value = await api.listerDemandes();
        etat.demandeActive.value = creation.demande;
        etat.historiqueDialogue.value = [
            { role: 'agent', texte: message.trim() },
            { role: 'assistant', texte: creation.dialogue.message }
        ];
        vueVide.hidden = true;
        vueDetail.hidden = false;
        vueValidation.hidden = true;
        remplirFiche(creation.dialogue.ficheProposee || creation.demande.ficheCourante);
        afficherPoints(creation.dialogue.pointsAPreciser);
        afficherStatut(creation.demande);
        gererBoutons(creation.demande);
    } catch (err) {
        afficherErreur(err.message);
    }
});

// --- Enregistrement de la fiche (édition manuelle) ---
formFiche.addEventListener('submit', async (e) => {
    e.preventDefault();
    const demande = etat.demandeActive.value;
    if (!demande) return;

    const fiche = {};
    for (const champ of [
        'applicationOuService', 'intituleCourt', 'synthese', 'situationActuelle',
        'difficultesRencontrees', 'resultatRecherche', 'criteresDeReussite',
        'solutionEnvisagee', 'utilisateursConcernes', 'processusMetierConcerne',
        'beneficesAttendus', 'echeanceSouhaitee', 'contraintes', 'piecesComplementaires'
    ]) {
        fiche[champ] = formFiche.elements[champ].value || null;
    }

    try {
        const nouvelleFiche = await api.editerFiche(demande.id, fiche);
        messageFiche.textContent = `Fiche enregistrée (version ${nouvelleFiche.numeroVersion}, ${nouvelleFiche.source}).`;
        messageFiche.hidden = false;
        setTimeout(() => { messageFiche.hidden = true; }, 4000);
        etat.demandeActive.value = await api.consulterDemande(demande.id);
        etat.demandes.value = await api.listerDemandes();
    } catch (err) {
        afficherErreur(err.message);
    }
});

// --- Soumission ---
$('#btn-soumettre').addEventListener('click', async () => {
    const demande = etat.demandeActive.value;
    if (!demande) return;

    const confirme = confirm(
        `Soumettre la demande ${demande.numero} pour validation ?\n` +
        'Cette action est définitive : la fiche sera transmise à votre chef de service.'
    );
    if (!confirme) return;

    try {
        const miseAJour = await api.soumettre(demande.id);
        etat.demandeActive.value = miseAJour;
        etat.demandes.value = await api.listerDemandes();
        afficherStatut(miseAJour);
        gererBoutons(miseAJour);
        if (profilEst('ChefService')) {
            etat.fileValidation.value = await api.fileEnAttente();
        }
    } catch (err) {
        afficherErreur(err.message);
    }
});

// --- Export de la fiche (Word / PDF) ---
async function exporterFiche(format) {
    const demande = etat.demandeActive.value;
    if (!demande) return;

    try {
        const blob = await api.exporter(demande.id, format);
        const url = URL.createObjectURL(blob);
        const lien = document.createElement('a');
        lien.href = url;
        lien.download = `fiche-${demande.numero}.${format}`;
        document.body.appendChild(lien);
        lien.click();
        lien.remove();
        URL.revokeObjectURL(url);
    } catch (err) {
        afficherErreur(err.message);
    }
}

$('#btn-export-word').addEventListener('click', () => exporterFiche('docx'));
$('#btn-export-pdf').addEventListener('click', () => exporterFiche('pdf'));

// --- Dialogue IA ---
formDialogue.addEventListener('submit', async (e) => {
    e.preventDefault();
    const demande = etat.demandeActive.value;
    const message = $('#message-dialogue').value.trim();
    if (!demande || !message) return;

    etat.historiqueDialogue.value = [...etat.historiqueDialogue.value, { role: 'agent', texte: message }];
    $('#message-dialogue').value = '';

    try {
        const reponse = await api.envoyerMessage(demande.id, message);
        etat.historiqueDialogue.value = [
            ...etat.historiqueDialogue.value,
            { role: 'assistant', texte: reponse.message }
        ];
        if (reponse.ficheProposee) {
            remplirFiche(reponse.ficheProposee);
        }
        afficherPoints(reponse.pointsAPreciser);
    } catch (err) {
        afficherErreur(err.message);
    }
});

// Rendu réactif du dialogue
effect(() => {
    const messages = etat.historiqueDialogue.value;
    historiqueDialogue.innerHTML = '';
    for (const m of messages) {
        const div = document.createElement('div');
        div.className = 'message ' + m.role;
        div.textContent = m.texte;
        historiqueDialogue.appendChild(div);
    }
    historiqueDialogue.scrollTop = historiqueDialogue.scrollHeight;
});

// --- File de validation (chef de service) ---
effect(() => {
    const file = etat.fileValidation.value;
    listeValidation.innerHTML = '';
    if (!profilEst('ChefService')) {
        vueValidation.hidden = true; // évite d'afficher la file d'une session précédente
        return;
    }

    if (!file.length) {
        listeValidation.innerHTML = '<p class="vide">Aucune demande en attente.</p>';
        return;
    }

    for (const d of file) {
        const item = document.createElement('div');
        item.className = 'item-validation';
        item.innerHTML = `
      <div class="numero">${d.numero}</div>
      <div class="synthese">${d.synthese || d.intituleCourt || 'Sans synthèse'}</div>
      <div class="meta">${d.demandeur} · ${d.bureau} · soumise le ${formaterDate(d.dateSoumission)}</div>
      <div class="decisions">
        <button class="valider" data-action="Valider">Valider</button>
        <button class="completer" data-action="DemanderComplements">Demander des compléments</button>
        <button class="refuser" data-action="Refuser">Refuser</button>
      </div>
      <div class="motif" hidden>
        <input type="text" placeholder="Motif (obligatoire)">
        <button class="confirmer">Confirmer</button>
      </div>
    `;

        const boutons = item.querySelectorAll('.decisions button');
        const motif = item.querySelector('.motif');
        const champMotif = motif.querySelector('input');
        const confirmer = motif.querySelector('.confirmer');

        boutons.forEach((b) => {
            b.addEventListener('click', () => {
                const action = b.dataset.action;
                if (action === 'Valider') {
                    decider(d.id, action, null);
                } else {
                    motif.hidden = false;
                    champMotif.focus();
                    confirmer.onclick = () => {
                        const commentaire = champMotif.value.trim();
                        if (!commentaire) {
                            alert('Le motif est obligatoire pour cette décision.');
                            return;
                        }
                        decider(d.id, action, commentaire);
                    };
                }
            });
        });

        listeValidation.appendChild(item);
    }
});

async function decider(demandeId, decision, commentaire) {
    try {
        await api.decider(demandeId, decision, commentaire);
        etat.fileValidation.value = await api.fileEnAttente();
        etat.demandes.value = await api.listerDemandes();
    } catch (err) {
        afficherErreur(err.message);
    }
}

// --- Onglet validation (chef de service) ---
const boutonValidation = document.createElement('button');
boutonValidation.textContent = 'File de validation';
boutonValidation.className = 'bouton-secondaire';
boutonValidation.style.marginTop = '0.75rem';
boutonValidation.addEventListener('click', () => {
    vueVide.hidden = true;
    vueDetail.hidden = true;
    vueValidation.hidden = false;
    etat.fileValidation.value = etat.fileValidation.value; // force le rendu
});
$('#btn-nouvelle').after(boutonValidation);

// --- Initialisation ---
(function initialiser() {
    const agentStocke = localStorage.getItem('agent');
    if (agentStocke) {
        try {
            etat.agent.value = JSON.parse(agentStocke);
            basculerVues();
            chargerDonnees();
        } catch {
            localStorage.removeItem('agent');
            basculerVues();
        }
    } else {
        basculerVues();
    }
})();