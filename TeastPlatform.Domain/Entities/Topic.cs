using System.Collections.Generic;
using TestPlatform.Domain.Configuration;

namespace TestPlatform.Domain.Entities;

public class Topic : Auditable
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation property for many-to-many
    public ICollection<TestTopic> TestTopics { get; set; } = new List<TestTopic>();
}
