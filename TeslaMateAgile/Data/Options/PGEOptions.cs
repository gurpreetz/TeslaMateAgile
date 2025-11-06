using System.ComponentModel.DataAnnotations;

namespace TeslaMateAgile.Data.Options;

public class PGEOptions
{
    [Required]
    public string BaseUrl { get; set; }

    [Required]
    public string Utility { get; set; }

    [Required]
    public string Market { get; set; }

    [Required(ErrorMessage = "RateName is required. Please specify your PGE rate plan (e.g., 'EV2A', 'E-TOU-C', etc.)")]
    public string RateName { get; set; }

    [Required(ErrorMessage = "RepresentativeCircuitId is required. Please specify your PGE representative circuit ID for your service territory.")]
    public string RepresentativeCircuitId { get; set; }

    [Required]
    public string Program { get; set; }
}
