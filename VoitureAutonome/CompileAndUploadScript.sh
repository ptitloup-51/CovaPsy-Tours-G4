#!/bin/bash

# Configuration
RUNTIME="linux-arm64"
PUBLISH_DIR="publish_output"
REMOTE_USER="covapsytours5"
REMOTE_HOST="192.168.2.127"
REMOTE_PASS="covapsytours5"
REMOTE_DIR="/home/covapsytours5/Documents/Test"
EXECUTABLE="VoitureAutonome"

# Étape 1 : Publier l'application
echo "📦 Publication de l'application pour $RUNTIME..."
dotnet publish --runtime $RUNTIME --self-contained false -o $PUBLISH_DIR

if [ $? -ne 0 ]; then
    echo "❌ Erreur lors de la publication !"
    exit 1
fi

# Étape 2 : Transférer les fichiers en SSH
echo "📡 Transfert des fichiers vers $REMOTE_HOST..."

# Créer le répertoire distant si nécessaire
sshpass -p "$REMOTE_PASS" ssh -o StrictHostKeyChecking=no $REMOTE_USER@$REMOTE_HOST "mkdir -p $REMOTE_DIR"

# Transférer les fichiers avec rsync
sshpass -p "$REMOTE_PASS" rsync -avz --delete $PUBLISH_DIR/ $REMOTE_USER@$REMOTE_HOST:$REMOTE_DIR/

if [ $? -ne 0 ]; then
    echo "❌ Erreur lors du transfert des fichiers !"
    exit 1
fi

# Étape 3 : Donner les permissions et exécuter l'application en gardant le terminal ouvert
echo "🔑 Attribution des permissions et exécution du programme en premier plan..."

sshpass -p "$REMOTE_PASS" ssh -t -o StrictHostKeyChecking=no $REMOTE_USER@$REMOTE_HOST << EOF
    chmod +x $REMOTE_DIR/$EXECUTABLE
    echo "🚀 Chargement des variables d'environnement..."
    
    # Ajout explicite de dotnet au PATH
    export DOTNET_ROOT=/home/covapsytours5/.dotnet
    export PATH=\$DOTNET_ROOT:\$PATH

    # Vérification du chemin de dotnet
    echo "🔍 Chemin de dotnet utilisé : \$(which dotnet)"

    echo "🚀 Lancement de l'application en premier plan..."
    cd $REMOTE_DIR
    sudo -E ./VoitureAutonome
EOF

if [ $? -ne 0 ]; then
    echo "❌ Erreur lors de l'exécution de la commande SSH !"
    exit 1
fi

echo "✅ Déploiement terminé avec succès !"