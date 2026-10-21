# Patch — Awareness Lab

- Replace automatic recurring attacks with one-shot `ActivityGate` startup.
- Create fictitious workspaces and coin files inside a unique LocalAppData session.
- Remove real Desktop/Documents scans, bulk recursive removal and registry file associations.
- Remove global keyboard hook, system cursor changes, arbitrary commands and forced shutdown settings.
- Provide in-app coin browser (double-click) and validated drag-and-drop coin redemption.
- Show success/failure message (editable JSON) and a mandatory explicit simulation disclosure.
- Backup and restore wallpaper with crash-recovery best effort.
- Add Ctrl+Shift+F12 emergency exit and tray menu Quit.
- Preserve original art, sound assets, LICENSE.


## v2 — affichage final et dossiers réels (opt-in)

- Correctif : l'écran de fin devient la fenêtre principale WPF et reste visible jusqu'à fermeture manuelle. L'overlay est masqué au lieu d'être fermé pendant le transfert.
- Nouveau paramètre `CoinPlacement` (`Simulation` par défaut, `RealUserFolders` sur demande).
- En mode réel, images PNG non exécutables réparties à la racine des dossiers utilisateur autorisés, sans extension shell ou privilège élevé.
- Les pièces réelles sont suivies dans le manifeste sous LocalAppData et validées par empreinte SHA-256 avant tout effacement.
- La réouverture après crash réessaie seulement le nettoyage validé ; les dossiers réels restent intacts.
- Les deux modes conservent le paiement par double-clic dans l'explorateur intégré ; le mode réel autorise aussi le glisser-déposer depuis Windows.

## v3 — fenêtre finale persistante et recherche libre en mode réel

- Écran final sans barre système de fermeture ; fermeture volontaire uniquement par le bouton « Terminer et quitter ». Toute demande de fermeture non autorisée est annulée.
- Fenêtre de résultat déplaçable par l’en-tête, messages longs consultables par défilement, et aucun timer de fermeture.
- `RealUserFolders` : plus d’ouverture automatique du `CoinBrowser` ni de bouton permettant d’afficher un index de toutes les pièces. Le participant recherche lui-même les fichiers PNG dans l’Explorateur Windows et les glisse vers la fenêtre de rançon.
- `Simulation` conserve exactement l’explorateur intégré et le double-clic.
- Aucune modification du gestionnaire de pièces, des chemins autorisés, du manifeste SHA-256 ni du nettoyage de la v2.

## v4 — correction fermeture inattendue de l'écran final

- Correction de la course entre la fin du jeu et `NAudio.WaveOut.PlaybackStopped`. Le callback ne réutilise plus de `WaveFileReader` détruit lors du nettoyage audio.
- Synchronisation des actions audio, arrêt idempotent et désabonnement de l'événement *avant* la libération des ressources ; les exceptions de périphérique audio sont enregistrées sans faire tomber l'application.
- Journal local minimal dans `%LOCALAPPDATA%\Rans0mAwarenessLab\lab-events.log` (entrée, sortie, transitions, exceptions), rotation au-delà de 1 Mo ; aucune touche de clavier n'est enregistrée.
- Aucun changement de la génération et du nettoyage des pièces, du mode RealUserFolders, ni du comportement manuel de l'écran final.
- Le diagnostic du problème reste une hypothèse tant qu'une sortie de journal/Windows n'a pas confirmé la cause exacte.
