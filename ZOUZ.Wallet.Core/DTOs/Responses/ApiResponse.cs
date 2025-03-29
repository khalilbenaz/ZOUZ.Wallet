namespace ZOUZ.Wallet.Core.DTOs.Responses;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; }

    public ApiResponse(bool success = true, string message = null)
    {
        Success = success;
        Message = message;
        Errors = new List<string>();
    }

    public ApiResponse(T data, bool success = true, string message = null)
    {
        Success = success;
        Message = message;
        Data = data;
        Errors = new List<string>();
    }

    public static ApiResponse<T> SuccessResponse(T data, string message = "Opération réussie")
    {
        return new ApiResponse<T>(data, true, message);
    }

    public static ApiResponse<T> ErrorResponse(string message, List<string> errors = null)
    {
        var response = new ApiResponse<T>(false, message);
        if (errors != null)
        {
            response.Errors = errors;
        }
        return response;
    }
}