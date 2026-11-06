namespace RDS.ExpenseTracker.Domain.Dtos;

// Profilo dell'utente autenticato esposto al frontend (GET user/me).
public class CurrentUserDto
{
    public string Email { get; set; } = string.Empty;
    public bool IsDemo { get; set; }
    public bool IsAdmin { get; set; }
}
