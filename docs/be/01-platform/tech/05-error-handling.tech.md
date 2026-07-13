# 05 - Error handling (Tecnico)

## Pattern
I servizi ritornano `FluentResults.Result`/`Result<T>` invece di lanciare eccezioni per errori di business. `ApiControllerBase.ExecuteAsync` scompone il risultato: successo → `200 OK`; fallimento → mappa `DomainResultError.Kind` (da `Domain/Common/DomainResultErrors.cs`) su una `DomainException`, catturata da `ExceptionHandlingMiddleware` e tradotta in `ProblemDetails`.

## Mappatura eccezione → status HTTP (`Domain/Exceptions/DomainExceptions.cs`)
| Eccezione | Status |
|---|---|
| `BadRequestDomainException` | 400 |
| `ValidationDomainException` | 422 |
| `UnauthorizedDomainException` | 401 |
| `ForbiddenDomainException` | 403 |
| `NotFoundDomainException` | 404 |
| `ConflictDomainException` | 409 |

Qualunque altra eccezione non gestita → `500 Internal Server Error` con messaggio generico.

## Helper
`DomainErrors` (in `Domain/Common/DomainResultErrors.cs`) fornisce factory comuni: `InvalidId`, `NotFound`, `Required`, `Conflict`, `BadRequest`.
