namespace MaterialManagement.Api.Models;

public class Project
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ProjectStatus Status { get; set; }
    public DateOnly? StartDate { get; set; }
    public string? Notes { get; set; }
    public byte[]? Photo { get; set; }
    public string? PhotoContentType { get; set; }
    // Care overrides for the finished product; null = use the calculated (strictest) value.
    public Wash? Wash { get; set; }
    public Bleach? Bleach { get; set; }
    public Iron? Iron { get; set; }
    public DryClean? DryClean { get; set; }
    public TumbleDry? TumbleDry { get; set; }
}
