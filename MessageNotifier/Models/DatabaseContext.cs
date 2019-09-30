namespace MessageNotifier
{
    using MessageNotifierLibrary.Models;
    using System.Data.Entity;

    public class DatabaseContext : DbContext
    {
        public DatabaseContext()
            : base("name=DatabaseContext")
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<DbContext>());
        }

        public virtual DbSet<Contact> Contacts { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<ContactInfo> ContactInfo { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

        }
    }
}
