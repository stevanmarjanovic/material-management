namespace MaterialManagement.Api.Dtos;

public record CutInput(double X, double Y, double W, double H, string? Label, int? ProjectId);
