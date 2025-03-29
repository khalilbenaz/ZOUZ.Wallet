using Microsoft.AspNetCore.Mvc;
using ZOUZ.Wallet.Core.DTOs.Requests;
using ZOUZ.Wallet.Core.DTOs.Responses;
using ZOUZ.Wallet.Core.Entities.Enum;
using ZOUZ.Wallet.Core.Exceptions;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.API.Endpoints;

public static class OfferEndpoints
    {
        public static void MapOfferEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/offers")
                .WithTags("Offers").WithOpenApi();

            // GET /api/offers - Liste des offres
            group.MapGet("/", async (
                [FromQuery] bool activeOnly = false,
                [FromQuery] string type = null,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10,
                [FromServices] IOfferService offerService = null) =>
            {
                // Conversion de l'enum depuis la string
                OfferType? offerType = null;
                if (!string.IsNullOrEmpty(type) && 
                    Enum.TryParse<OfferType>(type, true, out var typeEnum))
                {
                    offerType = typeEnum;
                }

                var offers = await offerService.GetOffersAsync(activeOnly, offerType, pageNumber, pageSize);
                return Results.Ok(offers);
            })
            .WithName("GetOffers")
            .Produces<PagedResponse<OfferResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status500InternalServerError);

            // GET /api/offers/{id} - Détails d'une offre
            group.MapGet("/{id}", async (
                Guid id,
                [FromServices] IOfferService offerService) =>
            {
                try
                {
                    var offer = await offerService.GetOfferByIdAsync(id);
                    return Results.Ok(ApiResponse<OfferResponse>.SuccessResponse(offer));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<OfferResponse>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("GetOfferById")
            .Produces<ApiResponse<OfferResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<OfferResponse>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError);

            // POST /api/offers - Créer une offre (Admin seulement)
            group.MapPost("/", async (
                [FromBody] CreateOfferRequest request,
                [FromServices] IOfferService offerService,
                HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;
                var isAdmin = httpContext.User.IsInRole("Admin");

                if (!isAdmin)
                {
                    return Results.Forbid();
                }

                try
                {
                    var offer = await offerService.CreateOfferAsync(request);
                    return Results.Created($"/api/offers/{offer.Id}", ApiResponse<OfferResponse>.SuccessResponse(offer, "Offre créée avec succès"));
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ApiResponse<OfferResponse>.ErrorResponse(ex.Message));
                }
                catch (BusinessRuleException ex)
                {
                    return Results.BadRequest(ApiResponse<OfferResponse>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("CreateOffer")
            .Produces<ApiResponse<OfferResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<OfferResponse>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

            // PUT /api/offers/{id} - Mettre à jour une offre (Admin seulement)
            group.MapPut("/{id}", async (
                Guid id,
                [FromBody] CreateOfferRequest request,
                [FromServices] IOfferService offerService,
                HttpContext httpContext) =>
            {
                var isAdmin = httpContext.User.IsInRole("Admin");

                if (!isAdmin)
                {
                    return Results.Forbid();
                }

                try
                {
                    var offer = await offerService.UpdateOfferAsync(id, request);
                    return Results.Ok(ApiResponse<OfferResponse>.SuccessResponse(offer, "Offre mise à jour avec succès"));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<OfferResponse>.ErrorResponse(ex.Message));
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ApiResponse<OfferResponse>.ErrorResponse(ex.Message));
                }
                catch (BusinessRuleException ex)
                {
                    return Results.BadRequest(ApiResponse<OfferResponse>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("UpdateOffer")
            .Produces<ApiResponse<OfferResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<OfferResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<OfferResponse>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

            // DELETE /api/offers/{id} - Supprimer une offre (Admin seulement)
            group.MapDelete("/{id}", async (
                Guid id,
                [FromServices] IOfferService offerService,
                HttpContext httpContext) =>
            {
                var isAdmin = httpContext.User.IsInRole("Admin");

                if (!isAdmin)
                {
                    return Results.Forbid();
                }

                try
                {
                    var result = await offerService.DeleteOfferAsync(id);
                    return Results.Ok(ApiResponse<bool>.SuccessResponse(result, "Offre supprimée avec succès"));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<bool>.ErrorResponse(ex.Message));
                }
                catch (BusinessRuleException ex)
                {
                    return Results.BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("DeleteOffer")
            .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<bool>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<bool>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

            // GET /api/offers/{id}/wallets - Liste des wallets associés à une offre (Admin seulement)
            group.MapGet("/{id}/wallets", async (
                Guid id,
                [FromServices] IOfferService offerService,
                HttpContext httpContext,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10) =>
            {
                var isAdmin = httpContext.User.IsInRole("Admin");

                if (!isAdmin)
                {
                    return Results.Forbid();
                }

                try
                {
                    var wallets = await offerService.GetWalletsByOfferIdAsync(id, pageNumber, pageSize);
                    return Results.Ok(wallets);
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<PagedResponse<WalletResponse>>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("GetWalletsByOfferId")
            .Produces<PagedResponse<WalletResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<PagedResponse<WalletResponse>>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

            // PATCH /api/offers/{id}/activate - Activer une offre (Admin seulement)
            group.MapPatch("/{id}/activate", async (
                Guid id,
                [FromServices] IOfferService offerService,
                HttpContext httpContext) =>
            {
                var isAdmin = httpContext.User.IsInRole("Admin");

                if (!isAdmin)
                {
                    return Results.Forbid();
                }

                try
                {
                    var offer = await offerService.ActivateOfferAsync(id);
                    return Results.Ok(ApiResponse<OfferResponse>.SuccessResponse(offer, "Offre activée avec succès"));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<OfferResponse>.ErrorResponse(ex.Message));
                }
                catch (BusinessRuleException ex)
                {
                    return Results.BadRequest(ApiResponse<OfferResponse>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("ActivateOffer")
            .Produces<ApiResponse<OfferResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<OfferResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<OfferResponse>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

            // PATCH /api/offers/{id}/deactivate - Désactiver une offre (Admin seulement)
            group.MapPatch("/{id}/deactivate", async (
                Guid id,
                [FromServices] IOfferService offerService,
                HttpContext httpContext) =>
            {
                var isAdmin = httpContext.User.IsInRole("Admin");

                if (!isAdmin)
                {
                    return Results.Forbid();
                }

                try
                {
                    var offer = await offerService.DeactivateOfferAsync(id);
                    return Results.Ok(ApiResponse<OfferResponse>.SuccessResponse(offer, "Offre désactivée avec succès"));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<OfferResponse>.ErrorResponse(ex.Message));
                }
                catch (BusinessRuleException ex)
                {
                    return Results.BadRequest(ApiResponse<OfferResponse>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("DeactivateOffer")
            .Produces<ApiResponse<OfferResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<OfferResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<OfferResponse>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
        }
    }