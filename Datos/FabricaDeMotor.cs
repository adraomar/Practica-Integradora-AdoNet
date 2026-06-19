namespace PracticaIntegradora.Datos;

public static class FabricaDeMotor
{
    public static IAccesoDatos Crear(Motor motor)
    {
        return motor switch
        {
            Motor.SqlServer => new AccesoSqlServer(),
            Motor.Postgres => new AccesoPostgres(),
            Motor.MySql => new AccesoMySql(),
            _ => throw new Exception("Motor inválido")
        };
    }
}