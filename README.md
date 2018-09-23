# Scalable and Performant ASP.NET Core Web API's: Server side caching using Redis


In this post we’ll implement a shared cache on the server.

## Benchmark

Before we see the impact of caching data on server, let’s take a benchmark on a webapi endpoint contacts/{contactId}, where we have just added our ETag in the last post. We’ll load test on a record that hasn’t been modified, so, should get a 304. We’ll use WebSurge for the load test. The results are in the screenshot below:

![screenshot of conversion](https://raw.githubusercontent.com/gopikrishnareddy93/ServerSideCachingUsingRedis/master/Screenshots/Caching-ETag-LoadTest.png)


## Implementing a redis server cache

So, let’s implement the server cache now. We’ll choose redis for our cache. First, we need to add the Microsoft.Extensions.Caching.Redis nuget package to our project. 

We will create ETagCache Class that will expose a method, GetCachedObject, that retreives an object from the redis cache for the requested ETag. We also expose a method, SetCachedObject, that sets an object in the cache and adds an “ETag” HTTP header.


``` csharp

public class ETagCache
{
    private readonly IDistributedCache _cache;
    private readonly HttpContext _httpContext;
    public ETagCache(IDistributedCache cache, IHttpContextAccessor httpContextAccessor)
    {
        _cache = cache;
        _httpContext = httpContextAccessor.HttpContext;
    }

    public T GetCachedObject<T>(string cacheKeyPrefix)
    {
        string requestETag = GetRequestedETag();

        if (!string.IsNullOrEmpty(requestETag))
        {
            // Construct the key for the cache 
            string cacheKey = $"{cacheKeyPrefix}-{requestETag}";

            // Get the cached item
            string cachedObjectJson = _cache.GetString(cacheKey);

            // If there was a cached item then deserialise this 
            if (!string.IsNullOrEmpty(cachedObjectJson))
            {
                return JsonConvert.DeserializeObject<T>(cachedObjectJson);
            }
        }

        return default(T);
    }

    public bool SetCachedObject(string cacheKeyPrefix, dynamic objectToCache)
    {
        if (!IsCacheable(objectToCache))
        {
            return true;
        }

        string requestETag = GetRequestedETag();
        string responseETag = Convert.ToBase64String(objectToCache.RowVersion);

        // Add the contact to the cache for 30 mins if not already in the cache
        if (objectToCache != null && responseETag != null)
        {
            string cacheKey = $"{cacheKeyPrefix}-{responseETag}";
            string serializedObjectToCache = JsonConvert.SerializeObject(objectToCache);
            _cache.SetString(cacheKey, serializedObjectToCache, new DistributedCacheEntryOptions() { AbsoluteExpiration = DateTime.Now.AddMinutes(30) });
        }

        // Add the current ETag to the HTTP header
        _httpContext.Response.Headers.Add("ETag", responseETag);

        bool IsModified = !(_httpContext.Request.Headers.ContainsKey("If-None-Match") && responseETag == requestETag);
        return IsModified;
    }

    private string GetRequestedETag()
    {
        return _httpContext.Request.Headers.ContainsKey("If-None-Match") ? _httpContext.Request.Headers["If-None-Match"].First() : string.Empty;
    }

    private bool IsCacheable(dynamic objectToCache)
    {
        var type = objectToCache.GetType();
        return type.GetProperty("RowVersion") != null;
    }
}

```

We make this available to be injected into our controllers by registering the service in Startup. We also need to allow ETagCache get access to HttpContext by registering HttpContextAccessor.


Finally then wire this up in Startup.ConfigureServices()

``` csharp
 
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddDistributedRedisCache(options =>
    {
        options.Configuration = "localhost:6379"; //location of redis server
    });
    services.AddScoped<ETagCache>();
    services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    services.AddMvc();
    ...
}

```

In this implementation, we only read from the cache if an ETag has been supplied in the request – this allows the client to determine whether the server cache should be used.


We can then inject ETagCache into our controllers and use it within the appropriate action methods:

``` csharp

[Route("api/contacts")]
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

```

When our API is hit for the first time we get an ETag:

![screenshot of conversion](https://raw.githubusercontent.com/gopikrishnareddy93/ServerSideCachingUsingRedis/master/Screenshots/Caching-ETag-ServerCache-1stRequest.png)

The data is also cached in redis:

![screenshot of conversion](https://raw.githubusercontent.com/gopikrishnareddy93/ServerSideCachingUsingRedis/master/Screenshots/Caching-ETag-ServerCache-redis.png)

If we then hit our API again, this time passing the ETag, we get a 304 in a fast response:

![screenshot of conversion](https://raw.githubusercontent.com/gopikrishnareddy93/ServerSideCachingUsingRedis/master/Screenshots/Caching-ETag-ServerCache-2ndRequest.png)

So, let’s now load test this again passing the ETag, with the redis cached item in place:

![screenshot of conversion](https://raw.githubusercontent.com/gopikrishnareddy93/ServerSideCachingUsingRedis/master/Screenshots/Caching-ETag-ServerCache-LoadTest.png)

That’s a decent improvement!

In the above example we set the cache to expire after a certain amount of time (30 mins). The other approach is to proactively remove the cached item when the resource is updated via IDistributedCache.Remove(cacheKey).

## Conclusion

Once we’ve setup a bit of generic infrastructure code, it’s pretty easy to implement ETags with server side caching in our action methods. It does give a good performance improvement as well – particularly when it prevents an expensive database query.