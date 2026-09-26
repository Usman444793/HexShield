using Microsoft.AspNetCore.Authorization;
using System;

namespace HexShield.Infrastructure.Authorization;

public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) : base()
    {
        Policy = $"Permission_{{permission}}";
    }
}
