using Microsoft.AspNetCore.Mvc;
using ZOUZ.Wallet.Core.DTOs.Requests;
using ZOUZ.Wallet.Core.DTOs.Responses;
using ZOUZ.Wallet.Core.Entities.Enum;
using ZOUZ.Wallet.Core.Exceptions;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.API.Endpoints;

public static class WalletEndpoints
    {
        public static void MapWalletEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/wallets")
                .WithTags("Wallets").WithOpenApi();

            // GET /api/wallets - Liste des wallets (avec filtres)
            group.MapGet("/", async (
                [FromQuery] string ownerName,
                [FromQuery] Guid? offerId,
                [FromQuery] decimal? minBalance,
                [FromQuery] decimal? maxBalance,
                [FromQuery] string status,
                [FromQuery] string kycLevel,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10,
                [FromServices] IWalletService walletService = null,
                HttpContext httpContext = null) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;

                var request = new GetWalletsRequest
                {
                    OwnerName = ownerName,
                    OfferId = offerId,
                    MinBalance = minBalance,
                    MaxBalance = maxBalance,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                // Conversion des enums depuis les strings
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<WalletStatus>(status, true, out var statusEnum))
                {
                    request.Status = statusEnum;
                }

                if (!string.IsNullOrEmpty(kycLevel) && Enum.TryParse<KycLevel>(kycLevel, true, out var kycLevelEnum))
                {
                    request.KycLevel = kycLevelEnum;
                }

                var wallets = await walletService.GetWalletsAsync(request, userId);
                return Results.Ok(wallets);
            })
            .WithName("GetWallets")
            .Produces<PagedResponse<WalletResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .RequireAuthorization();

            // GET /api/wallets/{id} - Détails d'un wallet
            group.MapGet("/{id}", async (
                Guid id,
                [FromServices] IWalletService walletService,
                HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;

                try
                {
                    var wallet = await walletService.GetWalletByIdAsync(id, userId);
                    return Results.Ok(ApiResponse<WalletResponse>.SuccessResponse(wallet));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<WalletResponse>.ErrorResponse(ex.Message));
                }
                catch (UnauthorizedException ex)
                {
                    return Results.Unauthorized();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("GetWalletById")
            .Produces<ApiResponse<WalletResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<WalletResponse>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // POST /api/wallets - Créer un wallet
            group.MapPost("/", async (
                [FromBody] CreateWalletRequest request,
                [FromServices] IWalletService walletService,
                HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;

                try
                {
                    var wallet = await walletService.CreateWalletAsync(request, userId);
                    return Results.Created($"/api/wallets/{wallet.Id}", ApiResponse<WalletResponse>.SuccessResponse(wallet, "Wallet créé avec succès"));
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ApiResponse<WalletResponse>.ErrorResponse(ex.Message));
                }
                catch (BusinessRuleException ex)
                {
                    return Results.BadRequest(ApiResponse<WalletResponse>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("CreateWallet")
            .Produces<ApiResponse<WalletResponse>>(StatusCodes.Status201Created)
            .Produces<ApiResponse<WalletResponse>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // PUT /api/wallets/{id} - Mettre à jour un wallet
            group.MapPut("/{id}", async (
                Guid id,
                [FromBody] UpdateWalletRequest request,
                [FromServices] IWalletService walletService,
                HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;

                try
                {
                    var wallet = await walletService.UpdateWalletAsync(id, request, userId);
                    return Results.Ok(ApiResponse<WalletResponse>.SuccessResponse(wallet, "Wallet mis à jour avec succès"));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<WalletResponse>.ErrorResponse(ex.Message));
                }
                catch (UnauthorizedException ex)
                {
                    return Results.Unauthorized();
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ApiResponse<WalletResponse>.ErrorResponse(ex.Message));
                }
                catch (BusinessRuleException ex)
                {
                    return Results.BadRequest(ApiResponse<WalletResponse>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("UpdateWallet")
            .Produces<ApiResponse<WalletResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<WalletResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<WalletResponse>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // DELETE /api/wallets/{id} - Supprimer un wallet
            group.MapDelete("/{id}", async (
                Guid id,
                [FromServices] IWalletService walletService,
                HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;

                try
                {
                    var result = await walletService.DeleteWalletAsync(id, userId);
                    return Results.Ok(ApiResponse<bool>.SuccessResponse(result, "Wallet supprimé avec succès"));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<bool>.ErrorResponse(ex.Message));
                }
                catch (UnauthorizedException ex)
                {
                    return Results.Unauthorized();
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
            .WithName("DeleteWallet")
            .Produces<ApiResponse<bool>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<bool>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<bool>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // GET /api/wallets/{id}/balance - Consulter le solde d'un wallet
            group.MapGet("/{id}/balance", async (
                Guid id,
                [FromServices] IWalletService walletService,
                HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;

                try
                {
                    var balance = await walletService.GetWalletBalanceAsync(id, userId);
                    return Results.Ok(ApiResponse<decimal>.SuccessResponse(balance));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<decimal>.ErrorResponse(ex.Message));
                }
                catch (UnauthorizedException ex)
                {
                    return Results.Unauthorized();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("GetWalletBalance")
            .Produces<ApiResponse<decimal>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<decimal>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // GET /api/wallets/{id}/transactions - Historique des transactions d'un wallet
            group.MapGet("/{id}/transactions", async (
                Guid id,
                [FromQuery] DateTime? startDate,
                [FromQuery] DateTime? endDate,
                [FromQuery] string type,
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10,
                [FromServices] ITransactionService transactionService = null,
                HttpContext httpContext = null) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;

                try
                {
                    // Conversion de l'enum depuis la string
                    TransactionType? transactionType = null;
                    if (!string.IsNullOrEmpty(type) && 
                        Enum.TryParse<TransactionType>(type, true, out var typeEnum))
                    {
                        transactionType = typeEnum;
                    }

                    var transactions = await transactionService.GetWalletTransactionsAsync(
                        id, startDate, endDate, transactionType, pageNumber, pageSize, userId);
                    
                    return Results.Ok(transactions);
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<PagedResponse<TransactionResponse>>.ErrorResponse(ex.Message));
                }
                catch (UnauthorizedException ex)
                {
                    return Results.Unauthorized();
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("GetWalletTransactions")
            .Produces<PagedResponse<TransactionResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<PagedResponse<TransactionResponse>>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // PUT /api/wallets/{id}/offer - Assigner une offre à un wallet
            group.MapPut("/{id}/offer", async (
                Guid id,
                [FromBody] AssignOfferToWalletRequest request,
                [FromServices] IWalletService walletService,
                HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;

                try
                {
                    var wallet = await walletService.AssignOfferToWalletAsync(id, request, userId);
                    return Results.Ok(ApiResponse<WalletResponse>.SuccessResponse(wallet, "Offre assignée avec succès"));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<WalletResponse>.ErrorResponse(ex.Message));
                }
                catch (UnauthorizedException ex)
                {
                    return Results.Unauthorized();
                }
                catch (BusinessRuleException ex)
                {
                    return Results.BadRequest(ApiResponse<WalletResponse>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("AssignOfferToWallet")
            .Produces<ApiResponse<WalletResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<WalletResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<WalletResponse>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // POST /api/wallets/{id}/deposit - Dépôt d'argent
            group.MapPost("/{id}/deposit", async (
                Guid id,
                [FromBody] DepositRequest request,
                [FromServices] ITransactionService transactionService,
                HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;

                try
                {
                    var transaction = await transactionService.DepositAsync(id, request, userId);
                    return Results.Ok(ApiResponse<TransactionResponse>.SuccessResponse(transaction, "Dépôt effectué avec succès"));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
                }
                catch (UnauthorizedException ex)
                {
                    return Results.Unauthorized();
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
                }
                catch (BusinessRuleException ex)
                {
                    return Results.BadRequest(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("DepositToWallet")
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // POST /api/wallets/{id}/withdraw - Retrait d'argent
            group.MapPost("/{id}/withdraw", async (
                Guid id,
                [FromBody] WithdrawalRequest request,
                [FromServices] ITransactionService transactionService,
                HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;

                try
                {
                    var transaction = await transactionService.WithdrawAsync(id, request, userId);
                    return Results.Ok(ApiResponse<TransactionResponse>.SuccessResponse(transaction, "Retrait effectué avec succès"));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
                }
                catch (UnauthorizedException ex)
                {
                    return Results.Unauthorized();
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
                }
                catch (BusinessRuleException ex)
                {
                    return Results.BadRequest(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("WithdrawFromWallet")
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // POST /api/wallets/transfer - Transfert entre wallets
            group.MapPost("/transfer", async (
                [FromBody] TransferRequest request,
                [FromServices] ITransactionService transactionService,
                HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;

                try
                {
                    var transaction = await transactionService.TransferAsync(request, userId);
                    return Results.Ok(ApiResponse<TransactionResponse>.SuccessResponse(transaction, "Transfert effectué avec succès"));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
                }
                catch (UnauthorizedException ex)
                {
                    return Results.Unauthorized();
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
                }
                catch (BusinessRuleException ex)
                {
                    return Results.BadRequest(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("TransferBetweenWallets")
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // POST /api/wallets/{id}/pay-bill - Paiement de facture
            group.MapPost("/{id}/pay-bill", async (
                Guid id,
                [FromBody] PayBillRequest request,
                [FromServices] ITransactionService transactionService,
                HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;

                try
                {
                    var transaction = await transactionService.PayBillAsync(id, request, userId);
                    return Results.Ok(ApiResponse<TransactionResponse>.SuccessResponse(transaction, "Paiement de facture effectué avec succès"));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
                }
                catch (UnauthorizedException ex)
                {
                    return Results.Unauthorized();
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
                }
                catch (BusinessRuleException ex)
                {
                    return Results.BadRequest(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("PayBillFromWallet")
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status400BadRequest)
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();
        }
    }