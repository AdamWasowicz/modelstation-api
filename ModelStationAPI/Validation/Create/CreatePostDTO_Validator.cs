﻿using FluentValidation;
using ModelStationAPI.Entities;
using ModelStationAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModelStationAPI.Validation
{
    public class CreatePostDTO_Validator : AbstractValidator<CreatePostDTO>
    {
        public CreatePostDTO_Validator(ModelStationDbContext dbContext)
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .MaximumLength(32);


            RuleFor(x => x.Text)
                .MaximumLength(256);
        }
    }
}
