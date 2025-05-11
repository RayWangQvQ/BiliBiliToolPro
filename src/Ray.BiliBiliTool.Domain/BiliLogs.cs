using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ray.BiliBiliTool.Domain;

[Table("bili_logs")]
public class BiliLogs
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("timeStamp")]
    public required DateTime Timestamp { get; set; }

    [Column("level")]
    public required string Level { get; set; }

    [Column("exception")]
    public string? Exception { get; set; }

    [Column("renderedMessage")]
    public string? RenderedMessage { get; set; }

    [Column("properties")]
    public string? Properties { get; set; }

    [Column("fireInstanceIdComputed")]
    public string? FireInstanceIdComputed { get; set; }

    public string FormattedLogLevel =>
        Level.ToLower() switch
        {
            "verbose" => "VERB",
            "debug" => "DBG",
            "information" => "INFO",
            "warning" => "WARN",
            "error" => "ERR",
            "fatal" => "FATAL",
            _ => Level.ToUpper(),
        };
}
