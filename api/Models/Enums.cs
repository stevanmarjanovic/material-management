namespace MaterialManagement.Api.Models;

// Care values follow ISO 3758. Names must match web/src/catalog.js.
public enum Wash { W30, W30Mild, W30VeryMild, W40, W40Mild, W40VeryMild, W50, W50Mild, W60, W60Mild, W70, W95, Hand, DoNot }
public enum Bleach { Any, NonChlorine, DoNot }
public enum Iron { Low, Medium, High, DoNot }
public enum DryClean { P, PMild, F, FMild, W, WMild, WVeryMild, DoNot }
public enum TumbleDry { Normal, Low, DoNot }
public enum ProjectStatus { Planned, InProgress, Done }
public enum Fiber { Cotton, Polyester, Elastane, Viscose, Polyamide, Wool, Linen, Silk, Other }

public record FiberPart(Fiber Fiber, double Percent);
