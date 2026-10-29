using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nabta.Infrastructure.identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nabta.Infrastructure.Data
{
    public class AppDbContext: IdentityDbContext<ApplicationUser>
    {
        public AppDbContext( DbContextOptions<AppDbContext> options): base(options)
        {
        }
    }
}
