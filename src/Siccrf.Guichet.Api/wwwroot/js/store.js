/**
 * Store minimal à signaux (signals) en JavaScript vanilla, sans framework.
 *
 * Usage :
 *   const compteur = signal(0);
 *   effect(() => console.log(compteur.value));   // réagit aux changements
 *   compteur.value = 1;                          // déclenche les effets
 *
 * Un signal est un objet { value } dont la lecture enregistre la dépendance
 * et dont l'écriture notifie les effets abonnés.
 */

export function signal(valeurInitiale) {
  let valeur = valeurInitiale;
  const abonnes = new Set();

  const signalObj = {
    get value() {
      // Enregistrement de la dépendance si un effet est en cours d'exécution
      if (effetCourant) {
        abonnes.add(effetCourant);
        effetCourant.dependances.add(abonnes);
      }
      return valeur;
    },
    set value(nouvelleValeur) {
      if (Object.is(valeur, nouvelleValeur)) return;
      valeur = nouvelleValeur;
      // Notifie une copie des abonnés (les effets peuvent se désabonner)
      for (const effet of [...abonnes]) {
        effet.executer();
      }
    },
    // API interne : abonnement direct (utilisé par effect)
    _abonner(fn) {
      abonnes.add(fn);
      return () => abonnes.delete(fn);
    }
  };

  return signalObj;
}

let effetCourant = null;

/**
 * Exécute fn immédiatement, puis à chaque changement des signaux lus.
 * Retourne une fonction de nettoyage (désabonnement).
 */
export function effect(fn) {
  const effet = {
    dependances: new Set(),
    executer() {
      // Désabonne des anciennes dépendances
      for (const dep of this.dependances) {
        dep.delete(this);
      }
      this.dependances.clear();

      const precedent = effetCourant;
      effetCourant = this;
      try {
        fn();
      } finally {
        effetCourant = precedent;
      }
    }
  };

  effet.executer();
  return () => {
    for (const dep of effet.dependances) {
      dep.delete(effet);
    }
    effet.dependances.clear();
  };
}

/**
 * Signal dérivé (lecture seule) : recalcule à chaque accès et suit ses dépendances.
 */
export function derive(fn) {
  const s = signal(undefined);
  effect(() => { s.value = fn(); });
  return s;
}
