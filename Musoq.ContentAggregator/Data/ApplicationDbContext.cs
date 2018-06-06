using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Musoq.ContentAggregator.Models;

namespace Musoq.ContentAggregator.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Script> Scripts { get; set; }

        public DbSet<UserScripts> UserScripts { get; set; }

        public DbSet<MusoqQueryScript> QueryScripts { get; set; }

        public DbSet<Table> Tables { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
    }
    public abstract class Script
    {
        public Guid ScriptId { get; set; }

        public virtual ScriptType Type { get; set; }

        public string ManagingName { get; set; }
    }

    public enum ShowAt {
        None,
        MainPage,
        UserMainPage
    }

    public class UserScripts
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid UserScriptId { get; set; }

        public string UserId { get; set; }

        [ForeignKey(nameof(Script))]
        public Guid ScriptId { get; set; }

        public Script Script { get; set; }

        public ShowAt ShowAt { get; set; }

        public DateTimeOffset RefreshedAt { get; set; }
    }

    public enum ScriptType
    {
        MusoqQuery
    }

    public class MusoqQueryScript : Script
    {
        public override ScriptType Type => ScriptType.MusoqQuery;

        public string Query { get; set; }
    }

    public class Table
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid TableId { get; set; }

        [ForeignKey(nameof(Script))]
        public Guid ScriptId { get; set; }

        public Script Script { get; set; }

        public string Json { get; set; }

        public Guid BatchId { get; set; }

        public DateTimeOffset InsertedAt { get; set; }
    }
}
