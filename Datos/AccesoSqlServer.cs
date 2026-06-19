using Microsoft.Data.SqlClient;

namespace PracticaIntegradora.Datos;

public class AccesoSqlServer : IAccesoDatos
{
    private readonly string _adminConnection =
        "Server=localhost,1433;Database=master;User Id=sa;Password=Curso.NET2026;TrustServerCertificate=True";

    private readonly string _connectionString =
        "Server=localhost,1433;Database=practico;User Id=sa;Password=Curso.NET2026;TrustServerCertificate=True";

    public void CrearEstructura()
    {
        using (var cn = new SqlConnection(_adminConnection))
        {
            cn.Open();

            var crearDb = @"
            IF DB_ID('practico') IS NULL
                CREATE DATABASE practico";

            using var query = new SqlCommand(crearDb, cn);

            query.ExecuteNonQuery();
        }

        using var conexion = new SqlConnection(_connectionString);

        conexion.Open();

        var sql = @"
        DROP TABLE IF EXISTS detalle_pedido;
        DROP TABLE IF EXISTS pedidos;
        DROP TABLE IF EXISTS productos;
        DROP TABLE IF EXISTS clientes;
        DROP TABLE IF EXISTS categorias;

        CREATE TABLE categorias(
            id INT IDENTITY(1,1) PRIMARY KEY,
            nombre VARCHAR(100) NOT NULL
        );

        CREATE TABLE clientes(
            id INT IDENTITY(1,1) PRIMARY KEY,
            nombre VARCHAR(100) NOT NULL,
            email VARCHAR(150) NOT NULL
        );

        CREATE TABLE productos(
            id INT IDENTITY(1,1) PRIMARY KEY,
            nombre VARCHAR(150) NOT NULL,
            precio DECIMAL(12,2) NOT NULL,
            stock INT NOT NULL,
            categoria_id INT NOT NULL
                FOREIGN KEY REFERENCES categorias(id)
        );

        CREATE TABLE pedidos(
            id INT IDENTITY(1,1) PRIMARY KEY,
            cliente_id INT NOT NULL
                FOREIGN KEY REFERENCES clientes(id),
            fecha DATETIME NOT NULL
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

        using var cmd = new SqlCommand(sql, conexion);

        cmd.ExecuteNonQuery();

        Console.WriteLine("Estructura creada.");
    }

    public void InsertarDatosPrueba()
    {
        using var cn = new SqlConnection(_connectionString);

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
        using var cn = new SqlConnection(_connectionString);

        cn.Open();

        using var tx = cn.BeginTransaction();

        try
        {
            Console.WriteLine("\nRF4 — Ejecutar operaciones");

            ConsultarProductos(cn, tx);

            MostrarPedido(cn, tx, 1);

            ActualizarPrecios(cn, tx, 1);

            BorrarDetalle(cn, tx, 1, 2);

            tx.Commit();

            Console.WriteLine("Operaciones confirmadas (commit).");
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void DemostrarRollback()
    {
        Console.WriteLine("\nRF5 — Demostrar rollback");

        using var cn = new SqlConnection(_connectionString);

        cn.Open();

        decimal precioAntes = ObtenerPrecio(cn, 1);

        Console.WriteLine($"Precio del producto #1 ANTES: ${precioAntes:0.00}");

        using var tx = cn.BeginTransaction();

        try
        {
            using var cmd = new SqlCommand(
                "UPDATE productos SET precio = 1 WHERE id = 1",
                cn,
                tx);

            cmd.ExecuteNonQuery();

            Console.WriteLine(
                "UPDATE aplicado (precio -> 1) dentro de la transacción.");

            throw new Exception("Error simulado: algo salió mal.");
        }
        catch (Exception ex)
        {
            tx.Rollback();

            Console.WriteLine(
                $"Excepción capturada -> ROLLBACK. ({ex.Message})");
        }

        decimal precioDespues = ObtenerPrecio(cn, 1);

        Console.WriteLine($"Precio del producto #1 DESPUÉS: ${precioDespues:0.00}");

        if (precioAntes == precioDespues)
        {
            Console.WriteLine(
                "OK: el rollback funcionó, el dato NO cambió.");
        }
    }

    private int InsertarCategoria(SqlConnection cn, SqlTransaction tx, string nombre)
    {
        var sql = @"
        INSERT INTO categorias(nombre)
        VALUES(@nombre);

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var cmd = new SqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@nombre", nombre);

        return (int)cmd.ExecuteScalar()!;
    }

    private int InsertarProducto(
        SqlConnection cn,
        SqlTransaction tx,
        string nombre,
        decimal precio,
        int stock,
        int categoriaId)
    {
        var sql = @"
        INSERT INTO productos(nombre, precio, stock, categoria_id)
        VALUES(@nombre,@precio,@stock,@categoriaId);

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var cmd = new SqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.Parameters.AddWithValue("@precio", precio);
        cmd.Parameters.AddWithValue("@stock", stock);
        cmd.Parameters.AddWithValue("@categoriaId", categoriaId);

        return (int)cmd.ExecuteScalar()!;
    }

    private int InsertarCliente(
        SqlConnection cn,
        SqlTransaction tx,
        string nombre,
        string email)
    {
        var sql = @"
        INSERT INTO clientes(nombre,email)
        VALUES(@nombre,@email);

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var cmd = new SqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.Parameters.AddWithValue("@email", email);

        return (int)cmd.ExecuteScalar()!;
    }

    private int InsertarPedido(
        SqlConnection cn,
        SqlTransaction tx,
        int clienteId)
    {
        var sql = @"
        INSERT INTO pedidos(cliente_id,fecha)
        VALUES(@clienteId,@fecha);

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

        using var cmd = new SqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@clienteId", clienteId);
        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);

        return (int)cmd.ExecuteScalar()!;
    }

    private void InsertarDetalle(
        SqlConnection cn,
        SqlTransaction tx,
        int pedidoId,
        int productoId,
        int cantidad,
        decimal precio)
    {
        var sql = @"
        INSERT INTO detalle_pedido
        VALUES(@pedido,@producto,@cantidad,@precio)";

        using var cmd = new SqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@pedido", pedidoId);
        cmd.Parameters.AddWithValue("@producto", productoId);
        cmd.Parameters.AddWithValue("@cantidad", cantidad);
        cmd.Parameters.AddWithValue("@precio", precio);

        cmd.ExecuteNonQuery();
    }

    private void ConsultarProductos(SqlConnection cn, SqlTransaction tx)
    {
        Console.WriteLine("\n[C1] Productos con su categoría:");

        var sql = @"
        SELECT p.id,
               p.nombre,
               p.precio,
               c.nombre
        FROM productos p
        INNER JOIN categorias c
            ON p.categoria_id = c.id";

        using var cmd = new SqlCommand(sql, cn, tx);

        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            Console.WriteLine(
                $"#{dr.GetInt32(0)} " +
                $"{dr.GetString(1)} — " +
                $"${dr.GetDecimal(2):0.00} " +
                $"[{dr.GetString(3)}]");
        }
    }

    private void MostrarPedido(SqlConnection cn, SqlTransaction tx, int pedidoId)
    {
        Console.WriteLine($"\n[C2] Detalle y total del pedido #{pedidoId}:");

        var sql = @"
        SELECT p.nombre,
               dp.cantidad,
               dp.precio_unitario,
               dp.cantidad * dp.precio_unitario
        FROM detalle_pedido dp
        INNER JOIN productos p
            ON p.id = dp.producto_id
        WHERE dp.pedido_id = @pedidoId";

        using var cmd = new SqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@pedidoId", pedidoId);

        using var dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            Console.WriteLine(
                $"{dr.GetString(0)} x{dr.GetInt32(1)} @ " +
                $"${dr.GetDecimal(2):0.00} = " +
                $"${dr.GetDecimal(3):0.00}");
        }

        dr.Close();

        sql = @"
        SELECT SUM(cantidad * precio_unitario)
        FROM detalle_pedido
        WHERE pedido_id = @pedidoId";

        using var totalCmd = new SqlCommand(sql, cn, tx);

        totalCmd.Parameters.AddWithValue("@pedidoId", pedidoId);

        decimal total = Convert.ToDecimal(totalCmd.ExecuteScalar());

        Console.WriteLine($"TOTAL pedido #{pedidoId}: ${total:0.00}");
    }

    private void ActualizarPrecios(
    SqlConnection cn,
    SqlTransaction tx,
    int categoriaId)
    {
        var sql = @"
        UPDATE productos
        SET precio = precio * 1.10
        WHERE categoria_id = @categoriaId";

        using var cmd = new SqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@categoriaId", categoriaId);

        int filas = cmd.ExecuteNonQuery();

        Console.WriteLine(
            $"[U1] Subí 10% precios de categoría #{categoriaId} -> {filas} filas.");
    }

    private void BorrarDetalle(
    SqlConnection cn,
    SqlTransaction tx,
    int pedidoId,
    int productoId)
    {
        var sql = @"
        DELETE FROM detalle_pedido
        WHERE pedido_id = @pedidoId
        AND producto_id = @productoId";

        using var cmd = new SqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@pedidoId", pedidoId);
        cmd.Parameters.AddWithValue("@productoId", productoId);

        int filas = cmd.ExecuteNonQuery();

        Console.WriteLine(
            $"[D1] Borré línea (pedido {pedidoId}, producto {productoId}) -> {filas} filas.");
    }

    private decimal ObtenerPrecio(SqlConnection cn, int id)
    {
        using var cmd = new SqlCommand(
            "SELECT precio FROM productos WHERE id = @id",
            cn);

        cmd.Parameters.AddWithValue("@id", id);

        return Convert.ToDecimal(cmd.ExecuteScalar());
    }
}