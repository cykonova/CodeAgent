namespace CodeAgent.Agents;

public class WorkflowDefinition
{
    public string                    Id          { get; set; } = string.Empty;
    public string                    Name        { get; set; } = string.Empty;
    public string                    Description { get; set; } = string.Empty;
    public List<WorkflowStep>        Steps       { get; set; } = new();
    public Dictionary<string, object> Metadata    { get; set; } = new();
}