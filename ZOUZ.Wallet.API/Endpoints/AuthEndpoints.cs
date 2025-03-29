using Microsoft.AspNetCore.Mvc;
using ZOUZ.Wallet.Core.DTOs.Requests;
using ZOUZ.Wallet.Core.DTOs.Responses;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.API.Endpoints;

public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/auth")
                .WithTags("Authentication").WithOpenApi();

            // POST /api/auth/register - Inscription
            group.MapPost("/register", async (
                [FromBody] RegisterRequest request,
                [FromServices] IAuthService authService) =>
            {
                try
                {
                    var result = await authService.RegisterAsync(request);
                    
                    if (result.Success)
                    {
                        return Results.Ok(result);
                    }
                    
                    return Results.BadRequest(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("Register")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces<AuthResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

            // POST /api/auth/login - Connexion
            group.MapPost("/login", async (
                [FromBody] LoginRequest request,
                [FromServices] IAuthService authService) =>
            {
                try
                {
                    var result = await authService.LoginAsync(request);
                    
                    if (result.Success)
                    {
                        return Results.Ok(result);
                    }
                    
                    return Results.BadRequest(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("Login")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces<AuthResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

            // POST /api/auth/refresh-token - Rafraîchir le token
            group.MapPost("/refresh-token", async (
                [FromBody] RefreshTokenRequest request,
                [FromServices] IAuthService authService) =>
            {
                try
                {
                    var result = await authService.RefreshTokenAsync(request);
                    
                    if (result.Success)
                    {
                        return Results.Ok(result);
                    }
                    
                    return Results.BadRequest(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("RefreshToken")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces<AuthResponse>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

            // POST /api/auth/revoke-token - Révoquer le token
            group.MapPost("/revoke-token", async (
                [FromBody] string token,
                [FromServices] IAuthService authService) =>
            {
                try
                {
                    var result = await authService.RevokeTokenAsync(token);
                    
                    if (result)
                    {
                        return Results.Ok(new { Success = true, Message = "Token révoqué avec succès" });
                    }
                    
                    return Results.BadRequest(new { Success = false, Message = "Impossible de révoquer le token" });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("RevokeToken")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // POST /api/auth/change-password - Changer le mot de passe
            group.MapPost("/change-password", async (
                [FromBody] ChangePasswordRequest request,
                [FromServices] IAuthService authService,
                HttpContext httpContext) =>
            {
                try
                {
                    var userId = httpContext.User.FindFirst("sub")?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        return Results.Unauthorized();
                    }
                    
                    var result = await authService.ChangePasswordAsync(userId, request);
                    
                    if (result)
                    {
                        return Results.Ok(new { Success = true, Message = "Mot de passe changé avec succès" });
                    }
                    
                    return Results.BadRequest(new { Success = false, Message = "Impossible de changer le mot de passe" });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("ChangePassword")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // POST /api/auth/request-password-reset - Demander la réinitialisation du mot de passe
            group.MapPost("/request-password-reset", async (
                [FromBody] string email,
                [FromServices] IAuthService authService) =>
            {
                try
                {
                    var result = await authService.RequestPasswordResetAsync(email);
                    
                    // On renvoie toujours un succès pour des raisons de sécurité (ne pas révéler si l'email existe)
                    return Results.Ok(new { Success = true, Message = "Si l'email existe, un lien de réinitialisation a été envoyé" });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("RequestPasswordReset")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

            // POST /api/auth/reset-password - Réinitialiser le mot de passe
            group.MapPost("/reset-password", async (
                [FromBody] ResetPasswordRequest request,
                [FromServices] IAuthService authService) =>
            {
                try
                {
                    var result = await authService.ResetPasswordAsync(request);
                    
                    if (result)
                    {
                        return Results.Ok(new { Success = true, Message = "Mot de passe réinitialisé avec succès" });
                    }
                    
                    return Results.BadRequest(new { Success = false, Message = "Impossible de réinitialiser le mot de passe" });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("ResetPassword")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

            // GET /api/auth/verify-email - Vérifier l'email
            group.MapGet("/verify-email", async (
                [FromQuery] string userId,
                [FromQuery] string token,
                [FromServices] IAuthService authService) =>
            {
                try
                {
                    var result = await authService.VerifyEmailAsync(userId, token);
                    
                    if (result)
                    {
                        return Results.Ok(new { Success = true, Message = "Email vérifié avec succès" });
                    }
                    
                    return Results.BadRequest(new { Success = false, Message = "Impossible de vérifier l'email" });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("VerifyEmail")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);

            // POST /api/auth/verify-phone - Vérifier le numéro de téléphone
            group.MapPost("/verify-phone", async (
                [FromBody] string code,
                [FromServices] IAuthService authService,
                HttpContext httpContext) =>
            {
                try
                {
                    var userId = httpContext.User.FindFirst("sub")?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        return Results.Unauthorized();
                    }
                    
                    var result = await authService.VerifyPhoneAsync(userId, code);
                    
                    if (result)
                    {
                        return Results.Ok(new { Success = true, Message = "Numéro de téléphone vérifié avec succès" });
                    }
                    
                    return Results.BadRequest(new { Success = false, Message = "Impossible de vérifier le numéro de téléphone" });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("VerifyPhone")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // POST /api/auth/initiate-2fa - Initier l'authentification à deux facteurs
            group.MapPost("/initiate-2fa", async (
                [FromServices] IAuthService authService,
                HttpContext httpContext) =>
            {
                try
                {
                    var userId = httpContext.User.FindFirst("sub")?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        return Results.Unauthorized();
                    }
                    
                    var result = await authService.InitiateTwoFactorAsync(userId);
                    
                    if (result)
                    {
                        return Results.Ok(new { Success = true, Message = "Code d'authentification à deux facteurs envoyé" });
                    }
                    
                    return Results.BadRequest(new { Success = false, Message = "Impossible d'initier l'authentification à deux facteurs" });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("Initiate2FA")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // POST /api/auth/verify-2fa - Vérifier l'authentification à deux facteurs
            group.MapPost("/verify-2fa", async (
                [FromBody] string code,
                [FromServices] IAuthService authService,
                HttpContext httpContext) =>
            {
                try
                {
                    var userId = httpContext.User.FindFirst("sub")?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        return Results.Unauthorized();
                    }
                    
                    var result = await authService.VerifyTwoFactorAsync(userId, code);
                    
                    if (result)
                    {
                        return Results.Ok(new { Success = true, Message = "Authentification à deux facteurs validée" });
                    }
                    
                    return Results.BadRequest(new { Success = false, Message = "Code d'authentification à deux facteurs invalide" });
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("Verify2FA")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces<object>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
        }
    }