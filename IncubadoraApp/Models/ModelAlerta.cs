using System;

namespace IncubadoraApp.Models;

public class ModelAlerta
{
    public uint Id { get; set; }
    public uint DispositivoId { get; set; }
    public ulong? LeituraId { get; set; }
    public string Tipo { get; set; } = string.Empty; // ex: 'temperatura_alta', 'humidade_baixa'
    public string Gravidade { get; set; } = "media"; // 'baixa','media','alta','critica'
    public string Mensagem { get; set; } = string.Empty;
    public string Estado { get; set; } = "ativo"; // 'ativo','resolvido'
    public DateTime DataHora { get; set; }
    public uint? ResolvidoPor { get; set; }
    public DateTime? ResolvidoEm { get; set; }
}