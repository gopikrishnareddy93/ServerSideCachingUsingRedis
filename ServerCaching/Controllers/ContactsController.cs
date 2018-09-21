using System.Net;
using System.Threading.Tasks;
using ServerCaching.Repository;
using ServerCaching.Models;
using Microsoft.AspNetCore.Mvc;

namespace ServerCaching.Controllers
{
    [Route("api/[controller]")]
    public class ContactsController : Controller
    {
        private readonly IContactsRepository m_ContactsRepo;
        private readonly ETagCache m_Cache;

        public ContactsController(IContactsRepository _repo, ETagCache _cache)
        {
            m_ContactsRepo = _repo;
            m_Cache = _cache;
        }


        [HttpGet("{id}", Name = "GetContacts")]
        public async Task<IActionResult> GetById(string id)
        {
            // If we have no cached contact, then get the contact from the database
            Contact contact =
                m_Cache.GetCachedObject<Contact>($"contact-{id}") ??
                              await m_ContactsRepo.Find(id);

            // If no contact was found, then return a 404
            if (contact == null)
            {
                return NotFound();
            }

            bool isModified = m_Cache.SetCachedObject($"contact-{id}", contact);

            if (isModified)
            {
                return Ok(contact);
            }

            return StatusCode((int)HttpStatusCode.NotModified);
        }
    }
}