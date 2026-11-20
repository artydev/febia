# Prompt LLM — Développement d'un démonstrateur WebAPI ASP.NET Core

> Document de travail — à adapter après relecture.
> Sources : `Expression_de_besoin_demonstrateur_IA_SICCRF(1).md` et `FEB.md` (même dossier).
> Version : brouillon v0.2 — décisions intégrées

---

## Bloc 1 — Rôle et contexte

Tu es un architecte et développeur .NET senior. Tu dois développer un **démonstrateur** de « guichet intelligent » des demandes d'évolution du système d'information de la DGCCRF, pour le séminaire « L'innovation maîtrisée » de janvier 2027.

Ce projet est un **démonstrateur sur données fictives** : il ne réalise aucun arbitrage réel, aucune modification du SI de production, aucun envoi de courriel réel. Il doit illustrer l'apport combiné de l'IA (compréhension, reformulation, rapprochement, synthèse) et de l'automatisation déterministe (droits, routage, statuts, notifications, décisions).

Le périmètre couvre les demandes d'évolution : nouvelles fonctionnalités, améliorations d'applications, nouveaux services numériques, automatisations de tâches, évolutions techniques. Les incidents, demandes d'assistance, demandes d'équipement courant et d'habilitation individuelle sont **hors périmètre** : l'assistant peut les reconnaître et orienter l'agent vers le dispositif approprié, sans créer de demande d'évolution.

## Bloc 2 — Choix techniques (décidés)

Applique les choix suivants :

- **Framework** : ASP.NET Core WebAPI, **.NET 10**.
- **ORM** : Entity Framework Core.
- **Base de données** : SQLite (fichier local, seed au démarrage) — simple pour un démonstrateur.
- **Authentification** : JWT simulée avec rôles (`Agent`, `ChefService`, `BureauXA`, `SICCRF`), utilisateurs de démonstration seedés. Pas de vrai fournisseur d'identité.
- **IA** : interface abstraite `IAssistantService` avec **deux implémentations** :
  1. `AssistantSimule` : réponses codées en dur, permet de démontrer sans clé API ;
  2. `AssistantOpenAI` (ou Azure OpenAI) : appel réel, activé par configuration (`appsettings`), jamais requis pour la démo.
- **Courriels** : `INotificationService` avec implémentation **log simulé** (écrit dans la console / un fichier) ; pas de SMTP réel.
- **Namespace racine** : `Siccrf.Guichet`.
- **Frontend** : JavaScript **vanilla**, sans framework ; si nécessaire, implémenter un **store** et utiliser des **signaux** (signals) pour la réactivité.
- **Export Word/PDF** : optionnel, ajouté à la demande (voir Bloc 3).

## Bloc 3 — Périmètre fonctionnel

Implémente **uniquement** les fonctionnalités **indispensables** ci-dessous. Les fonctionnalités souhaitables et la synthèse pour arbitrage sont **hors périmètre** pour ce démonstrateur. L'export Word/PDF est ajouté en **optionnel** (voir plus bas).

### Indispensables

1. **Dialogue IA et questions complémentaires** : l'agent décrit librement son besoin ; l'IA pose des questions ciblées (problème actuel, objectifs, bénéficiaires, usages, bénéfices attendus, contraintes, échéances justifiées). Une demande initiale vague doit devenir une fiche exploitable **sans invention d'informations** : l'IA distingue le besoin métier de la solution suggérée, ne complète pas par invention les informations absentes et signale les points restant à préciser.
2. **Fiche structurée éditable** : génération d'une fiche conforme au modèle SICCRF (`FEB.md`) : en-tête (n° de demande, demandeur, date, bureau, valideur) et rubriques contexte, objectifs, bénéfices, bénéficiaires, acteurs, évolutions, impacts, risques, calendrier. Corrections possibles par le dialogue **et** par saisie directe dans les champs. Les changements proposés par l'IA sont visibles et soumis à acceptation ; **les modifications manuelles ne doivent jamais être écrasées**. Confirmation explicite du demandeur avant soumission. Les brouillons restent privés jusqu'à la soumission.
3. **Circuit de validation simulé** : à la soumission, détermination du responsable valideur à partir d'une table de correspondance fictive agent–service–responsable ; notification (log simulé) au chef de service ; le chef de service consulte et choisit : **valider / demander des compléments / refuser** (motivation obligatoire pour les deux derniers). Chaque décision est enregistrée avec auteur, date, commentaire et changement de statut ; le demandeur est informé.
4. **Habilitations par profil** :
   - `Agent` : voit uniquement ses propres demandes ;
   - `ChefService` : voit uniquement les demandes soumises à son périmètre ;
   - `BureauXA` / `SICCRF` : vues adaptées (toutes les demandes, avec les informations pertinentes pour leur rôle).
   - Contrôle d'accès **côté serveur**, vérifié à chaque consultation : un lien de courriel ne vaut pas autorisation.

### Hors périmètre — pour information (ne pas implémenter)

5. **Préqualification et routage** : distinction demande technique / impact métier ; suggestion d'une catégorie et des expertises utiles (RSSI, Mission Data/IA) ; le circuit effectif résulte de règles explicites et d'une confirmation humaine.
6. **Rapprochement sémantique** : l'IA suggère des demandes similaires ; le bureau XA décide du regroupement.
7. **Tableau de bord** : suivi des statuts, demandes en attente, vision consolidée des dossiers.
8. **Synthèse pour arbitrage** : préparation d'un dossier CODIR / COPIL SI avec avis distincts et points ouverts.

### Optionnel (ajouté à la demande)

9. **Export de la fiche (Word ou PDF)** : export de la fiche structurée conforme au modèle SICCRF, pour interopérabilité (réutilisation dans les outils de suivi existants). À implémenter seulement si le temps le permet.

## Bloc 4 — Modèle de données minimal

Crée les entités suivantes (noms à adapter à tes conventions) :

- `Agent` : identifiant, nom, prénom, bureau, profil/rôle.
- `Rattachement` : table de correspondance fictive agent → service → responsable valideur.
- `Responsable` : identité du cadre valideur, service.
- `Demande` : en-tête (n°, demandeur, date, bureau, valideur, statut courant, catégorie, version courante).
- `VersionFiche` : contenu de la fiche (rubriques du modèle SICCRF), versionnée ; chaque modification (IA acceptée ou manuelle) crée une nouvelle version.
- `Validation` : décision (validée / à compléter / refusée), auteur, date, commentaire, statut avant/après.
- `Avis` : avis distincts (Bureau XA, SICCRF, RSSI, Data/IA) — jamais fusionnés de façon opaque.
- `Statut` : historique des changements de statut (traçabilité).
- `Notification` : destinataire, type, date, contenu (log simulé).

Ajoute un **seed de données fictives** conforme au scénario de démonstration : un agent demande « La DGCCRF doit désormais contrôler les plates-formes e-commerce et réaliser des prélèvements à distance. L'objectif est de faire évoluer les pratiques, processus et l'outiller » ; un chef de service ; une demande similaire de « pilotage » pour illustrer le rapprochement.

## Bloc 5 — Contrat d'API

Expose au minimum les endpoints suivants (REST, JSON) :

```
POST   /api/demandes                     → créer un brouillon (début de dialogue)
POST   /api/demandes/{id}/dialogue       → envoyer un message, recevoir la fiche mise à jour + points à préciser
GET    /api/demandes/{id}                → consulter (droits : propriétaire, valideur, XA/SICCRF)
PUT    /api/demandes/{id}/fiche          → édition manuelle de la fiche (nouvelle version)
POST   /api/demandes/{id}/soumettre      → confirmation explicite + notification au valideur
GET    /api/validations/en-attente       → file du chef de service
POST   /api/validations/{id}/decision    → valider / à compléter / refuser (+ motif)
GET    /api/demandes                     → liste selon profil (agent : siennes ; XA/SICCRF : toutes)
POST   /api/demandes/{id}/prequalifier   → suggestion catégorie + expertises (IA, confirmée par humain) — hors périmètre
GET    /api/demandes/{id}/similaires     → rapprochement sémantique (IA) — hors périmètre
GET    /api/tableau-de-bord              → statuts, en attente, vision consolidée — hors périmètre
GET    /api/demandes/{id}/export         → export Word/PDF de la fiche — optionnel
```

Chaque endpoint vérifie les droits côté serveur avant de répondre.

## Bloc 6 — Règles métier transverses (non négociables)

- **Confidentialité** : un agent ne voit que ses demandes ; un chef de service ne voit que les demandes qui lui sont soumises ; les brouillons non soumis ne sont pas visibles par la hiérarchie.
- **Traçabilité** : tout changement de statut, notification et décision est enregistré avec auteur et date ; les avis XA, SICCRF et experts sont conservés distinctement.
- **Maîtrise de l'IA** : propositions explicables ; validation humaine des reformulations, regroupements et orientations ; **aucun avis SSI, RGPD ou arbitrage automatique** — l'IA propose, l'humain décide.
- **Non-écrasement** : les modifications manuelles de la fiche ne sont jamais écrasées par les propositions de l'IA.
- **Déterminisme** : droits, routage, statuts, notifications et décisions relèvent de l'automatisation déterministe, pas de l'IA.

## Bloc 7 — Livrables et critères d'acceptation

1. Projet WebAPI compilable (`dotnet build` sans erreur) et lançable (`dotnet run`).
2. Seed de données fictives au démarrage (scénario du Bloc 4 jouable immédiatement).
3. Tests automatisés couvrant au minimum : règles de statuts (soumission, validation, refus, complément), contrôle d'accès par profil, non-écrasement des modifications manuelles. Framework de test au choix de l'LLM (xUnit, NUnit, MSTest…).
4. README : démarrage, configuration (clé IA optionnelle), scénario de démonstration nominal + scénario « retour pour complément ».
5. Aucune donnée réelle, aucun secret en dur (clé IA uniquement via configuration utilisateur).
6. Code, identifiants et commentaires en **français**, cohérents sur tout le projet.

## Bloc 8 — Format de réponse attendu

- Liste les fichiers créés avec leur rôle.
- Explique brièvement l'architecture (couches, dépendances, flux du scénario de démo).
- Indique les commandes à exécuter pour builder, tester et lancer.
- Signale les points restés ouverts ou les choix que tu as dû faire par défaut.

---

## Bloc 9 — Découpage en prompts incrémentaux

Le travail est découpé en **prompts successifs**, chacun s'appuyant sur le précédent. Le présent document sert de cadre commun (Blocs 1 à 8) ; chaque incrément précise sa cible :

1. **Incrément 1 — Socle** : projet WebAPI, modèle de données, DbContext, seed de données fictives, configuration (IA simulée/réelle, notifications en log).
2. **Incrément 2 — Brouillon et dialogue** : création de demande, dialogue IA, génération et édition de la fiche (versions, non-écrasement des modifications manuelles).
3. **Incrément 3 — Circuit de validation** : soumission, notification, file du chef de service, décisions, changements de statut, traçabilité.
4. **Incrément 4 — Habilitations** : contrôle d'accès par profil sur tous les endpoints, vérification côté serveur.
5. **Incrément 5 — Frontend (vanilla JS)** : interface de démonstration en JavaScript vanilla, sans framework ; store et signaux (signals) si nécessaire ; connexion à l'API.
6. **Incrément 6 — Export (optionnel)** : export Word/PDF de la fiche.

Chaque incrément doit se terminer par un build et des tests verts avant de passer au suivant.

---

## Décisions actées (intégrées au prompt)

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
| Frontend | JavaScript vanilla, sans framework ; store + signaux si nécessaire |
