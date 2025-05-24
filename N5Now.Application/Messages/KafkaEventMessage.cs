namespace N5Now.Application.Messages;

public class KafkaEventMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Operation { get; set; } = string.Empty;
}
