using Microsoft.EntityFrameworkCore;
using ServerCaching.Models;

namespace ServerCaching.Contexts
{
    public class ContactsContext : DbContext
    {
        public ContactsContext(DbContextOptions<ContactsContext> options)
            :base(options) { }
        public ContactsContext(){ }
        public DbSet<Contact> Contacts { get; set; }
    }
}
