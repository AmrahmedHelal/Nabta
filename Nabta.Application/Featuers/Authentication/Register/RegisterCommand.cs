
using MediatR;
using Nabta.Application;
using Nabta.Application.Dtos.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.Authentication.Register
{
    public record RegisterCommand(RegisterDto dto) : IRequest<AuthModel>;
}
