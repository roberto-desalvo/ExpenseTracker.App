namespace RDS.ExpenseTracker.Domain.Dtos;

/// <summary>
/// Preferenze di aspetto dell'utente (Impostazioni → Aspetto). Il catalogo degli stili è definito dal frontend.
/// </summary>
public class AppearancePreferencesDto
{
    /// <summary>Id dello stile (es. <c>aurora</c>).</summary>
    public string Preset { get; set; } = string.Empty;

    /// <summary>Uno fra <c>light</c>, <c>dark</c>, <c>system</c>.</summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>Uno fra <c>sidebar</c>, <c>topbar</c>.</summary>
    public string Nav { get; set; } = string.Empty;
}
