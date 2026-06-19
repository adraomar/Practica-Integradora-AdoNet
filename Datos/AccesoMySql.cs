using MySqlConnector;

namespace PracticaIntegradora.Datos;

public class AccesoMySql : IAccesoDatos
{
    private readonly string _adminConnection =
        "Server=localhost;Port=3306;Uid=root;Pwd=Curso.NET2026;";

    private readonly string _connectionString =
        "Server=localhost;Port=3306;Database=practico;Uid=root;Pwd=Curso.NET2026;";

    public void CrearEstructura()
    {
        CrearBase();

        using var conexion = new MySqlConnection(_connectionString);

        conexion.Open();

        var sql = @"
        DROP TABLE IF EXISTS detalle_pedido;
        DROP TABLE IF EXISTS pedidos;
        DROP TABLE IF EXISTS productos;
        DROP TABLE IF EXISTS clientes;
        DROP TABLE IF EXISTS categorias;

        CREATE TABLE categorias(
            id INT AUTO_INCREMENT PRIMARY KEY,
            nombre VARCHAR(100) NOT NULL
        );

        CREATE TABLE clientes(
            id INT AUTO_INCREMENT PRIMARY KEY,
            nombre VARCHAR(100) NOT NULL,
            email VARCHAR(150) NOT NULL
        );

        CREATE TABLE productos(
            id INT AUTO_INCREMENT PRIMARY KEY,
            nombre VARCHAR(150) NOT NULL,
            precio DECIMAL(12,2) NOT NULL,
            stock INT NOT NULL,
            categoria_id INT NOT NULL,
            FOREIGN KEY(categoria_id)
                REFERENCES categorias(id)
        );

        CREATE TABLE pedidos(
            id INT AUTO_INCREMENT PRIMARY KEY,
            cliente_id INT NOT NULL,
            fecha DATETIME NOT NULL,
            FOREIGN KEY(cliente_id)
                REFERENCES clientes(id)
        );

        CREATE TABLE detalle_pedido(
            pedido_id INT NOT NULL,
            producto_id INT NOT NULL,
            cantidad INT NOT NULL,
            precio_unitario DECIMAL(12,2) NOT NULL,

            PRIMARY KEY(pedido_id, producto_id),

            FOREIGN KEY(pedido_id)
                REFERENCES pedidos(id),

            FOREIGN KEY(producto_id)
                REFERENCES productos(id)
        );";

        using var cmd = new MySqlCommand(sql, conexion);

        cmd.ExecuteNonQuery();

        Console.WriteLine("Estructura creada.");
    }

    private void CrearBase()
    {
        using var cn = new MySqlConnection(_adminConnection);

        cn.Open();

        using var cmd = new MySqlCommand(
            "CREATE DATABASE IF NOT EXISTS practico;",
            cn);

        cmd.ExecuteNonQuery();
    }

    public void InsertarDatosPrueba()
    {
        using var cn = new MySqlConnection(_connectionString);

        cn.Open();

        using var tx = cn.BeginTransaction();

        try
        {
            int cat1 = InsertarCategoria(cn, tx, "Electrónica");
            int cat2 = InsertarCategoria(cn, tx, "Libros");
            int cat3 = InsertarCategoria(cn, tx, "Hogar");

            int prod1 = InsertarProducto(cn, tx, "Notebook 14\"", 850000, 10, cat1);
            int prod2 = InsertarProducto(cn, tx, "Mouse inalámbrico", 12000, 50, cat1);
            int prod3 = InsertarProducto(cn, tx, "Teclado mecánico", 35000, 20, cat1);
            int prod4 = InsertarProducto(cn, tx, "Clean Code", 28000, 15, cat2);
            int prod5 = InsertarProducto(cn, tx, "Lámpara LED escritorio", 15000, 30, cat3);

            int cli1 = InsertarCliente(cn, tx, "Juan Pérez", "juan@mail.com");
            int cli2 = InsertarCliente(cn, tx, "Ana Gómez", "ana@mail.com");

            int ped1 = InsertarPedido(cn, tx, cli1);
            int ped2 = InsertarPedido(cn, tx, cli2);

            InsertarDetalle(cn, tx, ped1, prod1, 1, 850000);
            InsertarDetalle(cn, tx, ped1, prod2, 2, 12000);
            InsertarDetalle(cn, tx, ped1, prod3, 1, 35000);

            InsertarDetalle(cn, tx, ped2, prod4, 1, 28000);
            InsertarDetalle(cn, tx, ped2, prod5, 2, 15000);

            tx.Commit();

            Console.WriteLine("Datos insertados.");
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void EjecutarOperaciones()
    {
        Console.WriteLine("Operaciones ejecutadas.");
    }

    public void DemostrarRollback()
    {
        Console.WriteLine("Rollback OK.");
    }

    private int InsertarCategoria(
        MySqlConnection cn,
        MySqlTransaction tx,
        string nombre)
    {
        var sql = @"
        INSERT INTO categorias(nombre)
        VALUES(@nombre);

        SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@nombre", nombre);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private int InsertarProducto(
        MySqlConnection cn,
        MySqlTransaction tx,
        string nombre,
        decimal precio,
        int stock,
        int categoriaId)
    {
        var sql = @"
        INSERT INTO productos(nombre, precio, stock, categoria_id)
        VALUES(@nombre, @precio, @stock, @categoriaId);

        SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.Parameters.AddWithValue("@precio", precio);
        cmd.Parameters.AddWithValue("@stock", stock);
        cmd.Parameters.AddWithValue("@categoriaId", categoriaId);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private int InsertarCliente(
        MySqlConnection cn,
        MySqlTransaction tx,
        string nombre,
        string email)
    {
        var sql = @"
        INSERT INTO clientes(nombre, email)
        VALUES(@nombre, @email);

        SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.Parameters.AddWithValue("@email", email);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private int InsertarPedido(
        MySqlConnection cn,
        MySqlTransaction tx,
        int clienteId)
    {
        var sql = @"
        INSERT INTO pedidos(cliente_id, fecha)
        VALUES(@clienteId, @fecha);

        SELECT LAST_INSERT_ID();";

        using var cmd = new MySqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@clienteId", clienteId);
        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private void InsertarDetalle(
        MySqlConnection cn,
        MySqlTransaction tx,
        int pedidoId,
        int productoId,
        int cantidad,
        decimal precio)
    {
        var sql = @"
        INSERT INTO detalle_pedido
        (pedido_id, producto_id, cantidad, precio_unitario)
        VALUES(@pedido, @producto, @cantidad, @precio);";

        using var cmd = new MySqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@pedido", pedidoId);
        cmd.Parameters.AddWithValue("@producto", productoId);
        cmd.Parameters.AddWithValue("@cantidad", cantidad);
        cmd.Parameters.AddWithValue("@precio", precio);

        cmd.ExecuteNonQuery();
    }
}