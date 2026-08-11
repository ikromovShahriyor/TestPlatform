using System;
using TestPlatform.Domain.Configuration;

namespace TestPlatform.Domain.Entities;

public class TestTopic : Auditable
{
    public Guid TestId { get; set; }
    public Test Test { get; set; } = null!;

    public Guid TopicId { get; set; }
    public Topic Topic { get; set; } = null!;
}
