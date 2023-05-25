using ConsumerWorkerService.Consumer;
using MassTransit;

var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddMassTransit(configure =>
                    {
                        configure.AddConsumer<MensagemConsumer>();

                        configure.UsingRabbitMq((context, cfg) =>
                        {
                            cfg.Host(new Uri("rabbitmq://localhost"), host =>
                            {
                                host.Username("guest");
                                host.Password("guest");
                            });

                            cfg.ConfigureEndpoints(context);
                        });
                    });

                    services.AddMassTransitHostedService();
                })
                .Build();

await host.RunAsync();
