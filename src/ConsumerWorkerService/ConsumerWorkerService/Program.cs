using ConsumerWorkerService;
using ConsumerWorkerService.Consumer;
using MassTransit;
using MassTransitExchange;

var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.json", true, true);
var config = builder.Build();

IHost host = Host.CreateDefaultBuilder(args)

.UseWindowsService(options =>
{
    options.ServiceName = "Consumidor para cancelar envelope Docusign";
})

.ConfigureServices(services =>
{
    //services.SetupIocDI(); Injeção de depêndencia

    services.AddMassTransit(cfg =>
    {
        cfg.AddConsumer<MensagemConsumer>();

        cfg.UsingRabbitMq((context, rabbitMqConfiguration) =>
        {
            rabbitMqConfiguration.UseTimeout(x =>
            {
                x.Timeout = TimeSpan.FromSeconds(config.GetValue<int>("Servicos:RabbitMq:Timeout"));
            });

            rabbitMqConfiguration.Host(config.GetValue<string>("Servicos:RabbitMq:Host"), config.GetValue<string>("Servicos:RabbitMq:VHost"), x =>
            {
                var tls = config.GetValue<bool>("Servicos:RabbitMq:Tls");

                if (tls)
                {
                    x.UseSsl(s =>
                    {
                        s.Protocol = System.Security.Authentication.SslProtocols.Tls12;
                    });
                }

                x.Username(config.GetValue<string>("Servicos:RabbitMq:User"));
                x.Password(config.GetValue<string>("Servicos:RabbitMq:Password"));
            });

            rabbitMqConfiguration.ReceiveEndpoint(config["Servicos:RabbitMq:Fila"], e =>
            {
                e.SetQuorumQueue();
                e.ConfigureConsumeTopology = false;
                e.ConcurrentMessageLimit = config.GetValue<int>("Servicos:RabbitMq:MsgConsumidasEmParalelo");
                e.PrefetchCount = config.GetValue<int>("Servicos:RabbitMq:MsgConsumidasEmParalelo");
                e.ThrowOnSkippedMessages();

                e.Bind<MensagemEntity>(x =>
                {
                    x.ExchangeType = "direct";
                    x.RoutingKey = config["Servicos:RabbitMq:RoutingKey"];
                });

                // Retry: Esta função é nativa do masstransit

                e.UseMessageRetry(x =>
                {
                    int qtdRetentativas = config.GetValue<int>("Servicos:Retry:Tentativas");
                    int tempoPrimeiraRetentativa = config.GetValue<int>("Servicos:Retry:PrimeiraRetentativa");
                    int incrementoRetentativa = config.GetValue<int>("Servicos:Retry:IncrementoRetentativa");

                    x.Incremental(
                        retryLimit: qtdRetentativas,
                        initialInterval: TimeSpan.FromSeconds(tempoPrimeiraRetentativa),
                        intervalIncrement: TimeSpan.FromSeconds(incrementoRetentativa));
                });

                // Circuit Breaker:
                e.UseKillSwitch(x =>
                {
                    int porcentagemDeErros = config.GetValue<int>("Servicos:CircuitBreaker:PorcentagemDeErros");
                    int tempoCircuitoAberto = config.GetValue<int>("Servicos:CircuitBreaker:TempoAberturaMinutos");
                    int tempoTracking = config.GetValue<int>("Servicos:CircuitBreaker:TempoDeTranckingEmMinutos");
                    int activationThreshold = config.GetValue<int>("Servicos:CircuitBreaker:ActivationThreshold");

                    x.TrackingPeriod = TimeSpan.FromMinutes(tempoTracking);
                    x.TripThreshold = porcentagemDeErros;
                    x.SetActivationThreshold(activationThreshold);
                    x.SetRestartTimeout(TimeSpan.FromMinutes(tempoCircuitoAberto));
                });

                e.ConfigureConsumer<MensagemConsumer>(context);
            });
        });
    });

    services.AddHostedService<Worker>();

}).Build();

host.Run();
