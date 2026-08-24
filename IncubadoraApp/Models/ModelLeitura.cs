using System;
namespace IncubadoraApp.Models;

public class ModelLeitura
{
    public ulong Id { get; set; }
    public uint DispositivoId { get; set; }
    public uint? FaseId { get; set; }
    public byte Dia { get; set; }
    public string FaseNome { get; set; } = string.Empty;
    public decimal? Temperatura { get; set; }
    public decimal? Humidade { get; set; }
    public string EstadoTemperatura { get; set; } = "desconhecido";
    public string EstadoHumidade { get; set; } = "desconhecido";
    public bool RotacaoAtiva { get; set; }
    public string Alerta { get; set; } = "nenhum";
    public DateTime DataHora { get; set; }
}