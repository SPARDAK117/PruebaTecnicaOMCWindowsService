using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Data;
using System.Text.RegularExpressions;
using Model;
using System.Text.Json;
using System.Text;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly DatTickets _datTickets;
    private readonly string _pendingFolderPath;
    private readonly string _processedFolderPath;
    private readonly string _logFolderPath;
    private readonly string _errorFolderPath;

    public Worker(ILogger<Worker> logger, DatTickets datTickets, IConfiguration configuration)
    {
        _logger = logger;
        _datTickets = datTickets;

        // Leer las rutas de las carpetas desde la configuración
        _pendingFolderPath = configuration["FileSettings:PendingFolderPath"];
        _processedFolderPath = configuration["FileSettings:ProcessedFolderPath"];
        _logFolderPath = configuration["FileSettings:LogFolderPath"];
        _errorFolderPath = configuration["FileSettings:ErrorFolderPath"];
    }

    //private async Task ProcesarArchivoFctAsync(string filePath)
    //{
    //    var lineas = await File.ReadAllLinesAsync(filePath);

    //    var regex = new Regex(@"^-->?(\d{6})\|(\d{3})\|(\d{8})\|(\d{6})\|(\d+)\|(\s*\d+\.\d{4})\|(\d+\.\d{2})\|");

    //    foreach (var linea in lineas)
    //    {
    //        var match = regex.Match(linea);

    //        if (match.Success && match.Groups.Count == 8)
    //        {
    //            try
    //            {
    //                //Validación por si se necesita que el formato del REGEX sea especifica en el numero de caracteres permitidos para registrar los tickets en este caso como lo dictan las instrucciones
    //                //de la prueba técnica ejemplo de cadena -->000104|081|20180405|095604|537794| 15.4500|112.00| ya que existen cadenas con numero de ticket distinto al señalado en el ejemplo descrito

    //                int ticketValue = int.Parse(match.Groups[5].Value);

    //                //if (ticketValue < 100000)
    //                //{
    //                //    throw new ArgumentException($"El valor del campo Ticket ({ticketValue}) es menor a 6 dígitos.");
    //                //}

    //                var ticket = new Model.Tickets
    //                {
    //                    Id_Tienda = match.Groups[1].Value,
    //                    Id_Registradora = match.Groups[2].Value,
    //                    FechaHora = DateTime.ParseExact(match.Groups[3].Value + match.Groups[4].Value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
    //                    Ticket = ticketValue,
    //                    Impuesto = decimal.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture),
    //                    Total = decimal.Parse(match.Groups[7].Value, CultureInfo.InvariantCulture),
    //                    FechaHora_Creacion = DateTime.Now
    //                };

    //                await _datTickets.InsertarTicketAsync(ticket);
    //            }
    //            catch (Exception ex)
    //            {
    //                _logger.LogWarning($"Error al procesar la línea: {linea} - {ex.Message}");

    //                // Evitar conflicto de nombres de archivo al renombrar y mover
    //                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
    //                var errorFileName = $"{Path.GetFileNameWithoutExtension(filePath)}_{timestamp}.fct_error";
    //                var errorFilePath = Path.Combine(_errorFolderPath, errorFileName);

    //                File.Move(filePath, errorFilePath);

    //                EscribirLog($"Archivo movido a la carpeta de errores: {errorFileName} debido a: {ex.Message}");

    //                throw;
    //            }
    //        }
    //        else
    //        {
    //            throw new FormatException($"Formato de línea no válido: {linea}");
    //        }
    //    }
    //}
    private async Task<Tickets> ProcesarArchivoFctAsync(string filePath)
    {
        var lineas = await File.ReadAllLinesAsync(filePath);

        // Regex para validar y extraer los datos del archivo .fct
        var regex = new Regex(@"^-->?(\d{6})\|(\d{3})\|(\d{8})\|(\d{6})\|(\d+)\|(\s*\d+\.\d{4})\|(\d+\.\d{2})\|");

        foreach (var linea in lineas)
        {
            var match = regex.Match(linea);

            if (match.Success && match.Groups.Count == 8)
            {
                try
                {
                    int ticketValue = int.Parse(match.Groups[5].Value);

                    var ticket = new Tickets
                    {
                        Id_Tienda = match.Groups[1].Value,
                        Id_Registradora = match.Groups[2].Value,
                        FechaHora = DateTime.ParseExact(match.Groups[3].Value + match.Groups[4].Value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
                        Ticket = ticketValue,
                        Impuesto = decimal.Parse(match.Groups[6].Value, CultureInfo.InvariantCulture),
                        Total = decimal.Parse(match.Groups[7].Value, CultureInfo.InvariantCulture),
                        FechaHora_Creacion = DateTime.Now
                    };

                    return ticket;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Error al procesar la línea: {linea} - {ex.Message}");

                    // Mover archivo a la carpeta de errores
                    var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                    var errorFileName = $"{Path.GetFileNameWithoutExtension(filePath)}_{timestamp}.fct_error";
                    var errorFilePath = Path.Combine(_errorFolderPath, errorFileName);

                    File.Move(filePath, errorFilePath);
                    EscribirLog($"Archivo movido a la carpeta de errores: {errorFileName} debido a: {ex.Message}");

                    throw;
                }
            }
            else
            {
                throw new FormatException($"Formato de línea no válido: {linea}");
            }
        }

        return null;
    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var files = Directory.GetFiles(_pendingFolderPath, "*.fct");

                foreach (var file in files)
                {
                    try
                    {
                        // Procesar el archivo .fct y crear un objeto Tickets
                        var ticket = await ProcesarArchivoFctAsync(file);
                        _logger.LogInformation("Archivo {file} procesado correctamente a las: {time}", file, DateTimeOffset.Now);

                        // Enviar el ticket a la API
                        await EnviarTicketAApiAsync(ticket);

                        // Mover el archivo a la carpeta "Procesados"
                        var processedFilePath = Path.Combine(_processedFolderPath, Path.GetFileName(file));
                        File.Move(file, processedFilePath);

                        // Escribir un log del archivo procesado
                        EscribirLog($"Archivo procesado: {file} a las {DateTimeOffset.Now}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error al procesar el archivo {file}.");

                        // Renombrar y mover el archivo a la carpeta "fct_error"
                        var errorFileName = Path.GetFileNameWithoutExtension(file) + ".fct_error";
                        var errorFilePath = Path.Combine(_errorFolderPath, errorFileName);
                        File.Move(file, errorFilePath);

                        // Escribir un log del error
                        EscribirLog($"Error procesando el archivo: {file}. Renombrado y movido a: {errorFilePath} - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar archivos en la carpeta 'Pendientes'.");
                EscribirLog($"Error general al procesar archivos en 'Pendientes' - {ex.Message}");
            }

            await Task.Delay(60000, stoppingToken);  // Espera 60 segundos antes de la próxima ejecución
        }
    }

    //private async Task<Tickets> ProcesarArchivoFctAsync(string filePath)
    //{
    //    // Aquí procesas el archivo .fct, extrayendo los datos necesarios para crear un objeto Tickets
    //    var lines = await File.ReadAllLinesAsync(filePath);

    //    // Asumiendo que solo hay una línea que necesitas procesar, o toma la primera línea
    //    var data = lines[0].Split('|'); // Asumiendo que el separador en el archivo es '|'

    //    var ticket = new Tickets
    //    {
    //        Id_Tienda = data[0],
    //        Id_Registradora = data[1],
    //        FechaHora = DateTime.ParseExact(data[2] + data[3], "yyyyMMddHHmmss", CultureInfo.InvariantCulture),
    //        Ticket = int.Parse(data[4]),
    //        Impuesto = decimal.Parse(data[5], CultureInfo.InvariantCulture),
    //        Total = decimal.Parse(data[6], CultureInfo.InvariantCulture),
    //        FechaHora_Creacion = DateTime.Now
    //    };

    //    return ticket;
    //}

    private async Task EnviarTicketAApiAsync(Tickets ticket)
    {
        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri("http://localhost:5127/");  // Reemplaza <port> con el puerto correcto de tu API
            var json = JsonSerializer.Serialize(ticket);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("api/tickets", content);

            if (!response.IsSuccessStatusCode)
            {
                // Manejar el error de forma adecuada
                _logger.LogError($"Error al enviar el ticket a la API: {response.StatusCode}");
            }
            else
            {
                _logger.LogInformation("Ticket enviado exitosamente a la API.");
            }
        }
    }

    private void EscribirLog(string mensaje)
    {
        try
        {
            var logFilePath = Path.Combine(_logFolderPath, $"log_{DateTime.Now:yyyyMMdd}.txt");
            Directory.CreateDirectory(_logFolderPath);  // Asegúrate de que la carpeta exista

            using (StreamWriter sw = new StreamWriter(logFilePath, true))
            {
                sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {mensaje}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al escribir en el log.");
        }
    }
}