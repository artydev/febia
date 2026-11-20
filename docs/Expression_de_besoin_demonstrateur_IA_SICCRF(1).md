# Expression de besoin – Démonstrateur de guichet intelligent des demandes d’évolutions du SI

DGCCRF / SICCRF | Document de cadrage pour réunion avec les équipes internes | Version de travail – novembre 2026

# 1\. Finalité et contexte

Le SICCRF souhaite réaliser, pour le séminaire « L’innovation maîtrisée » de janvier 2027, un démonstrateur illustrant l’apport combiné de l’intelligence artificielle et de l’automatisation dans le recueil, la qualification, la validation et l’instruction des demandes d’évolution du système d’information de la DGCCRF.

Le dispositif répond à quatre difficultés : demandes reçues par des canaux multiples ; formulations insuffisamment précises ; absence de validation hiérarchique clairement identifiable ; visibilité insuffisante de la direction générale sur les demandes et leur devenir.

Le démonstrateur doit montrer un parcours simple pour l’agent, mais encadré par un processus de gouvernance explicite, traçable et sécurisé.

# 2\. Périmètre

- Le guichet est ouvert à tous les agents de la DGCCRF pour exprimer des demandes d’évolution : nouvelles fonctionnalités, améliorations d’applications, nouveaux services numériques, automatisations de tâches ou évolutions techniques.
- Les incidents, demandes d’assistance, d’équipement courant et d’habilitation individuelle sont hors périmètre. L’assistant peut les reconnaître et orienter l’agent vers le dispositif approprié, sans créer une demande d’évolution.
- Le démonstrateur utilise des identités, demandes et données fictives ; il ne réalise aucun arbitrage réel ni aucune modification du SI de production.

# 3\. Parcours utilisateur attendu

## 3.1 Dépôt et qualification par l’agent

- L’agent accède à un portail unique et décrit librement son besoin dans un dialogue conversationnel.
- L’IA pose des questions ciblées pour clarifier le problème actuel, les objectifs, les bénéficiaires, les usages, les bénéfices attendus, les contraintes et les échéances justifiées.
- L’IA distingue le besoin métier de la solution suggérée ; elle ne complète pas par invention les informations absentes et signale les points restant à préciser.
- Le système prépare une fiche structurée à partir du modèle de fiche d’expression de besoin du SICCRF, en particulier ses rubriques de contexte, objectifs, bénéfices, bénéficiaires, acteurs, évolutions, impacts, risques et calendrier.

## 3.2 Relecture et confirmation

- L’interface présente idéalement le dialogue à gauche et la fiche éditable à droite.
- L’agent peut demander une correction dans le dialogue ou modifier directement les champs de la fiche.
- Les changements proposés par l’IA sont visibles et soumis à acceptation ; les modifications manuelles ne doivent pas être écrasées.
- L’agent confirme explicitement que la fiche reflète son besoin avant de la soumettre. Les brouillons restent privés jusqu’à cette soumission.

## 3.3 Validation hiérarchique et métier

- Le responsable valideur est déterminé à partir d’un référentiel organisationnel fiable ; pour le démonstrateur, une table de correspondance fictive agent–service–responsable suffit.
- La soumission déclenche un courriel automatique au chef de service, contenant un lien sécurisé vers la demande.
- Le chef de service accède au portail et choisit : valider, demander des compléments ou refuser (avec motivation pour les deux derniers cas).
- Chaque décision est associée à son auteur, sa date, son commentaire et un changement de statut ; le demandeur est informé.

# 4\. Instruction et gouvernance

Après validation métier, la demande est préqualifiée et orientée selon ses impacts. L’IA peut suggérer une catégorie et les expertises utiles ; le circuit effectif résulte de règles explicites et d’une confirmation humaine.

| Acteur | Responsabilité attendue |
| --- | --- |
| Bureau XA | Pour le compte de la direction générale : alignement avec le plan stratégique, cohérence métier, détection et proposition de regroupement des demandes similaires ou complémentaires. |
| SICCRF | Faisabilité, options techniques, architecture, charges, coûts, ressources, calendrier et coordination des expertises.<br>Points d’attention pour préserver la cohérence avec les autres briques du SI |
| RSSI (sollicité par le SICCRF) | Risques et exigences de sécurité, protections et besoins éventuels d’homologation. |
| Mission Data/IA (sollicitée par le SICCRF) | Enjeux relatifs aux données, à leur sensibilité, aux usages de l’IA et au RGPD ; association du DPD si nécessaire. |
| CODIR et/ou COPIL SI | Synthèse, priorisation et arbitrages selon la nature des sujets et les délégations établies. |

Le bureau XA et le SICCRF interviennent en parallèle lorsque les deux analyses sont nécessaires. Si une demande est purement technique, sans impact métier ni changement de processus identifié, le SICCRF conduit l’instruction et le bureau XA conserve une visibilité sans avis détaillé systématique.

Les demandes présentant des impacts métier, des changements de processus ou des enjeux transverses sont instruites conjointement par XA et le SICCRF. La qualification peut être révisée si de nouveaux impacts apparaissent.

# 5\. Fonctionnalités du démonstrateur

| Priorité | Fonctionnalité | Critère de démonstration |
| --- | --- | --- |
| Indispensable | Dialogue IA et questions complémentaires | Une demande initiale vague devient une fiche exploitable sans invention d’informations. |
| Indispensable | Fiche structurée éditable | Corrections par tchat et saisie directe, confirmation explicite. |
| Indispensable | Circuit de validation simulé | Notification, accès du responsable, décision et mise à jour du statut. |
| Indispensable | Habilitations par profil | Agent : ses demandes ; responsable : demandes soumises à son périmètre ; vues XA/SICCRF adaptées. |
| Souhaitable | Préqualification et routage | Distinction demande technique / impact métier ; proposition d’avis RSSI et Data/IA. |
| Souhaitable | Rapprochement sémantique | L’IA suggère des demandes similaires ; XA décide du regroupement. |
| Souhaitable | Tableau de bord | Suivi des statuts, demandes en attente et vision consolidée des dossiers. |
| Optionnel | Synthèse pour arbitrage | Préparation d’un dossier CODIR / COPIL SI avec avis distincts et points ouverts. |

# 6\. Exigences transverses

- Sécurité : authentification, contrôle d’accès côté serveur et vérification des droits à chaque consultation ; un lien de courriel ne vaut pas autorisation.
- Confidentialité : un agent ne voit que ses demandes ; un chef de service ne voit que les demandes qui lui sont soumises ; les brouillons non soumis ne sont pas visibles par la hiérarchie.
- Traçabilité : changements de statut, notifications et décisions ; conservation des avis XA, SICCRF et experts sans les fusionner de façon opaque.
- Maîtrise de l’IA : propositions explicables, validation humaine des reformulations, regroupements et orientations ; aucun avis SSI, RGPD ou arbitrage automatique.
- Interopérabilité : possibilité d’exporter la fiche structurée (Word ou PDF) et de réutiliser les données dans les outils de suivi existants.

# 7\. Scénario proposé pour la démonstration au séminaire

Cas fictif : un agent demande « La DGCCRF doit désormais contrôler les plates-formes e-commerce et réaliser des prélèvements à distance. L’objectif est de faire évoluer les pratiques, processus et l’outiller ». L’assistant précise les usages et génère la fiche ; l’agent corrige une rubrique, confirme et transmet. Le chef de service reçoit une notification et valide. Le système qualifie l’impact métier, ouvre l’instruction parallèle XA/SICCRF et suggère un rapprochement avec une autre demande de pilotage. Le bureau XA accepte ou écarte ce rapprochement ; le SICCRF identifie les expertises utiles. Un tableau de bord présente l’état du dossier et la préparation de l’arbitrage.

# 8\. Livrables attendus des équipes et décisions à prendre

- Une maquette navigable et une démonstration de bout en bout sur données fictives, avec un scénario nominal et un retour pour complément.
- Un modèle de données minimal : agents, rattachements, responsables, demandes, versions, validations, avis, statuts et notifications.
- Une proposition d’architecture : interface, service IA, référentiel de demandes, moteur de workflow, notifications et contrôle d’accès.
- Une estimation des efforts, des dépendances et des risques techniques pour un prototype de séminaire, puis pour une éventuelle industrialisation.
- Les arbitrages à instruire en réunion : socle technique (Grist et service IA ou application web), mode d’identification des responsables, niveau de réalisme des courriels, périmètre des tableaux de bord et règles exactes de saisine CODIR / COPIL SI.

Point d’attention : la démonstration doit distinguer clairement ce qui relève de l’IA (compréhension, reformulation, rapprochement, synthèse) de l’automatisation déterministe (droits, routage, statuts, notifications et décisions).