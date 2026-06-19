namespace PracticaIntegradora.Datos;

public static class FabricaDeMotor
{
    public static IAccesoDatos Crear(Motor motor)
    {
        return motor switch
        {
            Motor.Postgres => new AccesoPostgres(),
            Motor.SqlServer => new AccesoSqlServer(),
            Motor.MySql => new AccesoMySql(),
            _ => throw new ArgumentException("Motor inválido")
        };
    }
}