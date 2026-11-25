using MaterialManagement.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MaterialManagement.Api.Controllers;

/// <summary>Maps service exceptions to HTTP responses; the web app shows a 400's body text as the error.</summary>
public class ErrorFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        context.Result = context.Exception switch
        {
            NotFoundException => new NotFoundResult(),
            BadRequestException e => new BadRequestObjectResult(e.Message),
            _ => null,
        };
        context.ExceptionHandled = context.Result != null;
    }
}
