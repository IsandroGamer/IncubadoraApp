using System;

namespace IncubadoraApp.Models;

public class ModelUtilizador
{
    public uint Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Contacto { get; set; }
    public string CredenciaisHash { get; set; } = string.Empty;
    public string Papel { get; set; } = "avicultor"; // 'avicultor' ou 'administrador'
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}