# 💳 ZOUZ Wallet API

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

Une API RESTful complète pour la gestion de wallets électroniques, conçue pour respecter les normes de Bank Al-Maghrib. Cette solution fournit une infrastructure robuste pour la gestion des comptes numériques, les transactions financières et les offres promotionnelles.

## ✨ Fonctionnalités

### Gestion des Wallets
- Création, mise à jour et suppression de wallets
- Vérification du solde et de l'historique des transactions
- Filtrage des wallets par propriétaire, offre, solde et état

### Transactions Financières
- Dépôt d'argent (carte bancaire, virement, Orange Money/Inwi Money)
- Retrait d'argent avec vérification du solde
- Transfert entre wallets
- Paiement de factures locales (télécoms, eau, électricité, taxes)
- Journalisation complète des transactions

### Offres et Promotions
- Gestion des offres (cashback, frais réduits, bonus de recharge)
- Attribution d'offres aux wallets
- Contrôle des plafonds de dépense

### Conformité Réglementaire
- Respect des règles de Bank Al-Maghrib
- Gestion de l'identification des utilisateurs (KYC)
- Vérification d'identité via CIN
- Restrictions pour les utilisateurs non vérifiés
- Audit et traçabilité

### Sécurité Avancée
- Authentification JWT pour utilisateurs et administrateurs
- Gestion fine des autorisations
- Double authentification (2FA) pour les opérations sensibles
- Détection des transactions suspectes
- Protection contre les attaques (rate limiting, CORS)

## 🏗️ Architecture

Ce projet suit une architecture en couches (Clean Architecture) :

```
ZOUZ.Wallet/ 
├── ZOUZ.Wallet.API/            # Couche de présentation
├── ZOUZ.Wallet.Core/           # Couche de domaine (entités, interfaces)
└── ZOUZ.Wallet.Infrastructure/ # Couche d'infrastructure (accès aux données)
```

## 🚀 Technologies

- **.NET 8** : Plateforme et framework de développement
- **ASP.NET Core Minimal APIs** : API RESTful légère et performante
- **Entity Framework Core** : ORM pour l'accès aux données
- **SQL Server/PostgreSQL** : Base de données relationnelle
- **JWT Authentication** : Sécurité et gestion des sessions
- **Swagger/OpenAPI** : Documentation interactive de l'API
- **FluentValidation** : Validation de données
- **Automapper** : Mapping entre objets

## 🔧 Installation

### Prérequis
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server/) ou [PostgreSQL](https://www.postgresql.org/)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) / [VS Code](https://code.visualstudio.com/) / [JetBrains Rider](https://www.jetbrains.com/rider/)

### Configuration

1. Cloner le dépôt :
   ```bash
   git clone https://github.com/votre-username/ZOUZ.Wallet.git
   cd ZOUZ.Wallet
   ```

2. Configurer la chaîne de connexion à la base de données dans `appsettings.json` :
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ZOUZWalletDb;Trusted_Connection=True;"
   }
   ```

3. Appliquer les migrations pour créer la base de données :
   ```bash
   cd src/ZOUZ.Wallet.API
   dotnet ef database update
   ```

4. Lancer l'application :
   ```bash
   dotnet run
   ```

5. Accéder à l'interface Swagger :
   ```
   https://localhost:5001/swagger
   ```

## 📝 Exemples d'utilisation

### Authentification

```bash
# Inscription d'un nouvel utilisateur
curl -X POST "https://localhost:5001/api/auth/register" \
     -H "Content-Type: application/json" \
     -d '{
       "username": "testuser",
       "email": "testuser@example.com",
       "password": "Password123!",
       "confirmPassword": "Password123!",
       "fullName": "Test User",
       "phoneNumber": "+2126XXXXXXXX"
     }'

# Connexion
curl -X POST "https://localhost:5001/api/auth/login" \
     -H "Content-Type: application/json" \
     -d '{
       "username": "testuser",
       "password": "Password123!"
     }'
```

### Gestion des wallets

```bash
# Créer un wallet
curl -X POST "https://localhost:5001/api/wallets" \
     -H "Authorization: Bearer {token}" \
     -H "Content-Type: application/json" \
     -d '{
       "ownerName": "Test User",
       "phoneNumber": "+2126XXXXXXXX",
       "initialBalance": 1000,
       "currency": "MAD"
     }'

# Obtenir les détails d'un wallet
curl -X GET "https://localhost:5001/api/wallets/{id}" \
     -H "Authorization: Bearer {token}"
```

### Transactions

```bash
# Effectuer un dépôt
curl -X POST "https://localhost:5001/api/wallets/{id}/deposit" \
     -H "Authorization: Bearer {token}" \
     -H "Content-Type: application/json" \
     -d '{
       "amount": 500,
       "paymentMethod": "CreditCard",
       "description": "Test deposit",
       "cardNumber": "4111111111111111",
       "cardHolderName": "Test User",
       "expiryDate": "12/25",
       "cvv": "123"
     }'

# Effectuer un transfert
curl -X POST "https://localhost:5001/api/wallets/transfer" \
     -H "Authorization: Bearer {token}" \
     -H "Content-Type: application/json" \
     -d '{
       "sourceWalletId": "{sourceId}",
       "destinationWalletId": "{destinationId}",
       "amount": 200,
       "description": "Test transfer"
     }'
```

## 📁 Structure des dossiers détaillée

```
ZOUZ.Wallet/
   ├── ZOUZ.Wallet.API/
   │   ├── Program.cs                    # Point d'entrée et configuration
   │   ├── appsettings.json              # Configuration de l'application
   │   ├── Endpoints/                    # Définitions des endpoints
   │   │   ├── WalletEndpoints.cs
   │   │   ├── OfferEndpoints.cs
   │   │   ├── TransactionEndpoints.cs
   │   │   └── AuthEndpoints.cs
   │   └── Middleware/                   # Middleware personnalisé
   │
   ├── ZOUZ.Wallet.Core/
   │   ├── Entities/                     # Entités de domaine
   │   ├── DTOs/                         # Objets de transfert de données
   │   ├── Interfaces/                   # Interfaces de service et repository
   │   ├── Services/                     # Implémentation des services
   │   └── Exceptions/                   # Exceptions personnalisées
   │
   └── ZOUZ.Wallet.Infrastructure/
       ├── Data/                         # Accès aux données
       │   ├── WalletDbContext.cs
       │   ├── Configurations/           # Configurations EF Core
       │   └── Migrations/               # Migrations EF Core
       ├── Repositories/                 # Implémentation des repositories
       └── Services/                     # Services d'infrastructure


```

## 👥 Comment contribuer

Les contributions sont les bienvenues !.

1. Fork du projet
2. Créez votre branche de fonctionnalité (`git checkout -b feature/amazing-feature`)
3. Committez vos changements (`git commit -m 'Add some amazing feature'`)
4. Push vers la branche (`git push origin feature/amazing-feature`)
5. Ouvrez une Pull Request

## 📄 Licence

Ce projet est sous licence MIT.

## 🙏 Remerciements

- [Bank Al-Maghrib](https://www.bkam.ma/) pour les directives réglementaires
- Tous les contributeurs qui participent à l'amélioration de ce projet
