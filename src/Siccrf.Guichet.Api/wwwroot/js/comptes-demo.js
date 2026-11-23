/**
 * Aides de l'écran de connexion (démonstrateur) :
 * - un clic sur un compte de démonstration pré-remplit le formulaire ;
 * - bouton « Afficher / Masquer » pour le mot de passe.
 */

const champIdentifiant = document.querySelector('#identifiant');
const champMotDePasse = document.querySelector('#mot-de-passe');
const boutonAfficher = document.querySelector('#afficher-mdp');

document.querySelectorAll('[data-compte]').forEach((bouton) => {
    bouton.addEventListener('click', () => {
        champIdentifiant.value = bouton.dataset.compte;
        champMotDePasse.value = 'demo';
        document.querySelector('#form-connexion button[type="submit"]').focus();
    });
});

boutonAfficher?.addEventListener('click', () => {
    const visible = champMotDePasse.type === 'text';
    champMotDePasse.type = visible ? 'password' : 'text';
    boutonAfficher.textContent = visible ? 'Afficher' : 'Masquer';
    boutonAfficher.setAttribute('aria-pressed', String(!visible));
});