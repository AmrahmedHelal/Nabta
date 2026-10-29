using MediatR;
using Nabta.Application.Dtos.Auth;
using Nabta.Application.Featuers.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nabta.Application.Featuers.Authentication.LogIn
{
    public record LogInCommand(LoginDto dto) : IRequest<AuthModel>;
}
