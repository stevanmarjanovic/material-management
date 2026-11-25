namespace MaterialManagement.Api.Services;

/// <summary>Thrown by services; <c>ErrorFilter</c> turns it into a 404.</summary>
public class NotFoundException() : Exception("Not found.");

/// <summary>Thrown by services; <c>ErrorFilter</c> turns it into a 400 with the message as the body.</summary>
public class BadRequestException(string message) : Exception(message)
{
    public static void ThrowIf(string? error)
    {
        if (error != null) throw new BadRequestException(error);
    }
}
