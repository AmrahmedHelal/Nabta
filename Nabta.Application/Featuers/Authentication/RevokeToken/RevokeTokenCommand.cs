using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Authentication.RevokeToken
{
    public record RevokeTokenCommand(string token) : IRequest<bool>; 
}
