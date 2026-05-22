namespace PRN232.LearningManagerSystem.Services.Models.Common;

/// <summary>
/// Query parameters used by service layer list operations.
/// Controller maps API-layer ListQueryParameters into this model before calling services.
/// </summary>
public class ServiceListQueryParameters
{
    public string? Search { get; set; }
    public string? Sort { get; set; }

    private int _page = 1;
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    private int _size = 10;
    public int Size
    {
        get => _size;
        set => _size = value < 1 ? 1 : value > 100 ? 100 : value;
    }

    public string? Fields { get; set; }
    public string? Expand { get; set; }
}
