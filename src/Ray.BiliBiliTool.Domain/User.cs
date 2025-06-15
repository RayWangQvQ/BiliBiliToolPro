using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ray.BiliBiliTool.Domain;

[Table("bili_User")]
public class User
{
    [Key]
    [Column("id")]
    public long Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public required string Salt { get; set; }
    public List<string> Roles { get; set; } = [];
}
