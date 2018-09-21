//using System.Threading.Tasks;
//using ServerCaching.Repository;
//using Microsoft.AspNetCore.Mvc;
//using ServerCaching.Models;

//namespace ServerCaching.Controllers
//{
//    [Route("api/[controller]")]
//    public class ContactsController : Controller
//    {
//        public IContactsRepository ContactsRepo { get; set; }

//        public ContactsController(IContactsRepository _repo)
//        {
//            ContactsRepo = _repo;
//        }

//        [HttpGet("{id}", Name = "GetContacts")]
//        public async Task<IActionResult> GetById(string id)
//        {
//            Contact contact = await ContactsRepo.Find(id);

//            if (contact == null)
//            {
//                return NotFound();
//            }
//            return Ok(contact);
//        }
//    }
//}