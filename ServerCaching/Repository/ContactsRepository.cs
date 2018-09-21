using System.Collections.Generic;
using System.Linq;
using ServerCaching.Contexts;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ServerCaching.Models;

namespace ServerCaching.Repository
{
    public class ContactsRepository : IContactsRepository
    {
        ContactsContext _context;
        public ContactsRepository(ContactsContext context)
        {
            _context = context;
        }       

        public async Task Add(Contact item)
        {            
            await _context.Contacts.AddAsync(item);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Contact>> GetAll()
        {
            return await _context.Contacts.ToListAsync();
        }

        public bool CheckValidUserKey(string reqkey)
        {
            var userkeyList = new List<string>
            {
                "28236d8ec201df516d0f6472d516d72d",
                "38236d8ec201df516d0f6472d516d72c",
                "48236d8ec201df516d0f6472d516d72b"
            };

            return userkeyList.Contains(reqkey);
        }

        public async Task<Contact> Find(string key)
        {
            return await _context.Contacts
                .Where(e => e.Id.Equals(key))
                .SingleOrDefaultAsync();
        }       

        public async Task Remove(string Id)
        {
            var itemToRemove = await _context.Contacts.SingleOrDefaultAsync(r => r.Id == Id);
            if (itemToRemove != null)
            {
                _context.Contacts.Remove(itemToRemove);
                await _context.SaveChangesAsync();
            }
        }

        public async Task Update(Contact item)
        {            
            var itemToUpdate = await _context.Contacts.SingleOrDefaultAsync(r => r.MobilePhone == item.MobilePhone);
            if (itemToUpdate != null)
            {
                itemToUpdate.FirstName = item.FirstName;
                itemToUpdate.LastName = item.LastName;
                itemToUpdate.IsFamilyMember = item.IsFamilyMember;
                itemToUpdate.Company = item.Company;
                itemToUpdate.JobTitle = item.JobTitle;
                itemToUpdate.Email = item.Email;
                itemToUpdate.MobilePhone = item.MobilePhone;
                itemToUpdate.DateOfBirth = item.DateOfBirth;
                itemToUpdate.AnniversaryDate = item.AnniversaryDate;
                await _context.SaveChangesAsync();
            }
        }
    }
}