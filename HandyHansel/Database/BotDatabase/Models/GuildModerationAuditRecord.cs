using NodaTime;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HandyHansel.BotDatabase.Models
{
    [Table("all_guild_moderation_audit_records")]
    public class GuildModerationAuditRecord
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("guild_id")]
        public ulong GuildId { get; set; }

        [Column("moderator_user_id")]
        public ulong ModeratorUserId { get; set; }

        [Column("user_id")]
        public ulong UserId { get; set; }

        [Column("moderation_action_type")]
        public ModerationActionType ModerationAction { get; set; }

        [Column("reason")]
        public string Reason { get; set; }

        [Column("timestamp")]
        public Instant Timestamp { get; set; }
    }

    public enum ModerationActionType
    {
        NONE,
        WARN,
        BAN,
        TEMPBAN,
        MUTE,
        TEMPMUTE,
        KICK,
        NEGATEKARMA,
    }
}