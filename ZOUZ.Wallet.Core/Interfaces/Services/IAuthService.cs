using ZOUZ.Wallet.Core.DTOs.Requests;
using ZOUZ.Wallet.Core.DTOs.Responses;

namespace ZOUZ.Wallet.Core.Interfaces.Services;

/// <summary>
    /// Interface pour le service d'authentification
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Inscrit un nouvel utilisateur
        /// </summary>
        /// <param name="request">Informations d'inscription</param>
        /// <returns>Résultat de l'inscription avec token JWT si réussie</returns>
        Task<AuthResponse> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Authentifie un utilisateur existant
        /// </summary>
        /// <param name="request">Informations de connexion</param>
        /// <returns>Résultat de l'authentification avec token JWT si réussie</returns>
        Task<AuthResponse> LoginAsync(LoginRequest request);

        /// <summary>
        /// Rafraîchit un token JWT expiré
        /// </summary>
        /// <param name="request">Token d'accès expiré et token de rafraîchissement</param>
        /// <returns>Nouveaux tokens JWT</returns>
        Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);

        /// <summary>
        /// Révoque un token de rafraîchissement
        /// </summary>
        /// <param name="token">Token à révoquer</param>
        /// <returns>Vrai si le token a été révoqué avec succès</returns>
        Task<bool> RevokeTokenAsync(string token);

        /// <summary>
        /// Change le mot de passe d'un utilisateur
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="request">Ancien et nouveau mot de passe</param>
        /// <returns>Vrai si le mot de passe a été changé avec succès</returns>
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);

        /// <summary>
        /// Demande une réinitialisation de mot de passe
        /// </summary>
        /// <param name="email">Email de l'utilisateur</param>
        /// <returns>Vrai si l'email de réinitialisation a été envoyé</returns>
        Task<bool> RequestPasswordResetAsync(string email);

        /// <summary>
        /// Réinitialise le mot de passe avec un token
        /// </summary>
        /// <param name="request">Token de réinitialisation et nouveau mot de passe</param>
        /// <returns>Vrai si le mot de passe a été réinitialisé avec succès</returns>
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);

        /// <summary>
        /// Vérifie l'adresse email d'un utilisateur
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="token">Token de vérification</param>
        /// <returns>Vrai si l'email a été vérifié avec succès</returns>
        Task<bool> VerifyEmailAsync(string userId, string token);

        /// <summary>
        /// Vérifie le numéro de téléphone d'un utilisateur
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="code">Code de vérification</param>
        /// <returns>Vrai si le téléphone a été vérifié avec succès</returns>
        Task<bool> VerifyPhoneAsync(string userId, string code);

        /// <summary>
        /// Initie l'authentification à deux facteurs
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <returns>Vrai si le code 2FA a été envoyé avec succès</returns>
        Task<bool> InitiateTwoFactorAsync(string userId);

        /// <summary>
        /// Vérifie un code d'authentification à deux facteurs
        /// </summary>
        /// <param name="userId">ID de l'utilisateur</param>
        /// <param name="code">Code 2FA</param>
        /// <returns>Vrai si le code 2FA est valide</returns>
        Task<bool> VerifyTwoFactorAsync(string userId, string code);
    }