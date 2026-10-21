# RANS0M Lab — Security Awareness Edition

> Fork de démonstration de [Ixars/ransomdoors](https://github.com/Ixars/ransomdoors). Licence et crédits de l'auteur conservés dans `LICENSE.md`.
>
> **Aucun chiffrement, effacement de données personnelles, persistance, élévation de privilèges ou commande punitive.** À exécuter uniquement sur un poste de laboratoire autorisé.

## Ce qui change par rapport à l'original

- Au lancement, aucune fenêtre, son, fenêtre de configuration ni alerte. Le programme attend **3 secondes d'inactivité**, puis **une nouvelle activité clavier/souris** (via `GetLastInputInfo`, sans lire ni enregistrer les frappes).
- Déclenchement unique et non répétitif.
- Conservation des sons et des images originales, avec une courte animation d'apparition.
- Par défaut, les pièces `.gold1` à `.gold5` sont créées dans un **répertoire de session unique**, sous `%LOCALAPPDATA%\Rans0mAwarenessLab\Sessions\<identifiant>`. Avec l'option explicite `CoinPlacement: "RealUserFolders"`, de **vraies images PNG** de pièces peuvent être déposées à la racine des dossiers du profil Windows (Bureau, Documents, Téléchargements, Images, Musique, Vidéos).
- En mode `Simulation`, `CoinBrowser` affiche les faux dossiers Bureau/Documents/Téléchargements/etc. : **double-cliquer** sur une pièce permet de la récupérer. En mode `RealUserFolders`, cet index intégré ne s’ouvre plus du tout ; le participant cherche les pièces dans l’Explorateur Windows et les glisse sur la fenêtre RANS0M.
- En mode `Simulation`, aucun dossier personnel réel n'est touché. En mode `RealUserFolders`, le programme ne parcourt pas les documents personnels, n'écrit que des PNG uniques avec `FileMode.CreateNew`, ne renomme/déplace jamais les fichiers ou icônes existants, et ne supprime que ses PNG validés par leur nom aléatoire et empreinte SHA-256. Aucun dossier réel ni association de fichier n'est supprimé ou modifié.
- Le fond d'écran rouge (facultatif) est précédé d'une sauvegarde de l'ancien chemin dans `%LOCALAPPDATA%\Rans0mAwarenessLab\wallpaper-backup.json`. Restauration à la fin et tentative de récupération au prochain lancement après arrêt imprévu.
- Temps écoulé : simple texte configurable. Jamais de shutdown, de shell, de BSOD, ni de chiffrement.
- **Quitter à tout moment : `Ctrl + Maj + F12`, menu de la barre des tâches ou bouton `Quitter`.** Aucun administrateur requis dans une session standard.
- Les pièces générées sont répertoriées dans un manifeste et validées avant suppression (en mode PNG : nom + empreinte SHA-256). Le nettoyage ne supprime que les fichiers encore reconnus comme appartenant au jeu, et des dossiers vides créés par lui (pas de suppression récursive).

## Compiler (Windows 10 / 11, SDK .NET 10)

```powershell
dotnet restore .\rans0m.csproj
dotnet build .\rans0m.csproj -c Release
# Optionnel : exécutable autonome avec runtime embarqué
dotnet publish .\rans0m.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Après publication, l'exécutable et **`labsettings.json`** sont dans `bin\Release\net10.0-windows\win-x64\publish\`. Le projet dépend des packages NuGet NAudio et XamlAnimatedGif, téléchargés lors de `restore`.

## Paramètres

Modifier `labsettings.json` à côté du `.exe` :

```json
{
  "DurationSeconds": 90,
  "TargetGold": 500,
  "ChangeWallpaper": true,
  "CoinPlacement": "Simulation",
  "FailureMessage": "Vous avez laissé votre ordinateur déverrouillé. Vos fichiers ont été chiffrés et toutes vos données sont perdues.",
  "SuccessMessage": "Bravo ! Vous avez récupéré les pièces dans le temps imparti et sauvé votre ordinateur."
}
```

Les valeurs sont limitées à 15–3600 secondes et 50–5000 pièces. Le message de fin est explicitement accompagné du bandeau : **SIMULATION TERMINÉE — Aucun fichier n'a été chiffré ou supprimé**.

### Déposer les pièces dans les vrais dossiers du lab (optionnel)

Pour essayer sur une VM de laboratoire **dont les dossiers ne sont pas synchronisés** :

```json
"CoinPlacement": "RealUserFolders"
```

- Les fichiers sont des **PNG de 500 × 500 pixels** (`RANS0M-Lab-Coin-<ID>-G<valeur>.png`) afin que l'Explorateur affiche des miniatures de pièces. Ils ne sont **pas exécutables**.
- Ils apparaissent **directement** dans les dossiers utilisateur du profil courant, sans descendre dans les dossiers personnels existants. Les dossiers symboliques/jonctions, les emplacements en dehors du profil et les chemins reconnus comme OneDrive sont exclus.
- Pour les encaisser : **glisser le PNG depuis l’Explorateur Windows** vers la fenêtre de rançon. L’explorateur intégré n’apparaît plus en mode réel et le bouton d’indexation est masqué. Un double-clic sur le PNG dans l’Explorateur Windows ouvre l’application d’images, il n’encaisse pas la pièce. Aucun réglage des associations de fichiers n’est réalisé.
- Les fichiers d'origine ne sont ni modifiés ni renommés. Chaque image créée utilise `CreateNew`, un nom aléatoire et une entrée de manifeste sauvegardée **avant** écriture ; le programme supprime uniquement les images qu'il peut vérifier (nom + hash).
- En cas de coupure brutale, relancer l'application sous le **même compte Windows** récupère les pièces intactes après vérification. Si une pièce a été modifiée, elle reste en place pour éviter toute suppression abusive.
- Il ne faut pas utiliser cette option sur une session personnelle/cloud : même un PNG sans danger peut être synchronisé, indexé, ou enregistré par un antivirus ou un logiciel de sauvegarde.
- Pour revenir au comportement initial, remettre `"CoinPlacement": "Simulation"`.

### Écran de réussite ou d'échec

L’écran final **reste affiché jusqu’au clic sur « Terminer et quitter »** : sans minuterie, sans croix de fermeture ni raccourci `Échap`/`Alt+F4` actif. Les demandes de fermeture inattendues sont ignorées. Le bandeau supérieur permet de déplacer cette fenêtre. Les longs messages sont défilables. Les sons et le compte à rebours sont arrêtés avant l’affichage.

## Exécution et test

1. Lancer l'application depuis une **session Windows de test**. La fenêtre reste invisible jusqu'à une activité suivant au moins 3 secondes d'inactivité.
2. En `Simulation`, utiliser l’explorateur intégré et double-cliquer sur les pièces pour payer. En `RealUserFolders`, **aucun index intégré ne s’ouvre** : chercher soi-même les PNG dans les dossiers Windows et les glisser vers la fenêtre du jeu.
3. Vérifier le chemin du fond d'écran avant et après, y compris après `Ctrl + Maj + F12`.
4. Tester les deux issues (réussite et délai écoulé), **laisser l’écran final ouvert 30 secondes**, puis vérifier `%LOCALAPPDATA%\Rans0mAwarenessLab\Sessions` et, en mode réel, les dossiers Windows concernés.
5. Pour simuler une interruption, fermer brutalement dans une VM puis relancer ; les pièces reconnues et l'ancien fond d'écran doivent pouvoir être récupérés.

### Limitations connues

- **Pas de build Windows exécuté ici** : l'archive est une version source modifiée, à compiler et tester sur Windows. Un SDK Windows / runtime GUI n'est pas disponible dans l'environnement qui a produit ce patch.
- En mode simulation, les pièces restent dans un explorateur WPF dédié. En mode réel, les PNG apparaissent dans le Bureau et les autres dossiers autorisés, et l’encaissement s’effectue **par glisser-déposer depuis l’Explorateur Windows** ; le double-clic dans l’Explorateur Windows ne sert qu’à ouvrir l’image.
- La restauration utilise le chemin du fond d'écran Windows. Certains modes avancés (diaporama, fonds différents sur chaque moniteur, fonds dynamiques) peuvent nécessiter un contrôle manuel après la simulation.
- Après une coupure brutale, les fichiers non reconnus ou modifiés intentionnellement restent en place plutôt que d'être supprimés aveuglément.
- Une application ne peut pas être absolument invisible aux outils Windows (Gestionnaire des tâches) : elle ne cherche pas à masquer le processus.

## Git (plus tard)

L'archive est volontairement distribuée **sans `.git`**. Pour créer le dépôt avec votre identité Git locale :

```bash
git init
git config --local user.name "VOTRE_PSEUDO"
git config --local user.email "VOTRE_EMAIL"
git add .
git commit -m "feat: harmless RANS0M awareness lab"
```

Pour `gh repo create` et le push, on procèdera ensuite, à partir du poste de développement et de vos identifiants réels.

### Diagnostic si la fenêtre de résultat disparaît (v4)

La v4 corrige une possible exception de `NAudio` après arrêt du jeu. Si le problème persiste,
consulter `%LOCALAPPDATA%\Rans0mAwarenessLab\lab-events.log`. Après « Result window shown »,
une ligne « Application exiting » indique une fermeture de l'application ; « Fatal exception »
ou « WPF exception » indique une exception. Si le processus disparaît sans aucune de ces lignes,
consulter l'Observateur d'événements Windows → Journaux Windows → Application, pour une erreur
`.NET Runtime` ou `Application Error`. Attention : une fermeture brutale peut empêcher la dernière ligne
du journal d'être écrite. Aucun événement clavier ni contenu de fichier utilisateur n'est enregistré.

**Test conseillé** : gagner puis perdre une partie, laisser chaque écran de fin ouvert au moins
une minute, changer de fenêtre et bouger la souris, puis fermer via « Terminer et quitter ».
