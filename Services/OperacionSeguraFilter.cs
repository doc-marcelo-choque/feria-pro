using System.Data;
using FeriaPro.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
namespace FeriaPro.Services;

// La transacción serializable protege las reglas que abarcan varias filas (integrantes, estados y evaluaciones).
public class OperacionSeguraFilter(AppDbContext db, ILogger<OperacionSeguraFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method)) { await next(); return; }
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var resultado = await next();
        if (resultado.Exception == null)
        {
            try { await transaccion.CommitAsync(); }
            catch (MySqlException) { await transaccion.RollbackAsync(); resultado.Result = Error(); }
            return;
        }
        await transaccion.RollbackAsync();
        if (resultado.Exception is DbUpdateException or MySqlException)
        {
            logger.LogWarning("Operación rechazada por una restricción o modificación concurrente de datos.");
            resultado.ExceptionHandled = true;
            resultado.Result = Error();
        }
    }
    private static ViewResult Error() => new() { ViewName = "~/Views/Shared/OperacionError.cshtml", StatusCode = 409 };
}
