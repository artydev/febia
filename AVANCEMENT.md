# Avancement du projet — Démonstrateur « Guichet intelligent » SICCRF

> Suivi de l'évolution du projet. Dernière mise à jour : voir bas de page.
> Sources : `docs/Expression_de_besoin_demonstrateur_IA_SICCRF(1).md`, `docs/FEB.md`, `docs/PROMPT_LLM_WebAPI.md`.

---

## 1. Objectif

Développer un **démonstrateur** de guichet intelligent des demandes d'évolution du SI (DGCCRF/SICCRF) pour le séminaire « L'innovation maîtrisée » de janvier 2027 : WebAPI ASP.NET Core + frontend vanilla JS, sur données fictives, illustrant l'apport combiné de l'IA et de l'automatisation déterministe.

## 2. Décisions actées

| Sujet | Décision |
|---|---|
| Version .NET | .NET 10 |
| Base de données | SQLite |
| Mode IA | Simulé + réel (OpenAI / Azure OpenAI), simulé par défaut |
| Courriels | Log simulé (pas de SMTP) |
| Périmètre fonctionnel | Indispensables uniquement (+ export Word/PDF optionnel) |
| Langue du code | Français |
| Namespace | `Siccrf.Guichet` |
| Découpage | Prompt principal + prompts par incrément |
| Framework de tests | Au choix de l'LLM (xUnit, NUnit, MSTest…) |
| Export Word/PDF | Ajouté en optionnel |
| Frontend | JavaScript vanilla, sans framework ; store + signaux (signals) si nécessaire |

## 3. Plan d'incréments

| # | Incrément | Contenu | Statut |
|---|---|---|---|
| 1 | Socle | Projet WebAPI, modèle de données, DbContext, seed de données fictives, configuration (IA simulée/réelle, notifications en log) | ✅ Terminé |
| 2 | Brouillon et dialogue | Création de demande, dialogue IA, génération et édition de la fiche (versions, non-écrasement des modifications manuelles) | ✅ Terminé |
| 3 | Circuit de validation | Soumission, notification, file du chef de service, décisions, changements de statut, traçabilité | ✅ Terminé |
| 4 | Habilitations | Contrôle d'accès par profil sur tous les endpoints, vérification côté serveur | ✅ Terminé |
| 5 | Frontend (vanilla JS) | Interface de démonstration en JavaScript vanilla, sans framework ; store et signaux si nécessaire ; connexion à l'API | ✅ Terminé |
| 6 | Export (optionnel) | Export Word/PDF de la fiche | ⏳ À faire |

Chaque incrément doit se terminer par un build et des tests verts avant de passer au suivant.

## 4. Historique

| Date | Action | Détail |
|---|---|---|
| — | Initialisation du dépôt Git | `git init` dans `C:/temp/feb` |
| — | Lecture des documents sources | `docs/Expression_de_besoin_demonstrateur_IA_SICCRF(1).md`, `docs/FEB.md` |
| — | Rédaction du prompt LLM | `docs/PROMPT_LLM_WebAPI.md` (v0.1) |
| — | Décisions utilisateur | Q1–Q10 tranchées (voir tableau §2) |
| — | Mise à jour du prompt | v0.2 : décisions intégrées, Bloc 9 (incréments), tableau des décisions |
| — | Contrainte frontend | Vanilla JS sans framework, store + signaux si nécessaire (intégrée au prompt) |
| — | Création de ce fichier | Suivi de l'avancement |
| — | Incrément 1 — Socle | Solution `Siccrf.Guichet.slnx`, projets API + tests, EF Core SQLite 10.0.0, modèle de données (9 entités + enums), DbContext, seed en phases (scénario e-commerce), services IA (simulé + OpenAI) et notifications en log, endpoints `/` et `/api/sante` ; build OK, 3 tests verts, smoke test HTTP 200 |
| — | Incrément 2 — Brouillon et dialogue | DTOs + mappeurs (`Contrats/`), `DemandeService` (création, dialogue, édition versionnée, acceptation de proposition, non-écrasement des modifications manuelles), `DemandesController` (POST /api/demandes, POST /{id}/dialogue, PUT /{id}/fiche, POST /{id}/accepter-proposition, GET /{id}), enregistrement des contrôleurs ; build OK, 8 tests verts (3 socle + 5 intégration) |
| — | Incrément 3 — Circuit de validation | `Courriel` ajouté aux agents, DTOs validation, `ValidationService` (soumission avec confirmation explicite + détermination du valideur via rattachement + notification + traçabilité, file du chef de service, décisions valider/à compléter/refuser avec motif obligatoire), `ValidationsController` (POST /{id}/soumettre, GET /validations/en-attente, POST /validations/{id}/decision), resoumission autorisée depuis ACompleter ; build OK, 16 tests verts (3 socle + 5 dialogue + 8 validation) |
| — | Incrément 4 — Habilitations | Authentification JWT simulée (package JwtBearer 10.0.0), `MotDePasseHash` PBKDF2 + mots de passe seedés (`demo`), `AuthService` + `AuthController` (POST /api/auth/login → JWT avec rôle), `[Authorize]` sur tous les endpoints, identité depuis le JWT (fini le X-Agent-Id), `GET /api/demandes` (liste selon profil : agent → siennes, chef de service → périmètre, XA/SICCRF → toutes), consultation adaptée par profil ; build OK, 23 tests verts (3 socle + 5 dialogue + 8 validation + 7 habilitations) |
| — | Incrément 5 — Frontend (vanilla JS) | `wwwroot/` : `index.html` (connexion, liste, détail, fiche éditable, dialogue, file de validation), `styles.css`, `js/store.js` (signals : signal/effect/derive), `js/api.js` (client HTTP + JWT), `js/app.js` (logique applicative réactive) ; `UseDefaultFiles()` + `UseStaticFiles()` dans Program.cs, suppression de l'endpoint `MapGet("/")` (capturait la racine) ; build OK, 23 tests verts, smoke test : `/` sert index.html (200, 5932 octets), CSS/JS 200 |

## 5. Prochaines étapes

- [x] Relire et valider `docs/PROMPT_LLM_WebAPI.md` (v0.2)
- [x] Lancer l'incrément 1 — Socle (projet WebAPI, modèle de données, DbContext, seed, configuration)
- [x] Incrément 2 — Brouillon et dialogue (création de demande, dialogue IA, fiche éditable, versions, non-écrasement)
- [x] Incrément 3 — Circuit de validation (soumission, notification, file du chef de service, décisions, statuts, traçabilité)
- [x] Incrément 4 — Habilitations (contrôle d'accès par profil sur tous les endpoints, vérification côté serveur)
- [x] Incrément 5 — Frontend (vanilla JS, store + signaux, connexion à l'API)
- [ ] Incrément 6 — Export (optionnel)

---

*Dernière mise à jour : création du fichier.*
