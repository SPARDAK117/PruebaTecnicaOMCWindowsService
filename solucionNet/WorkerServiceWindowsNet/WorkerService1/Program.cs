using Data;  // Asegúrate de que este using apunta correctamente a tu namespace de la capa de datos
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()  // Esto asegura que el servicio funcione como un servicio de Windows
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no se encuentra en el archivo de configuración.");
        }

        // Registrar la cadena de conexión como un servicio singleton
        services.AddSingleton(connectionString);

        // Registrar las clases de la capa de datos como servicios
        services.AddSingleton<DatTickets>(provider =>
        {
            return new DatTickets(connectionString);
        });
        services.AddSingleton<DatResumen>();

        // Registrar el Worker
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();