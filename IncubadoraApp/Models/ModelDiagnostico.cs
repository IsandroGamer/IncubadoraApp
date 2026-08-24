using System;
namespace IncubadoraApp.Models;

public class ModelDiagnostico
{
    public uint Id { get; set; }
    public uint DispositivoId { get; set; }
    public uint? FaseId { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public decimal? Confianca { get; set; }
    public string? Recomendacao { get; set; }
    public DateTime DataHora { get; set; }
    public uint? GeradoPor { get; set; }
}