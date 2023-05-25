using MassTransit;
using MassTransitExchange;

namespace ConsumerWorkerService.Consumer
{
    public class MensagemConsumer : IConsumer<MensagemEntity>
    {
        public Task Consume(ConsumeContext<MensagemEntity> context)
        {
            var message = context.Message;
            
            // Aqui entra a regra após o consumo da mensagem...

            Console.WriteLine($"Received message {message.IdMensagem}: {message.Descricao}.");

            return Task.CompletedTask;
        }
    }
}
