namespace RDS.ExpenseTracker.Domain.Dtos;

/// <summary>
/// Disposizione delle card della pagina Analisi scelta dall'utente: ordine = ordine della lista.
/// Il catalogo delle card (id e larghezze ammesse per ciascuna) è definito dal frontend.
/// </summary>
public class AnalyticsLayoutDto
{
    public List<AnalyticsCardLayoutDto> Cards { get; set; } = [];
}

public class AnalyticsCardLayoutDto
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Uno fra <c>third</c>, <c>half</c>, <c>twoThirds</c>, <c>full</c>.</summary>
    public string Width { get; set; } = string.Empty;
}
