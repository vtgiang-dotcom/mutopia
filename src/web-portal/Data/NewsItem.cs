using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenMU.PlayerWeb.Data;

/// <summary>
/// News table owned by the player web (created via SQL, not by the OpenMU server).
/// </summary>
[Table("OpenMuWeb_News", Schema = "data")]
public class NewsItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("body")]
    public string Body { get; set; } = string.Empty;

    [Column("author")]
    public string Author { get; set; } = string.Empty;

    [Column("creationDate")]
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
}
