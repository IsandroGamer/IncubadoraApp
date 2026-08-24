using System;

namespace IncubadoraApp.Models;

public class ModelFase
{
    public uint Id { get; set; }
    public uint DispositivoId { get; set; }
    public string Tipo { get; set; } = "incubacao"; // 'incubacao','eclosao','pos_eclosao'
    public string? Nome { get; set; }
    public DateTime DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public string Estado { get; set; } = "ativa"; // 'ativa','terminada'
}