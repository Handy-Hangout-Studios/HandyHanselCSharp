using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyHansel.Models
{
    [Table("all_guild_prefixes")]
    public class GuildPrefix
    {
        [Key] [Column("id")] public int Id { get; set; }

        [Column("prefix")] public string Prefix { get; set; }

        [Column("guild_id")] public ulong GuildId { get; set; }
    }
}