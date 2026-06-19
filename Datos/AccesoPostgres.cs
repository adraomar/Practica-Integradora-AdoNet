using Npgsql;

namespace PracticaIntegradora.Datos;

public class AccesoPostgres : IAccesoDatos
{
    private readonly string _adminConnection =
        "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres";

    private readonly string _connectionString =
        "Host=localhost;Port=5432;Database=practico;Username=postgres;Password=postgres";

    public void CrearEstructura()
    {
        CrearBase();

        using var conexion = new NpgsqlConnection(_connectionString);

        conexion.Open();

        var sql = @"
        DROP TABLE IF EXISTS detalle_pedido;
        DROP TABLE IF EXISTS pedidos;
        DROP TABLE IF EXISTS productos;
        DROP TABLE IF EXISTS clientes;
        DROP TABLE IF EXISTS categorias;

        CREATE TABLE categorias(
            id SERIAL PRIMARY KEY,
            nombre VARCHAR(100) NOT NULL
        );

        CREATE TABLE clientes(
            id SERIAL PRIMARY KEY,
            nombre VARCHAR(100) NOT NULL,
            email VARCHAR(150) NOT NULL
        );

        CREATE TABLE productos(
            id SERIAL PRIMARY KEY,
            nombre VARCHAR(150) NOT NULL,
            precio NUMERIC(12,2) NOT NULL,
            stock INT NOT NULL,
            categoria_id INT NOT NULL
                REFERENCES categorias(id)
        );

        CREATE TABLE pedidos(
            id SERIAL PRIMARY KEY,
            cliente_id INT NOT NULL
                REFERENCES clientes(id),
            fecha TIMESTAMP NOT NULL
        );

        CREATE TABLE detalle_pedido(
            pedido_id INT NOT NULL,
            producto_id INT NOT NULL,
            cantidad INT NOT NULL,
            precio_unitario NUMERIC(12,2) NOT NULL,

            PRIMARY KEY(pedido_id, producto_id),

            FOREIGN KEY(pedido_id)
                REFERENCES pedidos(id),

            FOREIGN KEY(producto_id)
                REFERENCES productos(id)
        );";

        using var cmd = new NpgsqlCommand(sql, conexion);

        cmd.ExecuteNonQuery();

        Console.WriteLine("Estructura creada.");
    }

    private void CrearBase()
    {
        using var cn = new NpgsqlConnection(_adminConnection);

        cn.Open();

        using var check = new NpgsqlCommand(
            "SELECT 1 FROM pg_database WHERE datname = 'practico'",
            cn);

        var existe = check.ExecuteScalar();

        if (existe == null)
        {
            using var create = new NpgsqlCommand(
                "CREATE DATABASE practico",
                cn);

            create.ExecuteNonQuery();
        }
    }

    public void InsertarDatosPrueba()
    {
        using var cn = new NpgsqlConnection(_connectionString);

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
        using var cn = new NpgsqlConnection(_connectionString);

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

        using var cn = new NpgsqlConnection(_connectionString);

        cn.Open();

        decimal precioAntes = ObtenerPrecio(cn, 1);

        Console.WriteLine($"Precio del producto #1 ANTES: ${precioAntes:0.00}");

        using var tx = cn.BeginTransaction();

        try
        {
            using var cmd = new NpgsqlCommand(
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

    private int InsertarCategoria(
        NpgsqlConnection cn,
        NpgsqlTransaction tx,
        string nombre)
    {
        var sql = @"
        INSERT INTO categorias(nombre)
        VALUES(@nombre)
        RETURNING id;";

        using var cmd = new NpgsqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@nombre", nombre);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private int InsertarProducto(
        NpgsqlConnection cn,
        NpgsqlTransaction tx,
        string nombre,
        decimal precio,
        int stock,
        int categoriaId)
    {
        var sql = @"
        INSERT INTO productos(nombre, precio, stock, categoria_id)
        VALUES(@nombre, @precio, @stock, @categoriaId)
        RETURNING id;";

        using var cmd = new NpgsqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.Parameters.AddWithValue("@precio", precio);
        cmd.Parameters.AddWithValue("@stock", stock);
        cmd.Parameters.AddWithValue("@categoriaId", categoriaId);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private int InsertarCliente(
        NpgsqlConnection cn,
        NpgsqlTransaction tx,
        string nombre,
        string email)
    {
        var sql = @"
        INSERT INTO clientes(nombre,email)
        VALUES(@nombre,@email)
        RETURNING id;";

        using var cmd = new NpgsqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.Parameters.AddWithValue("@email", email);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private int InsertarPedido(
        NpgsqlConnection cn,
        NpgsqlTransaction tx,
        int clienteId)
    {
        var sql = @"
        INSERT INTO pedidos(cliente_id,fecha)
        VALUES(@clienteId,@fecha)
        RETURNING id;";

        using var cmd = new NpgsqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@clienteId", clienteId);
        cmd.Parameters.AddWithValue("@fecha", DateTime.Now);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private void InsertarDetalle(
        NpgsqlConnection cn,
        NpgsqlTransaction tx,
        int pedidoId,
        int productoId,
        int cantidad,
        decimal precio)
    {
        var sql = @"
        INSERT INTO detalle_pedido
        (pedido_id, producto_id, cantidad, precio_unitario)
        VALUES(@pedido,@producto,@cantidad,@precio);";

        using var cmd = new NpgsqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@pedido", pedidoId);
        cmd.Parameters.AddWithValue("@producto", productoId);
        cmd.Parameters.AddWithValue("@cantidad", cantidad);
        cmd.Parameters.AddWithValue("@precio", precio);

        cmd.ExecuteNonQuery();
    }

    private void ConsultarProductos(
    NpgsqlConnection cn,
    NpgsqlTransaction tx)
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

        using var cmd = new NpgsqlCommand(sql, cn, tx);

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

    private void MostrarPedido(
    NpgsqlConnection cn,
    NpgsqlTransaction tx,
    int pedidoId)
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

        using var cmd = new NpgsqlCommand(sql, cn, tx);

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

        using var totalCmd = new NpgsqlCommand(sql, cn, tx);

        totalCmd.Parameters.AddWithValue("@pedidoId", pedidoId);

        decimal total = Convert.ToDecimal(totalCmd.ExecuteScalar());

        Console.WriteLine($"TOTAL pedido #{pedidoId}: ${total:0.00}");
    }

    private void ActualizarPrecios(
    NpgsqlConnection cn,
    NpgsqlTransaction tx,
    int categoriaId)
    {
        var sql = @"
        UPDATE productos
        SET precio = precio * 1.10
        WHERE categoria_id = @categoriaId";

        using var cmd = new NpgsqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@categoriaId", categoriaId);

        int filas = cmd.ExecuteNonQuery();

        Console.WriteLine(
            $"[U1] Subí 10% precios de categoría #{categoriaId} -> {filas} filas.");
    }

    private void BorrarDetalle(
    NpgsqlConnection cn,
    NpgsqlTransaction tx,
    int pedidoId,
    int productoId)
    {
        var sql = @"
        DELETE FROM detalle_pedido
        WHERE pedido_id = @pedidoId
          AND producto_id = @productoId";

        using var cmd = new NpgsqlCommand(sql, cn, tx);

        cmd.Parameters.AddWithValue("@pedidoId", pedidoId);
        cmd.Parameters.AddWithValue("@productoId", productoId);

        int filas = cmd.ExecuteNonQuery();

        Console.WriteLine(
            $"[D1] Borré línea (pedido {pedidoId}, producto {productoId}) -> {filas} filas.");
    }

    private decimal ObtenerPrecio(
    NpgsqlConnection cn,
    int id)
    {
        using var cmd = new NpgsqlCommand(
            "SELECT precio FROM productos WHERE id = @id",
            cn);

        cmd.Parameters.AddWithValue("@id", id);

        return Convert.ToDecimal(cmd.ExecuteScalar());
    }
}