namespace NutriRed.Services.Common;

/// <summary>
/// Encapsula el resultado de una operación de negocio que retorna datos de tipo T.
/// Permite comunicar éxitos o fallas con mensajes claros sin recurrir al lanzamiento de excepciones.
/// </summary>
public class OperationResult<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<string> Errors { get; private set; } = new();

    // Constructor privado para forzar el uso de los métodos factoría estáticos
    private OperationResult() { }

    public static OperationResult<T> Ok(T data)
    {
        return new OperationResult<T>
        {
            Success = true,
            Data = data
        };
    }

    public static OperationResult<T> Fail(string errorMessage)
    {
        return new OperationResult<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            Errors = new List<string> { errorMessage }
        };
    }

    public static OperationResult<T> Fail(List<string> errors)
    {
        return new OperationResult<T>
        {
            Success = false,
            ErrorMessage = errors.FirstOrDefault() ?? "Ocurrió un error en la operación.",
            Errors = errors
        };
    }
}

/// <summary>
/// Encapsula el resultado de una operación de negocio que no retorna datos (equivalente a void).
/// </summary>
public class OperationResult
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<string> Errors { get; private set; } = new();

    private OperationResult() { }

    public static OperationResult Ok()
    {
        return new OperationResult
        {
            Success = true
        };
    }

    public static OperationResult Fail(string errorMessage)
    {
        return new OperationResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            Errors = new List<string> { errorMessage }
        };
    }

    public static OperationResult Fail(List<string> errors)
    {
        return new OperationResult
        {
            Success = false,
            ErrorMessage = errors.FirstOrDefault() ?? "Ocurrió un error en la operación.",
            Errors = errors
        };
    }
}
