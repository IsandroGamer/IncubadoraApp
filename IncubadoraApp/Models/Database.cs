using MySql.Data.MySqlClient;

namespace IncubadoraApp.Models;

public static class Database
{
    // Ajusta as tuas credenciais do MySQL aqui se necessário
    private static string connectionString = "Server=192.168.1.212;Database=incubadora_db;Uid=root;Pwd=IsaKellY1971;";

    public static MySqlConnection GetConnection()
    {
        return new MySqlConnection(connectionString);
    }
}