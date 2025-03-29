using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ZOUZ.Wallet.Core.DTOs.Requests;
using ZOUZ.Wallet.Core.DTOs.Responses;
using ZOUZ.Wallet.Core.Entities;
using ZOUZ.Wallet.Core.Entities.Enum;
using ZOUZ.Wallet.Core.Interfaces.Repositories;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.Core.Services;

public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            ITokenService tokenService,
            IEmailService emailService,
            ISmsService smsService,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _tokenService = tokenService;
            _emailService = emailService;
            _smsService = smsService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            _logger.LogInformation("Registering user with username: {Username}", request.Username);

            // Vérifier si l'utilisateur existe déjà
            if (await _userRepository.UsernameExistsAsync(request.Username))
            {
                return new AuthResponse 
                { 
                    Success = false, 
                    Errors = new List<string> { "Ce nom d'utilisateur est déjà utilisé." }
                };
            }

            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                return new AuthResponse 
                { 
                    Success = false, 
                    Errors = new List<string> { "Cette adresse e-mail est déjà utilisée." }
                };
            }

            if (request.Password != request.ConfirmPassword)
            {
                return new AuthResponse 
                { 
                    Success = false, 
                    Errors = new List<string> { "Les mots de passe ne correspondent pas." }
                };
            }

            // Créer un nouvel utilisateur
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                FullName = request.FullName,
                PhoneNumber = request.PhoneNumber,
                KycLevel = KycLevel.None,
                Role = "User",
                CreatedAt = DateTime.UtcNow,
                IsEmailVerified = false,
                IsPhoneVerified = false,
                IsTwoFactorEnabled = false
            };

            // Enregistrer l'utilisateur
            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            // Envoyer un e-mail de vérification
            await SendVerificationEmailAsync(user);

            // Générer le token JWT
            var token = _tokenService.GenerateJwtToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Stocker le refresh token (dans un scénario réel, cela serait stocké en base de données)
            // Pour simplifier, nous ne le stockons pas ici

            return new AuthResponse
            {
                Success = true,
                AccessToken = token,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpireMinutes"])),
                UserId = user.Id.ToString(),
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            _logger.LogInformation("Login attempt for username: {Username}", request.Username);

            // Récupérer l'utilisateur
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                return new AuthResponse 
                { 
                    Success = false, 
                    Errors = new List<string> { "Nom d'utilisateur ou mot de passe incorrect." }
                };
            }

            // Vérifier le mot de passe
            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Invalid password for username: {Username}", request.Username);
                return new AuthResponse 
                { 
                    Success = false, 
                    Errors = new List<string> { "Nom d'utilisateur ou mot de passe incorrect." }
                };
            }

            // Vérifier si l'authentification à deux facteurs est requise
            if (user.IsTwoFactorEnabled)
            {
                // Envoyer un code par SMS ou email
                await InitiateTwoFactorAsync(user.Id.ToString());

                return new AuthResponse
                {
                    Success = true,
                    RequiresTwoFactor = true,
                    UserId = user.Id.ToString(),
                    Username = user.Username
                };
            }

            // Générer le token JWT
            var token = _tokenService.GenerateJwtToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Stocker le refresh token (dans un scénario réel, cela serait stocké en base de données)
            // Pour simplifier, nous ne le stockons pas ici

            return new AuthResponse
            {
                Success = true,
                AccessToken = token,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpireMinutes"])),
                UserId = user.Id.ToString(),
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            _logger.LogInformation("Refreshing token");

            try
            {
                // Valider le token d'accès expiré
                var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return new AuthResponse 
                    { 
                        Success = false, 
                        Errors = new List<string> { "Token invalide." }
                    };
                }

                // Récupérer l'utilisateur
                var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
                if (user == null)
                {
                    return new AuthResponse 
                    { 
                        Success = false, 
                        Errors = new List<string> { "Utilisateur non trouvé." }
                    };
                }

                // Vérifier le refresh token (dans un scénario réel, cela serait vérifié en base de données)
                // Pour simplifier, nous ne vérifions pas ici

                // Générer un nouveau token JWT
                var newToken = _tokenService.GenerateJwtToken(user);
                var newRefreshToken = _tokenService.GenerateRefreshToken();

                return new AuthResponse
                {
                    Success = true,
                    AccessToken = newToken,
                    RefreshToken = newRefreshToken,
                    Expiration = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpireMinutes"])),
                    UserId = user.Id.ToString(),
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return new AuthResponse 
                { 
                    Success = false, 
                    Errors = new List<string> { "Erreur lors du rafraîchissement du token." }
                };
            }
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            _logger.LogInformation("Revoking token");

            // Dans un scénario réel, le token serait marqué comme révoqué en base de données
            // Pour simplifier, nous retournons simplement true
            return await Task.FromResult(true);
        }

        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request)
        {
            _logger.LogInformation("Changing password for user ID: {UserId}", userId);

            if (request.NewPassword != request.ConfirmNewPassword)
            {
                _logger.LogWarning("Password mismatch for user ID: {UserId}", userId);
                return false;
            }

            // Récupérer l'utilisateur
            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return false;
            }

            // Vérifier l'ancien mot de passe
            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                _logger.LogWarning("Invalid current password for user ID: {UserId}", userId);
                return false;
            }

            // Mettre à jour le mot de passe
            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            // Envoyer une notification
            await _emailService.SendEmailAsync(
                user.Email,
                "Mot de passe modifié",
                $"Bonjour {user.FullName}, votre mot de passe a été modifié avec succès.");

            return true;
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            _logger.LogInformation("Password reset requested for email: {Email}", email);

            // Récupérer l'utilisateur
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                // Pour des raisons de sécurité, ne pas indiquer si l'e-mail existe ou non
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
                return true;
            }

            // Générer un token de réinitialisation (dans un scénario réel, cela serait stocké en base de données)
            var resetToken = GenerateRandomToken();

            // Envoyer un e-mail de réinitialisation
            await _emailService.SendEmailAsync(
                user.Email,
                "Réinitialisation de mot de passe",
                $"Bonjour {user.FullName}, voici votre lien de réinitialisation de mot de passe : " +
                $"https://walletapi.ma/reset-password?email={user.Email}&token={resetToken}");

            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            _logger.LogInformation("Resetting password for email: {Email}", request.Email);

            if (request.NewPassword != request.ConfirmNewPassword)
            {
                _logger.LogWarning("Password mismatch for email: {Email}", request.Email);
                return false;
            }

            // Récupérer l'utilisateur
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("User not found for email: {Email}", request.Email);
                return false;
            }

            // Vérifier le token de réinitialisation (dans un scénario réel, cela serait vérifié en base de données)
            // Pour simplifier, nous ne vérifions pas ici

            // Mettre à jour le mot de passe
            user.PasswordHash = HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            // Envoyer une notification
            await _emailService.SendEmailAsync(
                user.Email,
                "Mot de passe réinitialisé",
                $"Bonjour {user.FullName}, votre mot de passe a été réinitialisé avec succès.");

            return true;
        }

        public async Task<bool> VerifyEmailAsync(string userId, string token)
        {
            _logger.LogInformation("Verifying email for user ID: {UserId}", userId);

            // Récupérer l'utilisateur
            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return false;
            }

            // Vérifier le token de vérification (dans un scénario réel, cela serait vérifié en base de données)
            // Pour simplifier, nous ne vérifions pas ici

            // Marquer l'e-mail comme vérifié
            user.IsEmailVerified = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> VerifyPhoneAsync(string userId, string code)
        {
            _logger.LogInformation("Verifying phone for user ID: {UserId}", userId);

            // Récupérer l'utilisateur
            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return false;
            }

            // Vérifier le code (dans un scénario réel, cela serait vérifié via un service externe)
            // Pour simplifier, nous vérifions simplement que le code a 6 chiffres
            if (string.IsNullOrEmpty(code) || code.Length != 6 || !int.TryParse(code, out _))
            {
                _logger.LogWarning("Invalid verification code for user ID: {UserId}", userId);
                return false;
            }

            // Marquer le téléphone comme vérifié
            user.IsPhoneVerified = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> InitiateTwoFactorAsync(string userId)
        {
            _logger.LogInformation("Initiating two-factor authentication for user ID: {UserId}", userId);

            // Récupérer l'utilisateur
            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return false;
            }

            // Générer un code à 6 chiffres
            var code = GenerateRandomCode();

            // Envoyer le code par SMS
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
                await _smsService.SendSmsAsync(
                    user.PhoneNumber,
                    $"Votre code d'authentification à deux facteurs : {code}");
            }

            // Envoyer également le code par e-mail (option de secours)
            await _emailService.SendEmailAsync(
                user.Email,
                "Code d'authentification à deux facteurs",
                $"Bonjour {user.FullName}, votre code d'authentification à deux facteurs : {code}");

            // Dans un scénario réel, le code serait stocké temporairement en base de données ou dans un cache
            // Pour simplifier, nous ne le stockons pas ici

            return true;
        }

        public async Task<bool> VerifyTwoFactorAsync(string userId, string code)
        {
            _logger.LogInformation("Verifying two-factor code for user ID: {UserId}", userId);

            // Récupérer l'utilisateur
            var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return false;
            }

            // Vérifier le code (dans un scénario réel, cela serait vérifié en base de données ou dans un cache)
            // Pour simplifier, nous vérifions simplement que le code a 6 chiffres
            if (string.IsNullOrEmpty(code) || code.Length != 6 || !int.TryParse(code, out _))
            {
                _logger.LogWarning("Invalid two-factor code for user ID: {UserId}", userId);
                return false;
            }

            // Dans un scénario réel, nous vérifierions que le code correspond à celui généré précédemment
            // Pour simplifier, nous supposons que le code est correct

            return true;
        }

        // Méthodes privées

        private string HashPassword(string password)
        {
            // Dans un scénario réel, nous utiliserions un algorithme de hachage sécurisé comme PBKDF2 ou Argon2
            // Pour simplifier, nous utilisons BCrypt
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private bool VerifyPassword(string password, string hash)
        {
            // Vérifier le mot de passe avec BCrypt
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }

        private string GenerateRandomToken()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes);
        }

        private string GenerateRandomCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private async Task SendVerificationEmailAsync(User user)
        {
            // Générer un token de vérification (dans un scénario réel, cela serait stocké en base de données)
            var verificationToken = GenerateRandomToken();

            // Envoyer un e-mail de vérification
            await _emailService.SendEmailAsync(
                user.Email,
                "Vérification de votre adresse e-mail",
                $"Bonjour {user.FullName}, veuillez confirmer votre adresse e-mail en cliquant sur ce lien : " +
                $"https://walletapi.ma/verify-email?userId={user.Id}&token={verificationToken}");
        }
    }