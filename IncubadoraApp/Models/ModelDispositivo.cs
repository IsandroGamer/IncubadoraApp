using System;

namespace IncubadoraApp.Models;

public class ModelDispositivo
{
    public uint Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Tipo { get; set; } = "incubadora";
    public string NumeroSerie { get; set; } = string.Empty;
    public string? Localizacao { get; set; }
    public string Estado { get; set; } = "desligado"; // 'ligado','desligado','erro','ciclo_concluido'
    public DateTime? UltimaLeituraEm { get; set; }

    // Módulo de Display
    public bool DisplayPresente { get; set; } = true;
    public string DisplayEstado { get; set; } = "desconhecido"; // 'ok','falha','desconhecido'
    public DateTime? DisplayUltimaAuth { get; set; }

    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}