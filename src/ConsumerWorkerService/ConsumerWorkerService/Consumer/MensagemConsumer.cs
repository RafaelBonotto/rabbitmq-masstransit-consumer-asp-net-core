using MassTransit;
using MassTransitExchange;

namespace ConsumerWorkerService.Consumer
{
    public class MensagemConsumer : IConsumer<MensagemEntity>
    {
        private readonly IConfiguration _config;

        public MensagemConsumer(IConfiguration config)
        {
            _config = config;
        }

        public async Task Consume(ConsumeContext<MensagemEntity> context)
        {
            try
            {
                var message = context.Message;

                // Aqui entra a regra após o consumo da mensagem...

                Console.WriteLine($"Received message {message.IdMensagem}: {message.Descricao}.");

                await context.ConsumeCompleted;
            }
            catch (Exception ex)
            {
                await RetentativaEnvioDLQAsync(context, ex);
            }
        }

        private async Task RetentativaEnvioDLQAsync(ConsumeContext<MensagemEntity> context, Exception exception)
        {
            if (MaximoRetentativasPermitidas(context))
            {
                var loggerTrace = Guid.NewGuid();

                Console.WriteLine($"ERRO MSG ENVIADA PARA FILA DLQ, LOG TRACE: {loggerTrace}");

                var msg = $"ERRO Id mensagem: {context.Message.IdMensagem}, Mensagem: {context.Message.Descricao} EXCEPTION:{exception.Message}";

                //await _logger.SalvarLogErroAsync(msg, exception, loggerTrace);
            }

            throw exception;
        }
        private bool MaximoRetentativasPermitidas(ConsumeContext<MensagemEntity> context)
        {
            var retentativas = _config.GetValue<int>("Servicos:Retry:Tentativas");
            int retentaivasPermitidas = retentativas - 1;

            return context.GetRetryCount() == retentaivasPermitidas;
        }
    }
}
