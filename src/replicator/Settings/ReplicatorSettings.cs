// ReSharper disable UnusedAutoPropertyAccessor.Global

#nullable disable
using System.ComponentModel.DataAnnotations;

namespace replicator.Settings;

public record EsdbSettings {
    public string ConnectionString { get; init; }
    public string Protocol         { get; init; }
    public int    PageSize         { get; init; } = 1024;
#nullable enable
    public CertifcateSettings? Certificate { get; init; }
}

public record CertifcateSettings {
#nullable enable
    public string? CertificatePrivateKey { get; init; }
#nullable enable
    public string? Certificate { get; init; }
}

public record CheckpointSeeder {
    public string Path { get; init; }

    public string Type { get; init; } = "none"; // "chaser"
}

public record Checkpoint {
    public string           Path            { get; init; }
    public string           Type            { get; init; } = "file";
    public int              CheckpointAfter { get; init; } = 1000;
    public string           Database        { get; init; } = "replicator";
    public string           InstanceId      { get; init; } = "default";
    public CheckpointSeeder Seeder          { get; init; } = new();
}

public record SinkSettings : EsdbSettings {
    public int    PartitionCount { get; init; } = 1;
    public string Router         { get; init; }
    public string Partitioner    { get; init; }
    public int    BufferSize     { get; init; } = 1000;
}

public record TransformSettings {
    public string Type       { get; init; } = "default";
    public string Config     { get; init; }
    public int    BufferSize { get; init; } = 1;
}

public record Filter {
    public string Type    { get; init; }
    public string Include { get; init; }
    public string Exclude { get; init; }
}

public record Replicator {
    public EsdbSettings      Reader                          { get; init; }
    public SinkSettings      Sink                            { get; init; }
    public bool              Scavenge                        { get; init; }
    public bool              RestartOnFailure                { get; init; } = true;
    public bool              RunContinuously                 { get; init; } = true;
    public int               RestartDelayInSeconds           { get; init; } = 5;
    public int               ReportMetricsFrequencyInSeconds { get; init; } = 5;
    public Checkpoint        Checkpoint                      { get; init; } = new();
    public TransformSettings Transform                       { get; init; } = new();
    public Filter[]          Filters                         { get; init; }
    public bool              EnableAuth                      { get; init; } = true;
    public AuthSettings      Auth                            { get; init; } = new();
    public bool              ReadFromEnd                     { get; init; } = false;
    public string            ReadFromEndFrom                 { get; init; } = "reader"; // "writer"
}

public record AuthSettings {
    public string Username { get; init; }
    public string Password { get; init; }

    [Required]
    [MinLength(16, ErrorMessage = "JwtSecret must be at least 16 characters long.")]
    public string JwtSecret { get; init; } = "please_change_me_in_production";
}

public static class ConfigExtensions {
    public static T GetAs<T>(this IConfiguration configuration) where T : new() {
        T result = new();
        configuration.GetSection(typeof(T).Name).Bind(result);

        return result;
    }
}
