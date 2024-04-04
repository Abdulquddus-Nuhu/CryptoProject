using Microsoft.AspNetCore.Identity;
using System.Xml.Linq;

namespace CryptoProject.Entities.Identity
{
    public class Role : IdentityRole<Guid>
    {
        public Role()
        {
            Id = Guid.NewGuid();
        }

        public Role(string roleName)
        {
            Id = Guid.NewGuid();
            Name = roleName;
        }

    }

}
