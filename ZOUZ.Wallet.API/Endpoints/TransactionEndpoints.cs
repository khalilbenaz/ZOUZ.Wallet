using Microsoft.AspNetCore.Mvc;
using ZOUZ.Wallet.Core.DTOs.Responses;
using ZOUZ.Wallet.Core.Exceptions;
using ZOUZ.Wallet.Core.Interfaces.Services;

namespace ZOUZ.Wallet.API.Endpoints;

public static class TransactionEndpoints
    {
        public static void MapTransactionEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/transactions")
                .WithTags("Transactions").WithOpenApi();

            // GET /api/transactions/{id} - Consulter une transaction
            group.MapGet("/{id}", async (
                Guid id,
                [FromServices] ITransactionService transactionService,
                HttpContext httpContext) =>
            {
                var userId = httpContext.User.FindFirst("sub")?.Value;

                try
                {
                    // Cette méthode devrait être implémentée dans le service de transactions
                    var transaction = await GetTransactionByIdAsync(id, transactionService, userId);
                    return Results.Ok(ApiResponse<TransactionResponse>.SuccessResponse(transaction));
                }
                catch (NotFoundException ex)
                {
                    return Results.NotFound(ApiResponse<TransactionResponse>.ErrorResponse(ex.Message));
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
            .WithName("GetTransactionById")
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status200OK)
            .Produces<ApiResponse<TransactionResponse>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization();

            // GET /api/transactions/suspicious - Récupérer les transactions suspectes (Admin seulement)
            group.MapGet("/suspicious", async (
                [FromQuery] DateTime? startDate,
                [FromQuery] DateTime? endDate,
                [FromServices] ITransactionService transactionService,
                HttpContext httpContext) =>
            {
                var isAdmin = httpContext.User.IsInRole("Admin");

                if (!isAdmin)
                {
                    return Results.Forbid();
                }

                try
                {
                    // Cette méthode devrait être implémentée dans le service de transactions
                    var transactions = await GetSuspiciousTransactionsAsync(
                        startDate ?? DateTime.UtcNow.AddDays(-30),
                        endDate ?? DateTime.UtcNow,
                        transactionService);
                    
                    return Results.Ok(transactions);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("GetSuspiciousTransactions")
            .Produces<PagedResponse<TransactionResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

            // GET /api/transactions/reports/daily - Rapport quotidien des transactions (Admin seulement)
            group.MapGet("/reports/daily", async (
                [FromQuery] DateTime? date,
                [FromServices] ITransactionService transactionService,
                HttpContext httpContext) =>
            {
                var isAdmin = httpContext.User.IsInRole("Admin");

                if (!isAdmin)
                {
                    return Results.Forbid();
                }

                try
                {
                    // Cette méthode devrait être implémentée dans le service de transactions
                    var report = await GetDailyTransactionReportAsync(
                        date ?? DateTime.UtcNow.Date,
                        transactionService);
                    
                    return Results.Ok(report);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("GetDailyTransactionReport")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

            // GET /api/transactions/reports/monthly - Rapport mensuel des transactions (Admin seulement)
            group.MapGet("/reports/monthly", async (
                [FromQuery] int? year,
                [FromQuery] int? month,
                [FromServices] ITransactionService transactionService,
                HttpContext httpContext) =>
            {
                var isAdmin = httpContext.User.IsInRole("Admin");

                if (!isAdmin)
                {
                    return Results.Forbid();
                }

                try
                {
                    // Cette méthode devrait être implémentée dans le service de transactions
                    var report = await GetMonthlyTransactionReportAsync(
                        year ?? DateTime.UtcNow.Year,
                        month ?? DateTime.UtcNow.Month,
                        transactionService);
                    
                    return Results.Ok(report);
                }
                catch (Exception ex)
                {
                    return Results.Problem(ex.Message);
                }
            })
            .WithName("GetMonthlyTransactionReport")
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError)
            .RequireAuthorization(policy => policy.RequireRole("Admin"));
        }

        // Helper methods (ces méthodes devraient être implémentées dans le service de transactions)
        private static async Task<TransactionResponse> GetTransactionByIdAsync(
            Guid id, 
            ITransactionService transactionService, 
            string userId)
        {
            // Cette méthode devrait être implémentée dans le service de transactions
            // Pour l'exemple, on lance une exception NotImplementedException
            throw new NotImplementedException("Cette méthode doit être implémentée dans le service de transactions");
        }

        private static async Task<PagedResponse<TransactionResponse>> GetSuspiciousTransactionsAsync(
            DateTime startDate, 
            DateTime endDate, 
            ITransactionService transactionService)
        {
            // Cette méthode devrait être implémentée dans le service de transactions
            // Pour l'exemple, on lance une exception NotImplementedException
            throw new NotImplementedException("Cette méthode doit être implémentée dans le service de transactions");
        }

        private static async Task<object> GetDailyTransactionReportAsync(
            DateTime date, 
            ITransactionService transactionService)
        {
            // Cette méthode devrait être implémentée dans le service de transactions
            // Pour l'exemple, on lance une exception NotImplementedException
            throw new NotImplementedException("Cette méthode doit être implémentée dans le service de transactions");
        }

        private static async Task<object> GetMonthlyTransactionReportAsync(
            int year, 
            int month, 
            ITransactionService transactionService)
        {
            // Cette méthode devrait être implémentée dans le service de transactions
            // Pour l'exemple, on lance une exception NotImplementedException
            throw new NotImplementedException("Cette méthode doit être implémentée dans le service de transactions");
        }
    }