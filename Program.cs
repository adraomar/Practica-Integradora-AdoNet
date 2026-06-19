using PracticaIntegradora.Datos;

Motor motor;

if (args.Length > 0)
{
    motor = args[0].ToLower() switch
    {
        "postgres" => Motor.Postgres,
        "sqlserver" => Motor.SqlServer,
        "mysql" => Motor.MySql,
        _ => throw new Exception("Motor inválido")
    };
}
else
{
    Console.WriteLine("1 - PostgreSQL");
    Console.WriteLine("2 - SQL Server");
    Console.WriteLine("3 - MySQL");

    Console.Write("Seleccione motor: ");

    motor = (Motor)int.Parse(Console.ReadLine()!);
}

IAccesoDatos acceso = FabricaDeMotor.Crear(motor);

Console.WriteLine($"\n===== MOTOR: {motor} =====");

acceso.CrearEstructura();
acceso.InsertarDatosPrueba();
acceso.EjecutarOperaciones();
acceso.DemostrarRollback();

Console.WriteLine($"\n===== FIN ({motor}) =====");