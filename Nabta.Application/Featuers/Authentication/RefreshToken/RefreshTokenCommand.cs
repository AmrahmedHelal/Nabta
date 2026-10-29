using Nabta.Application.Featuers.Authentication;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nabta.Application.Featuers
{
    public record RefreshTokenCommand(string token) : IRequest<AuthModel>;
}
